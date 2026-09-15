using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using PinyinIsland.Client.Models;
using PinyinIsland.Client.Services;

namespace PinyinIsland.Client.Pages;

public partial class GamePlay : ComponentBase, IAsyncDisposable
{
    [Parameter] public int StageId { get; set; } = 1;

    [Inject] public IProgressService ProgressService { get; set; } = default!;
    [Inject] public IStageDataService StageDataService { get; set; } = default!;
    [Inject] public NavigationManager Nav { get; set; } = default!;
    [Inject] public IJSRuntime JS { get; set; } = default!;
    [Inject] public ISnackbar Snackbar { get; set; } = default!;

    public enum GamePhase { Learn, Quiz, Evaluate, Clear }
    public GamePhase CurrentPhase { get; private set; } = GamePhase.Learn;

    public StageInfo? Stage { get; private set; }
    public StageData? StageDetails { get; private set; }
    public List<string> LearnLetters { get; private set; } = new();
    public int CurrentLearnVideoIndex { get; private set; } = 0;
    public string CurrentLearnLetter => LearnLetters.Count > CurrentLearnVideoIndex ? LearnLetters[CurrentLearnVideoIndex] : "b";
    public bool IsManualLetterSelected { get; private set; } = false;
    public bool HasWatchedAllVideos { get; private set; } = false;

    public List<QuestionItem> Questions { get; private set; } = new();
    public int CurrentQuestionIndex { get; private set; } = 0;
    public QuestionItem? CurrentQuestion => Questions.Count > CurrentQuestionIndex ? Questions[CurrentQuestionIndex] : null;

    // Quiz Phase state
    public string? SelectedOption { get; private set; } = null;
    public string? FailedOption { get; private set; } = null;
    public bool IsSpeakerAnimating { get; private set; } = false;
    public bool IsOptionDisabled { get; private set; } = false;
    public bool IsLockedForTransition { get; private set; } = false;
    public int TotalStarsEarnedInStage { get; private set; } = 0;
    public int CorrectFirstTryCount { get; private set; } = 0;
    public bool HasFailedCurrentQuestion { get; private set; } = false;
    public int CalculatedFinalStars { get; private set; } = 3;

    private int _lastLoadedStageId = -1;
    private bool _shouldAutoPlayFirstVideo = false;
    private bool _shouldAutoPlayFirstQuestion = false;

    public class QuestionItem
    {
        public int QuestionId { get; set; }
        public string TargetLetter { get; set; } = "";
        public string CorrectAnswer { get; set; } = "";
        public string? PromptAudio { get; set; }
        public string? ThaiSound { get; set; }
        public List<string> Options { get; set; } = new();
    }

    protected override async Task OnInitializedAsync()
    {
        await LoadStageDataAsync();
    }

    protected override async Task OnParametersSetAsync()
    {
        if (StageId != _lastLoadedStageId)
        {
            await LoadStageDataAsync();
        }
    }

    private async Task LoadStageDataAsync()
    {
        _lastLoadedStageId = StageId;
        Stage = ProgressService.GetStage(StageId);
        if (Stage == null)
        {
            Snackbar.Add("ไม่พบข้อมูลด่านนี้ กำลังพากลับไปหน้าแผนที่...", Severity.Warning);
            Nav.NavigateTo("/map");
            return;
        }

        StageDetails = await StageDataService.GetStageDataAsync(StageId);
        CurrentPhase = GamePhase.Learn;
        _shouldAutoPlayFirstVideo = true;
        SetupLearnLettersAndQuestions();
        StateHasChanged();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if ((firstRender || _shouldAutoPlayFirstVideo) && CurrentPhase == GamePhase.Learn && LearnLetters.Count > 0)
        {
            _shouldAutoPlayFirstVideo = false;
            await Task.Delay(200);
            await JS.InvokeVoidAsync("gameAudio.playLetterVideo", CurrentLearnVideoIndex, LearnLetters.Count);
        }

        if (_shouldAutoPlayFirstQuestion && CurrentPhase == GamePhase.Quiz)
        {
            _shouldAutoPlayFirstQuestion = false;
            await Task.Delay(350);
            await PlayCurrentQuestionVoice();
        }
    }

