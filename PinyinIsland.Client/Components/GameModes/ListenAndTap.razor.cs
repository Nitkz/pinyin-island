using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using PinyinIsland.Client.Models;

namespace PinyinIsland.Client.Components.GameModes;

public partial class ListenAndTap : ComponentBase
{
    [Inject] private IJSRuntime JS { get; set; } = default!;

    [Parameter] public QuestionItem Question { get; set; } = default!;
    [Parameter] public EventCallback<bool> OnQuestionCompleted { get; set; }

    public string? SelectedOption { get; private set; }
    public string? FailedOption { get; private set; }
    public bool IsSpeakerAnimating { get; private set; }
    public bool IsDisabled { get; private set; }

    private bool _hasFailed = false;
    private int _lastQuestionId = -1;

    protected override void OnParametersSet()
    {
        if (Question != null && Question.QuestionId != _lastQuestionId)
        {
            _lastQuestionId = Question.QuestionId;
            SelectedOption = null;
            FailedOption = null;
            IsSpeakerAnimating = false;
            IsDisabled = false;
            _hasFailed = false;
        }
    }

    public async Task SelectOption(string option)
    {
        if (IsDisabled || Question == null) return;

        SelectedOption = option;

        if (string.Equals(option, Question.CorrectAnswer, StringComparison.OrdinalIgnoreCase))
        {
            // CORRECT
            IsDisabled = true;

            try
            {
                await JS.InvokeVoidAsync("gameAudio.playSfx", "correct");
            }
            catch { }

            StateHasChanged();

            await Task.Delay(1200);
            if (OnQuestionCompleted.HasDelegate)
            {
                await OnQuestionCompleted.InvokeAsync(!_hasFailed);
            }
        }
        else
        {
            // WRONG
            FailedOption = option;
            _hasFailed = true;

            try
            {
                await JS.InvokeVoidAsync("gameAudio.playSfx", "wrong");
            }
            catch { }

            StateHasChanged();

            await Task.Delay(500);
            FailedOption = null;
            StateHasChanged();
        }
    }

    public async Task PlayAudio()
    {
        if (Question == null) return;

        IsSpeakerAnimating = true;
        StateHasChanged();

        try
        {
            if (!string.IsNullOrEmpty(Question.PromptAudio))
            {
                await JS.InvokeVoidAsync("gameAudio.playVoiceAudio", Question.PromptAudio);
            }
            else if (!string.IsNullOrWhiteSpace(Question.TargetLetter))
            {
                await JS.InvokeVoidAsync("gameAudio.playPinyinAudio", Question.TargetLetter);
            }
        }
        catch { }

        await Task.Delay(800);
        IsSpeakerAnimating = false;
        StateHasChanged();
    }

    public string GetOptionCardStateClass(string option)
    {
        if (string.Equals(SelectedOption, option, StringComparison.OrdinalIgnoreCase) && string.Equals(option, Question?.CorrectAnswer, StringComparison.OrdinalIgnoreCase))
        {
            return "card-correct";
        }
        if (string.Equals(FailedOption, option, StringComparison.OrdinalIgnoreCase))
        {
            return "card-wrong";
        }
        return "";
    }

    public (string Bg, string Border, string Shadow, string Text) GetPastelCardColor(int index)
    {
        return (index % 4) switch
        {
            0 => ("linear-gradient(180deg, #FFF0F5 0%, #FCE4EC 100%)", "#F48FB1", "#C2185B", "#880E4F"),
            1 => ("linear-gradient(180deg, #E1F5FE 0%, #B3E5FC 100%)", "#81D4FA", "#0288D1", "#01579B"),
            2 => ("linear-gradient(180deg, #FFFDE7 0%, #FFF9C4 100%)", "#FFE082", "#FFA000", "#E65100"),
            _ => ("linear-gradient(180deg, #E8F5E9 0%, #C8E6C9 100%)", "#A5D6A7", "#388E3C", "#1B5E20")
        };
    }
}
