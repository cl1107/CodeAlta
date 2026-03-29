using System.Text;

namespace CodeAlta.Presentation.Sidebar;

internal enum SidebarRowActionKind
{
    DeleteThread = 0,
    DeleteProject = 1,
    OpenProjectThreads = 2,
    OpenProjectDetails = 3,
}

internal readonly record struct SidebarRowActionDescriptor(
    SidebarRowActionKind Kind,
    Rune Icon,
    string Tooltip);
