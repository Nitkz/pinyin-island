using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using PinyinIsland.Client.Models;

namespace PinyinIsland.Client.Components.GameModes;

public partial class TrainSequence : ComponentBase, IAsyncDisposable
{
    [Inject] private IJSRuntime JS { get; set; } = default!;

    [Parameter] public QuestionItem Question { get; set; } = default!;
    [Parameter] public int IslandId { get; set; } = 1;
    [Parameter] public EventCallback<bool> OnQuestionCompleted { get; set; }

    public enum TrafficLightState { Yellow, Green, Red }
    public TrafficLightState CurrentLight { get; private set; } = TrafficLightState.Yellow;

    public List<string?> PlacedLetters { get; private set; } = new();
    public List<string> AvailableLetters { get; private set; } = new();
    public bool IsDeparting { get; private set; } = false;
    public bool IsErrorShaking { get; private set; } = false;
    public int CurrentlyReadingSlotIndex { get; private set; } = -1;

    private bool _isLocked = false;
    private bool _hasFailed = false;
    private int _lastQuestionId = -1;

    protected override void OnParametersSet()
    {
        if (Question != null && Question.QuestionId != _lastQuestionId)
        {
            _lastQuestionId = Question.QuestionId;
            InitSequence();
        }
    }

    private void InitSequence()
    {
        var rng = new Random();
        var items = Question.SequenceItems.Count > 0 ? Question.SequenceItems : Question.CorrectOrder;
        AvailableLetters = items.OrderBy(_ => rng.Next()).ToList();
        PlacedLetters = new List<string?>(new string?[Question.CorrectOrder.Count]);
        IsDeparting = false;
        IsErrorShaking = false;
        CurrentlyReadingSlotIndex = -1;
        CurrentLight = TrafficLightState.Yellow;
        _isLocked = false;
        _hasFailed = false;
    }

    private IJSObjectReference? _trainModule;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            try
            {
                _trainModule = await JS.InvokeAsync<IJSObjectReference>("import", "./js/trainSequence.js");
            }
            catch { }
        }
    }

    private async Task ScrollToSlot(int slotIndex)
    {
        if (_trainModule != null)
        {
            try
            {
                await _trainModule.InvokeVoidAsync("scrollToWagonSlot", slotIndex);
            }
            catch { }
        }
    }

    private async Task ScrollToFront()
    {
        if (_trainModule != null)
        {
            try
            {
                await _trainModule.InvokeVoidAsync("scrollToTrainFront");
            }
            catch { }
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_trainModule != null)
        {
            try
            {
                await _trainModule.DisposeAsync();
            }
            catch { }
        }
    }

    public async Task PlaceBlock(string letter)
    {
        if (_isLocked || !AvailableLetters.Contains(letter)) return;

        var emptySlotIndex = PlacedLetters.IndexOf(null);
        if (emptySlotIndex >= 0)
        {
            AvailableLetters.Remove(letter);
            PlacedLetters[emptySlotIndex] = letter;

            var isLastBlock = !PlacedLetters.Contains(null);

            try
            {
                await JS.InvokeVoidAsync("gameAudio.playSfx", "blockDrop");
                // Pronounce the phonetic sound when picked (for every block)
                await JS.InvokeVoidAsync("gameAudio.playPinyinAudio", letter);
            }
            catch { }

            // Auto scroll the placed wagon slot smoothly into viewport
            await ScrollToSlot(emptySlotIndex);

            StateHasChanged();

            if (isLastBlock)
            {
                await CheckTrainSequence();
            }
        }
    }

    public async Task RemoveBlock(int slotIndex)
    {
        if (_isLocked || slotIndex < 0 || slotIndex >= PlacedLetters.Count) return;

        var letter = PlacedLetters[slotIndex];
        if (letter != null)
        {
            PlacedLetters[slotIndex] = null;
            AvailableLetters.Add(letter);

            try
            {
                await JS.InvokeVoidAsync("gameAudio.playSfx", "tap");
            }
            catch { }

            StateHasChanged();
        }
    }

    private async Task CheckTrainSequence()
    {
        var isCorrect = true;
        for (int i = 0; i < Question.CorrectOrder.Count; i++)
        {
            if (PlacedLetters[i] != Question.CorrectOrder[i])
            {
                isCorrect = false;
                break;
            }
        }

        if (isCorrect)
        {
            _isLocked = true;
            CurrentLight = TrafficLightState.Green;
            StateHasChanged();

            // Give sufficient pause for the last placed block's pronunciation to complete cleanly before read-along starts
            await Task.Delay(900);

            // Step 1: Read-Along Sequential Sound (each wagon lights up and speaks completely with no mid-word cutoff!)
            for (int i = 0; i < PlacedLetters.Count; i++)
            {
                var letter = PlacedLetters[i];
                if (!string.IsNullOrEmpty(letter))
                {
                    CurrentlyReadingSlotIndex = i;
                    StateHasChanged();

                    try
                    {
                        await ScrollToSlot(i);
                        await JS.InvokeVoidAsync("gameAudio.playPinyinAudio", letter);
                    }
                    catch { }

                    // Allow full 1000ms for pinyin audio to finish cleanly without clipping
                    await Task.Delay(1000);
                }
            }

            CurrentlyReadingSlotIndex = -1;
            StateHasChanged();

            // Step 2: Play Train Whistle, then victory chime & departure!
            try
            {
                await JS.InvokeVoidAsync("gameAudio.playSfx", "trainWhistle");
            }
            catch { }

            // Delay for whistle to ring out before victory Ding-Dong
            await Task.Delay(650);

            try
            {
                await JS.InvokeVoidAsync("gameAudio.playSfx", "correct");
            }
            catch { }

            // Step 3: Train departs driving forward
            IsDeparting = true;
            StateHasChanged();

            await Task.Delay(1800);
            if (OnQuestionCompleted.HasDelegate)
            {
                await OnQuestionCompleted.InvokeAsync(!_hasFailed);
            }
        }
        else
        {
            // Lock interaction first
            _hasFailed = true;
            _isLocked = true;
            StateHasChanged();

            // Allow the last placed block's pronunciation to finish cleanly before buzzer/shake
            await Task.Delay(750);

            // Error: Red signal light, cartoon shake, and play buzzer
            CurrentLight = TrafficLightState.Red;
            IsErrorShaking = true;

            try
            {
                await JS.InvokeVoidAsync("gameAudio.playSfx", "wrong");
            }
            catch { }

            StateHasChanged();

            await Task.Delay(900);
            IsErrorShaking = false;
            CurrentLight = TrafficLightState.Yellow;

            // Reset blocks back to available
            for (int i = 0; i < PlacedLetters.Count; i++)
            {
                if (PlacedLetters[i] != null)
                {
                    AvailableLetters.Add(PlacedLetters[i]!);
                    PlacedLetters[i] = null;
                }
            }

            // Scroll back smoothly to front of train
            await ScrollToFront();

            _isLocked = false;
            StateHasChanged();
        }
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
