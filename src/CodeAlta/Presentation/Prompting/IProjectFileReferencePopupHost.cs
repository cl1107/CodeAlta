using XenoAtom.Terminal.UI;

namespace CodeAlta.Presentation.Prompting;

internal interface IProjectFileReferencePopupHost
{
    string? Text { get; set; }

    int CaretIndex { get; set; }

    Visual Visual { get; }

    void FocusPromptEditor();
}