    public string GetVideoUrlForLetter(string letter)
    {
        var clean = letter.ToLower().Trim();
        var finals = new HashSet<string> { "a", "o", "e", "i", "u", "v", "ü", "ai", "ei", "ui", "ao", "ou", "iu", "ie", "ue", "üe", "er", "an", "en", "in", "un", "vn", "ün", "ang", "eng", "ing", "ong" };
        var folder = finals.Contains(clean) ? "finals" : "initials";
        if (clean == "ü") clean = "v";
        if (clean == "üe") clean = "ue";
        if (clean == "ün") clean = "vn";

        return $"assets/videos/{folder}/{clean}.mp4";
    }

    public void SetupLearnLettersAndQuestions()
    {
        LearnLetters = (Stage?.FocusChars ?? "b, p, m, f")
            .Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries)
            .ToList();

        if (LearnLetters.Count == 0) LearnLetters = new() { "b", "p", "m", "f" };

        CurrentLearnVideoIndex = 0;
        IsManualLetterSelected = false;
        HasWatchedAllVideos = false;

        var rng = new Random();
        Questions = new List<QuestionItem>();

        // Check if we have repository questions from stages.json
        if (StageDetails != null && StageDetails.Questions.Count > 0)
        {
            var quizQuestions = StageDetails.Questions
                .Where(q => q.Type == "listen_pick" || q.Type == "pick_sound")
                .ToList();

            foreach (var q in quizQuestions)
            {
                var optionsList = new List<string>();
                var answer = q.Answer ?? q.TargetChar ?? "";

                // Anti-memorization: 50% chance to pick a variant if available
                if (q.Variants != null && q.Variants.Count > 0 && rng.Next(2) == 1)
                {
                    var variant = q.Variants[rng.Next(q.Variants.Count)];
                    optionsList = new List<string>(variant.Options);
                    answer = variant.Answer;
                }
                else if (q.Options.Count > 0)
                {
                    optionsList = new List<string>(q.Options);
                }

                if (optionsList.Count == 0 && !string.IsNullOrEmpty(q.TargetChar))
                {
                    optionsList = new List<string> { q.TargetChar };
                    var otherChars = LearnLetters.Where(c => c != q.TargetChar).ToList();
                    optionsList.AddRange(otherChars.OrderBy(_ => rng.Next()).Take(2));
                }

                // Shuffle option placement so answer position is dynamic
                optionsList = optionsList.OrderBy(_ => rng.Next()).ToList();

                Questions.Add(new QuestionItem
                {
                    QuestionId = q.Id,
                    TargetLetter = q.TargetChar ?? answer,
                    CorrectAnswer = answer,
                    PromptAudio = q.PromptAudio,
                    ThaiSound = q.ThaiSound,
                    Options = optionsList
                });
            }
        }

        // Fallback generator if needed
        if (Questions.Count == 0)
        {
            foreach (var letter in LearnLetters)
            {
                var otherPool = LearnLetters.Where(c => c != letter).ToList();
                if (otherPool.Count < 2)
                {
                    otherPool.AddRange(new[] { "b", "p", "m", "f", "d", "t", "n", "l" }.Where(x => x != letter && !otherPool.Contains(x)));
                }

                var shuffledDistractors = otherPool.OrderBy(_ => rng.Next()).Take(2).ToList();
                var options = new List<string> { letter };
                options.AddRange(shuffledDistractors);
                options = options.OrderBy(_ => rng.Next()).ToList();

                Questions.Add(new QuestionItem
                {
                    QuestionId = 0,
                    TargetLetter = letter,
                    CorrectAnswer = letter,
                    Options = options
                });
            }
        }

