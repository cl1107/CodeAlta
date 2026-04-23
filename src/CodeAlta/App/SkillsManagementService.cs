using CodeAlta.Catalog;
using CodeAlta.Catalog.Skills;

namespace CodeAlta.App;

internal sealed class SkillsManagementService
{
    private static readonly string[] RelatedFileDirectories = ["scripts", "references", "assets"];

    private readonly SkillCatalog _skillCatalog;
    private readonly CatalogOptions _catalogOptions;
    private readonly Func<ProjectDescriptor?> _getSelectedProject;

    public SkillsManagementService(
        SkillCatalog skillCatalog,
        CatalogOptions catalogOptions,
        Func<ProjectDescriptor?> getSelectedProject)
    {
        ArgumentNullException.ThrowIfNull(skillCatalog);
        ArgumentNullException.ThrowIfNull(catalogOptions);
        ArgumentNullException.ThrowIfNull(getSelectedProject);

        _skillCatalog = skillCatalog;
        _catalogOptions = catalogOptions;
        _getSelectedProject = getSelectedProject;
    }

    public async Task<IReadOnlyList<SkillDescriptor>> LoadAsync(
        SkillsManagementScope scope,
        CancellationToken cancellationToken = default)
    {
        var selectedProject = _getSelectedProject();
        var query = new SkillCatalogQuery
        {
            Discovery = new SkillDiscoveryContext
            {
                ProjectRoots = scope is SkillsManagementScope.CurrentProject or SkillsManagementScope.Combined &&
                               !string.IsNullOrWhiteSpace(selectedProject?.ProjectPath)
                    ? [selectedProject.ProjectPath]
                    : [],
                UserCodeAltaRoot = scope is SkillsManagementScope.User or SkillsManagementScope.Combined
                    ? _catalogOptions.GlobalRoot
                    : null,
                UserProfileRoot = scope is SkillsManagementScope.User or SkillsManagementScope.Combined
                    ? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
                    : null,
            },
            IncludeInvalid = true,
            IncludeShadowed = true,
            IncludeUntrusted = true,
        };

        return await _skillCatalog.ListAsync(query, cancellationToken).ConfigureAwait(false);
    }

    public static IReadOnlyList<SkillRelatedFile> ListRelatedFiles(SkillDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        if (string.IsNullOrWhiteSpace(descriptor.SkillRootPath) ||
            !Directory.Exists(descriptor.SkillRootPath))
        {
            return [];
        }

        var skillRootPath = Path.GetFullPath(descriptor.SkillRootPath);
        var relatedFiles = new List<SkillRelatedFile>();
        foreach (var directoryName in RelatedFileDirectories)
        {
            var directoryPath = Path.GetFullPath(Path.Combine(skillRootPath, directoryName));
            if (!IsUnderRoot(directoryPath, skillRootPath) ||
                !Directory.Exists(directoryPath))
            {
                continue;
            }

            var files = Directory.EnumerateFiles(
                directoryPath,
                "*",
                new EnumerationOptions
                {
                    RecurseSubdirectories = true,
                    IgnoreInaccessible = true,
                    AttributesToSkip = FileAttributes.ReparsePoint,
                });
            foreach (var filePath in files)
            {
                var fullPath = Path.GetFullPath(filePath);
                if (!IsUnderRoot(fullPath, skillRootPath))
                {
                    continue;
                }

                var relativePath = Path.GetRelativePath(skillRootPath, fullPath).Replace('\\', '/');
                relatedFiles.Add(new SkillRelatedFile(directoryName, relativePath, fullPath));
            }
        }

        return relatedFiles
            .OrderBy(static file => Array.IndexOf(RelatedFileDirectories, file.Category))
            .ThenBy(static file => file.RelativePath, StringComparer.OrdinalIgnoreCase)
            .Take(128)
            .ToArray();
    }

    private static bool IsUnderRoot(string candidatePath, string rootPath)
    {
        var normalizedCandidate = AppendDirectorySeparator(Path.GetFullPath(candidatePath));
        var normalizedRoot = AppendDirectorySeparator(Path.GetFullPath(rootPath));
        var comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
        return normalizedCandidate.StartsWith(normalizedRoot, comparison);
    }

    private static string AppendDirectorySeparator(string path)
        => path.Length > 0 && (path[^1] == Path.DirectorySeparatorChar || path[^1] == Path.AltDirectorySeparatorChar)
            ? path
            : path + Path.DirectorySeparatorChar;
}

internal enum SkillsManagementScope
{
    Combined,
    CurrentProject,
    User,
}

internal sealed record SkillRelatedFile(string Category, string RelativePath, string FullPath);
