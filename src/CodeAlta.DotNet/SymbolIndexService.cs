using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeAlta.DotNet;

/// <summary>
/// Builds a .NET symbol index from source files.
/// </summary>
public sealed class SymbolIndexService
{
    /// <summary>
    /// Builds symbol records for all discovered C# projects in a workspace snapshot.
    /// </summary>
    /// <param name="snapshot">Workspace snapshot.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Indexed symbol records.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="snapshot"/> is <see langword="null"/>.</exception>
    public async Task<IReadOnlyList<DotNetSymbolRecord>> BuildIndexAsync(
        DotNetWorkspaceSnapshot snapshot,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var records = new List<DotNetSymbolRecord>();
        foreach (var project in snapshot.Projects.Where(static x =>
                     string.Equals(x.Language, "csharp", StringComparison.OrdinalIgnoreCase)))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var projectDirectory = Path.GetDirectoryName(project.ProjectPath);
            if (string.IsNullOrWhiteSpace(projectDirectory) || !Directory.Exists(projectDirectory))
            {
                continue;
            }

            foreach (var file in EnumerateSourceFiles(projectDirectory))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var fileRecords = await BuildIndexForFileAsync(file, cancellationToken).ConfigureAwait(false);
                records.AddRange(fileRecords);
            }
        }

        return records
            .OrderBy(static x => x.FullyQualifiedName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static x => x.FilePath, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static x => x.StartLine)
            .ToArray();
    }

    /// <summary>
    /// Builds symbol records for a single C# source file.
    /// </summary>
    /// <param name="filePath">Source file path.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Indexed symbol records.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="filePath"/> is empty.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the source file does not exist.</exception>
    public async Task<IReadOnlyList<DotNetSymbolRecord>> BuildIndexForFileAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("File path is required.", nameof(filePath));
        }

        var normalized = Path.GetFullPath(filePath);
        if (!File.Exists(normalized))
        {
            throw new FileNotFoundException("Source file was not found.", normalized);
        }

        var source = await File.ReadAllTextAsync(normalized, cancellationToken).ConfigureAwait(false);
        var tree = CSharpSyntaxTree.ParseText(source, path: normalized, cancellationToken: cancellationToken);
        var root = await tree.GetRootAsync(cancellationToken).ConfigureAwait(false);

        var collector = new SymbolCollector(normalized);
        collector.Visit(root);
        return collector.Records;
    }

    private static IEnumerable<string> EnumerateSourceFiles(string projectDirectory)
    {
        foreach (var file in Directory.EnumerateFiles(projectDirectory, "*.cs", SearchOption.AllDirectories))
        {
            if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase) ||
                file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            yield return file;
        }
    }

    private sealed class SymbolCollector : CSharpSyntaxWalker
    {
        private readonly string _filePath;
        private readonly Stack<string> _namespaceStack = new();
        private readonly Stack<string> _typeStack = new();

        public SymbolCollector(string filePath)
            : base(SyntaxWalkerDepth.StructuredTrivia)
        {
            _filePath = filePath;
        }

        public List<DotNetSymbolRecord> Records { get; } = [];

        public override void VisitFileScopedNamespaceDeclaration(FileScopedNamespaceDeclarationSyntax node)
        {
            AddSymbol("namespace", node.Name.ToString(), node, summary: null);
            _namespaceStack.Push(node.Name.ToString());
            base.VisitFileScopedNamespaceDeclaration(node);
            _namespaceStack.Pop();
        }

        public override void VisitNamespaceDeclaration(NamespaceDeclarationSyntax node)
        {
            AddSymbol("namespace", node.Name.ToString(), node, summary: null);
            _namespaceStack.Push(node.Name.ToString());
            base.VisitNamespaceDeclaration(node);
            _namespaceStack.Pop();
        }

        public override void VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            VisitTypeLike("class", node.Identifier.Text, node, n => base.VisitClassDeclaration((ClassDeclarationSyntax)n));
        }

        public override void VisitStructDeclaration(StructDeclarationSyntax node)
        {
            VisitTypeLike("struct", node.Identifier.Text, node, n => base.VisitStructDeclaration((StructDeclarationSyntax)n));
        }

        public override void VisitRecordDeclaration(RecordDeclarationSyntax node)
        {
            VisitTypeLike("record", node.Identifier.Text, node, n => base.VisitRecordDeclaration((RecordDeclarationSyntax)n));
        }

        public override void VisitInterfaceDeclaration(InterfaceDeclarationSyntax node)
        {
            VisitTypeLike("interface", node.Identifier.Text, node, n => base.VisitInterfaceDeclaration((InterfaceDeclarationSyntax)n));
        }

        public override void VisitEnumDeclaration(EnumDeclarationSyntax node)
        {
            VisitTypeLike("enum", node.Identifier.Text, node, n => base.VisitEnumDeclaration((EnumDeclarationSyntax)n));
        }

        public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            AddMemberSymbol("method", node.Identifier.Text, node);
            base.VisitMethodDeclaration(node);
        }

        public override void VisitPropertyDeclaration(PropertyDeclarationSyntax node)
        {
            AddMemberSymbol("property", node.Identifier.Text, node);
            base.VisitPropertyDeclaration(node);
        }

        public override void VisitFieldDeclaration(FieldDeclarationSyntax node)
        {
            foreach (var variable in node.Declaration.Variables)
            {
                AddMemberSymbol("field", variable.Identifier.Text, variable);
            }

            base.VisitFieldDeclaration(node);
        }

        private void VisitTypeLike(
            string kind,
            string name,
            CSharpSyntaxNode node,
            Action<CSharpSyntaxNode> baseVisit)
        {
            AddSymbol(kind, name, node, GetSummary(node));
            _typeStack.Push(name);
            baseVisit(node);
            _typeStack.Pop();
        }

        private void AddMemberSymbol(string kind, string name, CSharpSyntaxNode node)
        {
            AddSymbol(kind, name, node, GetSummary(node));
        }

        private void AddSymbol(string kind, string name, CSharpSyntaxNode node, string? summary)
        {
            var lineSpan = node.GetLocation().GetLineSpan();
            Records.Add(
                new DotNetSymbolRecord
                {
                    Kind = kind,
                    Name = name,
                    FullyQualifiedName = GetQualifiedName(name),
                    FilePath = _filePath,
                    StartLine = lineSpan.StartLinePosition.Line + 1,
                    EndLine = lineSpan.EndLinePosition.Line + 1,
                    Summary = summary,
                });
        }

        private string GetQualifiedName(string leafName)
        {
            var parts = new List<string>();
            if (_namespaceStack.Count > 0)
            {
                parts.AddRange(_namespaceStack.Reverse());
            }

            if (_typeStack.Count > 0)
            {
                parts.AddRange(_typeStack.Reverse());
            }

            parts.Add(leafName);
            return string.Join(".", parts.Where(static x => !string.IsNullOrWhiteSpace(x)));
        }

        private static string? GetSummary(CSharpSyntaxNode node)
        {
            var lines = node.GetLeadingTrivia()
                .Select(static x => x.ToString().Trim())
                .Where(static x => x.StartsWith("///", StringComparison.Ordinal))
                .Select(static x => x[3..].Trim())
                .ToArray();
            if (lines.Length == 0)
            {
                return null;
            }

            var text = string.Join(" ", lines);
            return text
                .Replace("<summary>", string.Empty, StringComparison.OrdinalIgnoreCase)
                .Replace("</summary>", string.Empty, StringComparison.OrdinalIgnoreCase)
                .Trim();
        }
    }
}
