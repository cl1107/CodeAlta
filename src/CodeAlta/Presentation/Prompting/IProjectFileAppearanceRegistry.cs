using CodeAlta.Search;

namespace CodeAlta.Presentation.Prompting;

internal interface IProjectFileAppearanceRegistry
{
    ProjectFileAppearance GetAppearance(ProjectFileSearchItem item);
}
