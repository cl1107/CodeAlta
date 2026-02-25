using System.Diagnostics;
using CodeNoesis.CodexSdk.Generator;

const string schemaFolderName = "codex_app-server_schema";
const string combinedSchemaFileName = "codex_app_server_protocol.schemas.json";
const string defaultNamespace = "CodeNoesis.CodexSdk";

string? schemaFile = null;
string? outputDir = null;
var rootNamespace = defaultNamespace;

for (var i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--schema" or "-s" when i + 1 < args.Length:
            schemaFile = args[++i];
            break;
        case "--output" or "-o" when i + 1 < args.Length:
            outputDir = args[++i];
            break;
        case "--namespace" or "-n" when i + 1 < args.Length:
            rootNamespace = args[++i];
            break;
    }
}

// Default schema location: next to the running executable
var exeDir = AppContext.BaseDirectory;
var schemaDir = Path.Combine(exeDir, schemaFolderName);

if (schemaFile is null)
{
    var candidate = Path.Combine(schemaDir, combinedSchemaFileName);
    if (!File.Exists(candidate))
    {
        Console.WriteLine($"Schema not found at {candidate}");
        Console.WriteLine("Generating schema via: codex app-server generate-json-schema ...");

        Directory.CreateDirectory(schemaDir);

        // Resolve the codex executable. On Windows it may be a .ps1/.cmd shim
        // installed via fnm/npm, so fall back to invoking through the shell.
        var codexPath = FindExecutable("codex");
        ProcessStartInfo psi;
        if (codexPath != null)
        {
            psi = new ProcessStartInfo(codexPath, $"app-server generate-json-schema --out \"{schemaDir}\"")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };
        }
        else if (OperatingSystem.IsWindows())
        {
            // Use pwsh to resolve PATH shims (.ps1 / .cmd wrappers)
            psi = new ProcessStartInfo("pwsh",
                $"-NoProfile -NonInteractive -Command \"codex app-server generate-json-schema --out '{schemaDir}'\"")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };
        }
        else
        {
            psi = new ProcessStartInfo("/bin/sh",
                $"-c \"codex app-server generate-json-schema --out '{schemaDir}'\"")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };
        }

        using var proc = Process.Start(psi)
            ?? throw new InvalidOperationException("Failed to start codex process.");
        await proc.WaitForExitAsync().ConfigureAwait(false);

        if (proc.ExitCode != 0)
        {
            var stderr = await proc.StandardError.ReadToEndAsync().ConfigureAwait(false);
            Console.Error.WriteLine($"codex exited with code {proc.ExitCode}: {stderr}");
            return 1;
        }

        Console.WriteLine("Schema generated successfully.");
    }
    else
    {
        Console.WriteLine("Schema folder already exists, skipping generation.");
    }

    schemaFile = candidate;
}

// Default output: src/CodeNoesis.CodexSdk/generated (relative to repo root)
outputDir ??= Path.GetFullPath(
    Path.Combine(exeDir, "..", "..", "..", "..", "CodeNoesis.CodexSdk", "generated"));

Console.WriteLine($"Schema:    {schemaFile}");
Console.WriteLine($"Output:    {outputDir}");
Console.WriteLine($"Namespace: {rootNamespace}");
Console.WriteLine();

// Load all definitions
var defs = await SchemaWalker.LoadDefinitionsAsync(schemaFile, rootNamespace);
Console.WriteLine($"Found {defs.Count} type definitions");

// Build emitter with full type registry
var emitter = new CSharpEmitter(defs, rootNamespace);

// Emit all types
var filesByNamespace = emitter.EmitAll(defs);

// Clean output directory before writing
if (Directory.Exists(outputDir))
    Directory.Delete(outputDir, recursive: true);

// Write files
var totalFiles = 0;
foreach (var (ns, files) in filesByNamespace)
{
    // Map namespace to directory: CodeNoesis.CodexSdk -> outputDir,
    // CodeNoesis.CodexSdk.V2 -> outputDir/V2
    var relPath = ns == rootNamespace
        ? ""
        : ns[(rootNamespace.Length + 1)..].Replace('.', Path.DirectorySeparatorChar);
    var dir = Path.Combine(outputDir, relPath);
    Directory.CreateDirectory(dir);

    foreach (var (fileName, content) in files)
    {
        var filePath = Path.Combine(dir, fileName);
        await File.WriteAllTextAsync(filePath, content).ConfigureAwait(false);
        totalFiles++;
    }
}

Console.WriteLine($"Generated {totalFiles} files in {outputDir}/");

// Generate serializer context
var contextCode = emitter.EmitSerializerContext("CodexJsonSerializerContext");
var contextPath = Path.Combine(outputDir, "CodexJsonSerializerContext.gen.cs");
await File.WriteAllTextAsync(contextPath, contextCode).ConfigureAwait(false);
totalFiles++;

Console.WriteLine($"  (includes serializer context with {totalFiles - 1} type registrations)");

// Print warnings about types that fell back to JsonElement
if (emitter.Warnings.Count > 0)
{
    Console.WriteLine();
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine($"Warnings ({emitter.Warnings.Count}):");
    foreach (var warning in emitter.Warnings)
        Console.WriteLine($"  WARN: {warning}");
    Console.ResetColor();
}

return 0;

static string? FindExecutable(string name)
{
    var pathVar = Environment.GetEnvironmentVariable("PATH") ?? "";
    var extensions = OperatingSystem.IsWindows()
        ? new[] { ".exe", ".cmd", ".bat" }
        : Array.Empty<string>();

    foreach (var dir in pathVar.Split(Path.PathSeparator))
    {
        // Try known executable extensions first (avoids matching Unix shell
        // scripts on Windows that share the same base name).
        foreach (var ext in extensions)
        {
            var withExt = Path.Combine(dir, name + ext);
            if (File.Exists(withExt))
                return withExt;
        }

        // Direct match (Linux binary without extension)
        if (!OperatingSystem.IsWindows())
        {
            var candidate = Path.Combine(dir, name);
            if (File.Exists(candidate))
                return candidate;
        }
    }

    return null;
}
