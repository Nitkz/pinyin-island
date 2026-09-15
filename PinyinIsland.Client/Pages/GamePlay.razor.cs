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

    // Linear vs Shuffle Play Mode
    public UserProgress? UserProgress { get; private set; }
    public bool IsShuffleMode { get; private set; } = false;
    public bool CanPlayShuffleMode => UserProgress != null && UserProgress.GetStageStars(StageId) >= 3;

    public List<QuestionItem> Questions { get; private set; } = new();
    public int CurrentQuestionIndex { get; private set; } = 0;
    public QuestionItem? CurrentQuestion => Questions.Count > CurrentQuestionIndex ? Questions[CurrentQuestionIndex] : null;

    // Progression & Scoring
    public int TotalStarsEarnedInStage { get; private set; } = 0;
    public int CorrectFirstTryCount { get; private set; } = 0;
    public int CalculatedFinalStars { get; private set; } = 3;

    private int _lastLoadedStageId = -1;
    private bool _shouldAutoPlayFirstVideo = false;

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
        UserProgress = await ProgressService.GetProgressAsync();
        CurrentPhase = GamePhase.Learn;
        _shouldAutoPlayFirstVideo = true;
        SetupLearnLettersAndQuestions(isShuffle: false);
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

    public void SetupLearnLettersAndQuestions(bool isShuffle = false)
    {
        IsShuffleMode = isShuffle;
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
            foreach (var q in StageDetails.Questions)
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

                // Shuffle option placement so answer position is dynamic
                optionsList = optionsList.OrderBy(_ => rng.Next()).ToList();

                var qItem = new QuestionItem
                {
                    QuestionId = q.Id,
                    Type = q.Type,
                    TargetLetter = q.TargetChar ?? answer,
                    CorrectAnswer = answer,
                    PromptText = q.PromptText,
                    PromptAudio = q.PromptAudio,
                    ThaiSound = q.ThaiSound,
                    Options = optionsList,
                    OptionsAudio = q.OptionsAudio,
                    SequenceItems = q.Items != null ? new List<string>(q.Items) : new List<string>(),
                    CorrectOrder = q.CorrectOrder != null ? new List<string>(q.CorrectOrder) : (q.Items != null ? new List<string>(q.Items) : new List<string>()),
                    Pairs = q.Pairs != null ? new List<CardMatchPair>(q.Pairs) : new List<CardMatchPair>()
                };

                Questions.Add(qItem);
            }
        }

        // If in Shuffle Mode: Randomize question list order and take up to 5 questions
        if (isShuffle)
        {
            Questions = Questions.OrderBy(_ => rng.Next()).Take(5).ToList();
        }

        CurrentQuestionIndex = 0;
        CorrectFirstTryCount = 0;
    }

    public async Task StartShufflePlay()
    {
        if (!CanPlayShuffleMode) return;

        try
        {
            await JS.InvokeVoidAsync("gameAudio.pauseAllLetterVideos", LearnLetters.Count);
            await JS.InvokeVoidAsync("gameAudio.playSfx", "shuffle");
        }
        catch { }

        SetupLearnLettersAndQuestions(isShuffle: true);
        CurrentPhase = GamePhase.Quiz;
        StateHasChanged();
    }

    public async Task OnLearnVideoEnded(int finishedIndex)
    {
        if (finishedIndex != CurrentLearnVideoIndex) return;

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
        StateHasChanged();
    }

    public async Task PlayCurrentQuestionVoice()
    {
        var target = CurrentQuestion?.TargetLetter;
        if (string.IsNullOrWhiteSpace(target) && string.IsNullOrWhiteSpace(CurrentQuestion?.PromptAudio)) return;

        try
        {
            if (!string.IsNullOrEmpty(CurrentQuestion?.PromptAudio))
            {
                await JS.InvokeVoidAsync("gameAudio.playVoiceAudio", CurrentQuestion.PromptAudio);
            }
            else if (!string.IsNullOrWhiteSpace(CurrentQuestion?.TargetLetter))
            {
                await JS.InvokeVoidAsync("gameAudio.playPinyinAudio", CurrentQuestion.TargetLetter);
            }
        }
        catch { }
    }

    public async Task HandleQuestionCompleted(bool isFirstTryCorrect)
    {
        if (isFirstTryCorrect)
        {
            CorrectFirstTryCount++;
        }

        await AdvanceToNextQuestion();
    }

    private async Task AdvanceToNextQuestion()
    {
        if (CurrentQuestionIndex + 1 < Questions.Count)
        {
            CurrentQuestionIndex++;
            CurrentPhase = GamePhase.Quiz;
            StateHasChanged();
        }
        else
        {
            await CompleteStageAndShowClearDialog();
        }
    }

    public async Task CompleteStageAndShowClearDialog()
    {
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

        SetupLearnLettersAndQuestions(isShuffle: false);
        CurrentPhase = GamePhase.Quiz;
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

    public async ValueTask DisposeAsync()
    {
        try
        {
            await JS.InvokeVoidAsync("gameAudio.pauseAllLetterVideos", LearnLetters.Count);
        }
        catch { }
    }
}
