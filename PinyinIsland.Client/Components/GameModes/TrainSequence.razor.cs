using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using PinyinIsland.Client.Models;

namespace PinyinIsland.Client.Components.GameModes;

public partial class TrainSequence : ComponentBase
{
    [Inject] private IJSRuntime JS { get; set; } = default!;

    [Parameter] public QuestionItem Question { get; set; } = default!;
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
                if (!isLastBlock)
                {
                    // Pronounce the phonetic sound when picked
                    await JS.InvokeVoidAsync("gameAudio.playPinyinAudio", letter);
                }
            }
            catch { }

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
        if (Question == null) return;

        var expectedOrder = Question.CorrectOrder;
        bool isCorrect = true;

        for (int i = 0; i < expectedOrder.Count; i++)
        {
            if (PlacedLetters[i] != expectedOrder[i])
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

            // Give a short pause before starting sequential read-along
            await Task.Delay(400);

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
            // Error: Red signal light, cartoon shake, and return blocks
            _hasFailed = true;
            _isLocked = true;
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

            _isLocked = false;
            StateHasChanged();
        }
    }
}
