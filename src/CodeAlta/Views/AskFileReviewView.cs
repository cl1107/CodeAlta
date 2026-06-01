using CodeAlta.LiveTool;
using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Extensions.CodeEditor.TextMateSharp;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Text;

namespace CodeAlta.Views;

internal static class AskFileReviewView
{
    public static Visual? Create(AltaAskFile? file, IReadOnlyList<string> rootCandidates)
    {
        if (string.IsNullOrWhiteSpace(file?.Path))
        {
            return null;
        }

        var resolution = ResolveFilePath(file.Path!, rootCandidates);
        var body = TryReadText(resolution.FullPath, out var text, out var error)
            ? CreateEditor(text, resolution.FullPath)
            : CreateUnavailableContent(error);

        return new DockLayout()
            .Top(new VStack(
                new Markup("[bold]File context[/]"),
                new TextBlock(resolution.DisplayPath) { Wrap = true })
            {
                Spacing = 0,
                Margin = new Thickness(1, 0, 1, 0),
                HorizontalAlignment = Align.Stretch,
            })
            .Content(new Border(body.Stretch())
            {
                HorizontalAlignment = Align.Stretch,
                VerticalAlignment = Align.Stretch,
            })
            .Bottom(new Markup("[dim]File context replaces the session timeline while this ask is open · Ctrl+F find[/]")
            {
                Wrap = true,
                Margin = new Thickness(1, 0, 1, 0),
            })
            .HorizontalAlignment(Align.Stretch)
            .VerticalAlignment(Align.Stretch);
    }

    private static Visual CreateEditor(string text, string fullPath)
    {
        var editor = new CodeEditor()
            .AutoFocus(false)
            .WordWrap(false)
            .ShowLineNumbers(true)
            .HighlightCurrentLine(true)
            .MinHeight(8);
        editor.TextDocument = new TextDocument(text);
        editor.SyntaxHighlighter = CreateSyntaxHighlighter(fullPath);

        return new ScrollViewer(editor.Stretch(), focusable: false)
            .IsTabStop(false)
            .HorizontalAlignment(Align.Stretch)
            .VerticalAlignment(Align.Stretch);
    }

    private static Visual CreateUnavailableContent(string message)
        => new ScrollViewer(new TextBlock(message)
        {
            Wrap = true,
            Margin = new Thickness(1),
            HorizontalAlignment = Align.Stretch,
            VerticalAlignment = Align.Stretch,
        }, focusable: false)
            .HorizontalScrollEnabled(false)
            .VerticalScrollEnabled(true)
            .Stretch();

    private static bool TryReadText(string fullPath, out string text, out string error)
    {
        try
        {
            if (!File.Exists(fullPath))
            {
                text = string.Empty;
                error = $"Attached ask file was not found: {fullPath}";
                return false;
            }

            text = File.ReadAllText(fullPath);
            error = string.Empty;
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException or System.Security.SecurityException)
        {
            text = string.Empty;
            error = $"Attached ask file could not be loaded: {ex.Message}";
            return false;
        }
    }

    private static AskFileResolution ResolveFilePath(string path, IReadOnlyList<string> rootCandidates)
    {
        var normalizedPath = path.Trim();
        if (Path.IsPathFullyQualified(normalizedPath))
        {
            var fullPath = Path.GetFullPath(normalizedPath);
            return new AskFileResolution(fullPath, fullPath);
        }

        foreach (var root in rootCandidates)
        {
            if (string.IsNullOrWhiteSpace(root))
            {
                continue;
            }

            var candidate = Path.GetFullPath(Path.Combine(root, normalizedPath));
            if (File.Exists(candidate))
            {
                return new AskFileResolution(candidate, path.Replace('\\', '/'));
            }
        }

        var fallbackRoot = rootCandidates.FirstOrDefault(static root => !string.IsNullOrWhiteSpace(root)) ?? Environment.CurrentDirectory;
        var fallback = Path.GetFullPath(Path.Combine(fallbackRoot, normalizedPath));
        return new AskFileResolution(fallback, path.Replace('\\', '/'));
    }

    private static CodeEditorSyntaxHighlighter? CreateSyntaxHighlighter(string fullPath)
    {
        try
        {
            return new TextMateCodeEditorSyntaxHighlighter(
                new TextMateCodeEditorOptions
                {
                    FileName = fullPath,
                });
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    private sealed record AskFileResolution(string FullPath, string DisplayPath);
}
