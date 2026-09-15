using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using PinyinIsland.Client.Models;

namespace PinyinIsland.Client.Components.GameModes;

public partial class ListenAndTap : ComponentBase
{
    [Inject] private IJSRuntime JS { get; set; } = default!;

    [Parameter] public QuestionItem Question { get; set; } = default!;
    [Parameter] public int IslandId { get; set; } = 1;
    [Parameter] public EventCallback<bool> OnQuestionCompleted { get; set; }

    public string? SelectedOption { get; private set; }
    public string? FailedOption { get; private set; }
    public bool IsSpeakerAnimating { get; private set; }
    public bool IsDisabled { get; private set; }

    private bool _hasFailed = false;
    private int _lastQuestionId = -1;
    private bool _shouldAutoPlay = false;

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
            _shouldAutoPlay = true;
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_shouldAutoPlay)
        {
            _shouldAutoPlay = false;
            await Task.Delay(300);
            await PlayAudio();
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
            else if (!string.IsNullOrWhiteSpace(Question.CorrectAnswer))
            {
                await JS.InvokeVoidAsync("gameAudio.playPinyinAudio", Question.CorrectAnswer);
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

    public string GetThemeClass() => IslandId switch
    {
        1 => "theme-island-1-fruit",
        2 => "theme-island-2-rock",
        3 => "theme-island-3-water",
        4 => "theme-island-4-volcano",
        _ => "theme-island-1-fruit"
    };

    public string GetThemeOrnament(int index) => (IslandId, index % 2) switch
    {
        (1, 0) => "🥥",
        (1, 1) => "🐚",
        (2, 0) => "💎",
        (2, 1) => "🌿",
        (3, 0) => "🪷",
        (3, 1) => "🫧",
        (4, 0) => "🧰",
        (4, 1) => "🪙",
        _ => "✨"
    };
}
