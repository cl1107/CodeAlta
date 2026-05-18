using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Extensions.CodeEditor.TextMateSharp;
using XenoAtom.Terminal.UI.Text;

namespace CodeAlta.Views;

internal static class ConfigTomlEditor
{
    public static CodeEditor Create(string text, string? fileName)
    {
        var editor = new CodeEditor()
            .AutoFocus(true)
            .WordWrap(false)
            .ShowLineNumbers(true)
            .HighlightCurrentLine(true)
            .MinHeight(12);
        editor.TextDocument = new TextDocument(text);
        editor.SyntaxHighlighter = CreateSyntaxHighlighter(fileName);
        return editor;
    }

    private static CodeEditorSyntaxHighlighter? CreateSyntaxHighlighter(string? fileName)
    {
        try
        {
            return new TextMateCodeEditorSyntaxHighlighter(
                new TextMateCodeEditorOptions
                {
                    LanguageId = "toml",
                    FileName = fileName,
                });
        }
        catch (ArgumentException)
        {
            return null;
        }
    }
}
