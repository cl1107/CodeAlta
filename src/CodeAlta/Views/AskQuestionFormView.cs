using CodeAlta.LiveTool;
using XenoAtom.Terminal;
using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Commands;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Input;

namespace CodeAlta.Views;

internal sealed class AskQuestionFormView
{
    private readonly AltaAskRequest _request;
    private readonly bool[] _visited;
    private readonly CheckBox[][] _choiceBoxes;
    private readonly TextBox?[] _freeformBoxes;

    public AskQuestionFormView(AltaQueuedAsk ask)
    {
        ArgumentNullException.ThrowIfNull(ask);
        _request = ask.Request;
        _visited = new bool[_request.Questions.Count];
        _choiceBoxes = new CheckBox[_request.Questions.Count][];
        _freeformBoxes = new TextBox?[_request.Questions.Count];

        var pages = _request.Questions
            .Select((question, index) => new TabPage(question.Title ?? $"Question {index + 1}", BuildQuestionPage(question, index)))
            .ToArray();
        Tabs = new TabControl(pages)
        {
            HorizontalAlignment = Align.Stretch,
            VerticalAlignment = Align.Stretch,
        };
        Tabs.SelectionChanged((_, e) => SelectedIndex = e.NewIndex);
        if (Tabs.Tabs.Count > 0)
        {
            Tabs.SelectedIndex = 0;
        }

        Root = new DockLayout(
            top: null,
            content: Tabs,
            bottom: new HStack(
            [
                new Button(new TextBlock("Submit"))
                    .Click(SubmitOrAdvance),
                new Button(new TextBlock("Cancel"))
                    .Click(() => CancelRequested?.Invoke(this, EventArgs.Empty)),
            ])
            {
                Spacing = 1,
                HorizontalAlignment = Align.End,
            })
        {
            HorizontalAlignment = Align.Stretch,
            VerticalAlignment = Align.Stretch,
            Margin = new Thickness(1),
        };
        AddNavigationCommands(Root);
        Root.KeyDown((_, e) => HandleKeyDown(e));
    }

    public event EventHandler<IReadOnlyList<AltaAskAnswer>>? Submitted;

    public event EventHandler? CancelRequested;

    public Visual Root { get; }

    public TabControl Tabs { get; }

    public int SelectedIndex { get; private set; }

    public IReadOnlyList<bool> Visited => _visited;

    public void Next()
    {
        MarkVisited();
        if (_request.Questions.Count == 0)
        {
            return;
        }

        Tabs.SelectedIndex = Math.Min(Tabs.SelectedIndex + 1, _request.Questions.Count - 1);
        SelectedIndex = Tabs.SelectedIndex;
    }

    public void Previous()
    {
        MarkVisited();
        if (_request.Questions.Count == 0)
        {
            return;
        }

        Tabs.SelectedIndex = Math.Max(Tabs.SelectedIndex - 1, 0);
        SelectedIndex = Tabs.SelectedIndex;
    }

    public void SubmitOrAdvance()
    {
        MarkVisited();
        var nextUnvisited = Array.FindIndex(_visited, static visited => !visited);
        if (nextUnvisited >= 0)
        {
            Tabs.SelectedIndex = nextUnvisited;
            SelectedIndex = nextUnvisited;
            return;
        }

        Submitted?.Invoke(this, CollectAnswers());
    }