        Questions = Questions.OrderBy(_ => rng.Next()).ToList();
        CurrentQuestionIndex = 0;
        CorrectFirstTryCount = 0;
    }

    public async Task OnLearnVideoEnded(int finishedIndex)
    {
        // Only handle when the currently active video finishes
        if (finishedIndex != CurrentLearnVideoIndex) return;

        // If user manually selected this letter, don't auto advance
        if (IsManualLetterSelected)
        {
            return;
        }

        if (CurrentLearnVideoIndex + 1 < LearnLetters.Count)
        {
            CurrentLearnVideoIndex++;
            StateHasChanged();
            await JS.InvokeVoidAsync("gameAudio.playLetterVideo", CurrentLearnVideoIndex, LearnLetters.Count);
        }
        else
        {
            // All videos finished in initial stage playlist! Wait for user to start quiz
            HasWatchedAllVideos = true;
            StateHasChanged();
        }
    }

    public async Task SelectLearnVideo(int index)
    {
        if (index >= 0 && index < LearnLetters.Count)
        {
            IsManualLetterSelected = true;
            CurrentLearnVideoIndex = index;
            StateHasChanged();
            await JS.InvokeVoidAsync("gameAudio.playLetterVideo", CurrentLearnVideoIndex, LearnLetters.Count);
        }
    }

    public async Task ReplayAllLearnVideos()
    {
        try
        {
            await JS.InvokeVoidAsync("gameAudio.playSfx", "tap");
        }
        catch { }
        IsManualLetterSelected = false;
        CurrentLearnVideoIndex = 0;
        HasWatchedAllVideos = false;
        StateHasChanged();
        await JS.InvokeVoidAsync("gameAudio.playLetterVideo", CurrentLearnVideoIndex, LearnLetters.Count);
    }

    public async Task StartQuizPhase()
    {
        try
        {
            await JS.InvokeVoidAsync("gameAudio.pauseAllLetterVideos", LearnLetters.Count);
            await JS.InvokeVoidAsync("gameAudio.playSfx", "tap");
        }
        catch { }

        CurrentPhase = GamePhase.Quiz;
        HasFailedCurrentQuestion = false;
        _shouldAutoPlayFirstQuestion = true;
        StateHasChanged();
    }

    public async Task SkipLearnPhase()
    {
        await StartQuizPhase();
    }

    public async Task PlayCurrentQuestionVoice()
    {
        var target = CurrentQuestion?.TargetLetter;
        if (string.IsNullOrWhiteSpace(target)) return;

        IsSpeakerAnimating = true;
        StateHasChanged();

        try
        {
            if (!string.IsNullOrEmpty(CurrentQuestion?.PromptAudio))
            {
                // Play explicit prompt audio if provided in JSON repository
                await JS.InvokeVoidAsync("gameAudio.playVoiceAudio", CurrentQuestion.PromptAudio);
            }
            else if (!string.IsNullOrWhiteSpace(CurrentQuestion?.TargetLetter))
            {
                await JS.InvokeVoidAsync("gameAudio.playPinyinAudio", CurrentQuestion.TargetLetter);
            }
        }
        catch { }

        await Task.Delay(600);
        IsSpeakerAnimating = false;
        StateHasChanged();
    }

    public async Task CheckAnswer(string option)
    {
        if (IsLockedForTransition || CurrentQuestion == null) return;

        // Ensure case-insensitive trimmed comparison for robust answer validation
        if (string.Equals(option?.Trim(), CurrentQuestion.CorrectAnswer?.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            // CORRECT!
            SelectedOption = option;
            FailedOption = null;
            IsLockedForTransition = true;
            CurrentPhase = GamePhase.Evaluate;

            if (!HasFailedCurrentQuestion)
            {
                CorrectFirstTryCount++;
            }

            try
            {
                await JS.InvokeVoidAsync("gameAudio.playSfx", "correct");
            }
            catch { }

            StateHasChanged();

            // Transition to next question after 1.2s
            await Task.Delay(1200);

            SelectedOption = null;
            FailedOption = null;
            IsLockedForTransition = false;
            HasFailedCurrentQuestion = false;

            if (CurrentQuestionIndex + 1 < Questions.Count)
            {
                CurrentQuestionIndex++;
                CurrentPhase = GamePhase.Quiz;
                StateHasChanged();
                await Task.Delay(350);
                await PlayCurrentQuestionVoice();
            }
            else
            {
                // Finished all questions! Trigger Stage Clear
                await CompleteStageAndShowClearDialog();
            }
        }
        else
        {
            // WRONG (Kid-friendly bounce feedback)
            FailedOption = option;
            HasFailedCurrentQuestion = true;

            try
            {
                await JS.InvokeVoidAsync("gameAudio.playSfx", "wrong");
            }
            catch { }

            StateHasChanged();

            // Clear shake state after 500ms so kid can tap again
            await Task.Delay(500);
            FailedOption = null;
            StateHasChanged();
        }
    }

    public async Task CompleteStageAndShowClearDialog()
    {
        // Star calculation: 3 stars if >= 75% first try, 2 stars if >= 50%, 1 star minimum
        var ratio = (double)CorrectFirstTryCount / Math.Max(1, Questions.Count);
        if (ratio >= 0.75) CalculatedFinalStars = 3;
        else if (ratio >= 0.5) CalculatedFinalStars = 2;
        else CalculatedFinalStars = 1;

        TotalStarsEarnedInStage = CalculatedFinalStars;
        await ProgressService.CompleteStageAsync(StageId, CalculatedFinalStars);

        CurrentPhase = GamePhase.Clear;
        StateHasChanged();

        try
        {
            await JS.InvokeVoidAsync("gameAudio.playSfx", "cheer");
        }
        catch { }
    }

    public async Task ReplayStage()
    {
        try
        {
            await JS.InvokeVoidAsync("gameAudio.playSfx", "tap");
        }
        catch { }

        SetupLearnLettersAndQuestions();
        CurrentPhase = GamePhase.Quiz;
        SelectedOption = null;
        FailedOption = null;
        IsLockedForTransition = false;
        HasFailedCurrentQuestion = false;
        _shouldAutoPlayFirstQuestion = true;
        StateHasChanged();
    }

    public async Task GoToNextStage()
    {
        try
        {
            await JS.InvokeVoidAsync("gameAudio.playSfx", "tap");
        }
        catch { }

        var nextStageId = StageId + 1;
        if (nextStageId <= 14)
        {
            Nav.NavigateTo($"/play/{nextStageId}");
        }
        else
        {
            Nav.NavigateTo("/map");
        }
    }

    public void GoBackToMap()
    {
        Nav.NavigateTo("/map");
    }

    public string GetOptionCardStateClass(string option)
    {
        if (string.Equals(SelectedOption, option, StringComparison.OrdinalIgnoreCase)) return "card-correct";
        if (string.Equals(FailedOption, option, StringComparison.OrdinalIgnoreCase)) return "card-wrong";
        return "";
    }

    public (string Bg, string Border, string Shadow, string Text) GetPastelCardColor(int index)
    {
        return (index % 3) switch
        {
            0 => ("linear-gradient(180deg, #FFF0F5 0%, #FCE4EC 100%)", "#F48FB1", "#C2185B", "#880E4F"), // Soft Pink
            1 => ("linear-gradient(180deg, #E1F5FE 0%, #B3E5FC 100%)", "#81D4FA", "#0288D1", "#01579B"), // Sky Blue
            2 => ("linear-gradient(180deg, #FFFDE7 0%, #FFF9C4 100%)", "#FFE082", "#FFA000", "#E65100"), // Sun Yellow
            _ => ("linear-gradient(180deg, #E8F5E9 0%, #C8E6C9 100%)", "#A5D6A7", "#388E3C", "#1B5E20")  // Forest Green
        };
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            await JS.InvokeVoidAsync("gameAudio.pauseAllLetterVideos", LearnLetters.Count);
        }
        catch { }
    }
}