    private Visual BuildQuestionPage(AltaAskQuestion question, int index)
    {
        var children = new List<Visual>
        {
            new TextBlock(question.Question ?? question.Title ?? $"Question {index + 1}") { Wrap = true },
        };
        if (!string.IsNullOrWhiteSpace(question.Description))
        {
            children.Add(new TextBlock(question.Description) { Wrap = true });
        }

        var answerChildren = new List<Visual>();
        _choiceBoxes[index] = question.Choices
            .Select((choice, choiceIndex) =>
            {
                var box = new CheckBox($"{choiceIndex + 1}. {choice.Title}");
                answerChildren.Add(box);
                if (!string.IsNullOrWhiteSpace(choice.Description))
                {
                    answerChildren.Add(new TextBlock(choice.Description) { Wrap = true });
                }

                return box;
            })
            .ToArray();

        if (question.Freeform is not null)
        {
            if (!string.IsNullOrWhiteSpace(question.Freeform.Title))
            {
                answerChildren.Add(new TextBlock(question.Freeform.Title) { Wrap = true });
            }

            var freeform = new TextBox()
                .Placeholder(question.Freeform.Placeholder ?? string.Empty)
                .HorizontalAlignment(Align.Stretch);
            _freeformBoxes[index] = freeform;
            answerChildren.Add(freeform);
        }

        children.Add(new ScrollViewer(new VStack(answerChildren.ToArray()) { Spacing = 1 }.Stretch(), focusable: false)
            .HorizontalScrollEnabled(false)
            .VerticalScrollEnabled(true)
            .Stretch());
        return new VStack(children.ToArray())
        {
            Spacing = 1,
            Margin = new Thickness(1, 1, 1, 1),
            HorizontalAlignment = Align.Stretch,
            VerticalAlignment = Align.Stretch,
        };
    }

    private void AddNavigationCommands(Visual visual)
    {
        visual.AddCommand(new Command { Id = "CodeAlta.Ask.Next", LabelMarkup = "Next", DescriptionMarkup = "Next ask question.", Gesture = new KeyGesture(TerminalChar.CtrlN, TerminalModifiers.Ctrl), Execute = _ => Next() });
        visual.AddCommand(new Command { Id = "CodeAlta.Ask.Previous", LabelMarkup = "Previous", DescriptionMarkup = "Previous ask question.", Gesture = new KeyGesture(TerminalChar.CtrlP, TerminalModifiers.Ctrl), Execute = _ => Previous() });
        visual.AddCommand(new Command { Id = "CodeAlta.Ask.SubmitOrAdvance", LabelMarkup = "Submit", DescriptionMarkup = "Validate/advance or submit ask answers.", Gesture = new KeyGesture(TerminalKey.Enter), Execute = _ => SubmitOrAdvance() });
        visual.AddCommand(new Command { Id = "CodeAlta.Ask.Cancel", LabelMarkup = "Cancel", DescriptionMarkup = "Cancel ask mode.", Gesture = new KeyGesture(TerminalKey.Escape), Execute = _ => CancelRequested?.Invoke(this, EventArgs.Empty) });
    }

    private void HandleKeyDown(KeyEventArgs e)
    {
        switch (e.Key)
        {
            case TerminalKey.Right:
                Next();
                e.Handled = true;
                break;
            case TerminalKey.Left:
                Previous();
                e.Handled = true;
                break;
            case TerminalKey.Enter:
                SubmitOrAdvance();
                e.Handled = true;
                break;
            case TerminalKey.Escape:
                CancelRequested?.Invoke(this, EventArgs.Empty);
                e.Handled = true;
                break;
        }
    }

    private void MarkVisited()
    {
        if ((uint)SelectedIndex < (uint)_visited.Length)
        {
            _visited[SelectedIndex] = true;
        }
    }

    private IReadOnlyList<AltaAskAnswer> CollectAnswers()
    {
        var answers = new AltaAskAnswer[_request.Questions.Count];
        for (var index = 0; index < _request.Questions.Count; index++)
        {
            var selectedChoices = _choiceBoxes[index]
                .Select((box, choiceIndex) => box.IsChecked ? choiceIndex : -1)
                .Where(static choiceIndex => choiceIndex >= 0)
                .ToArray();
            answers[index] = new AltaAskAnswer
            {
                QuestionIndex = index,
                SelectedChoiceIndexes = selectedChoices,
                FreeformText = _freeformBoxes[index]?.Text,
            };
        }

        return answers;
    }
}
