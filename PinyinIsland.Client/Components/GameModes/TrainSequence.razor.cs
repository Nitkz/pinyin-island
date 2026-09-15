using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using PinyinIsland.Client.Models;

namespace PinyinIsland.Client.Components.GameModes;

public partial class TrainSequence : ComponentBase
{
    [Inject] private IJSRuntime JS { get; set; } = default!;

    [Parameter] public QuestionItem Question { get; set; } = default!;
    [Parameter] public EventCallback<bool> OnQuestionCompleted { get; set; }

    public List<string?> PlacedLetters { get; private set; } = new();
    public List<string> AvailableLetters { get; private set; } = new();
    public bool IsDeparting { get; private set; } = false;
    public bool IsErrorShaking { get; private set; } = false;

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

            try
            {
                await JS.InvokeVoidAsync("gameAudio.playSfx", "blockDrop");
            }
            catch { }

            StateHasChanged();

            if (!PlacedLetters.Contains(null))
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
            IsDeparting = true;

            try
            {
                await JS.InvokeVoidAsync("gameAudio.playSfx", "trainWhistle");
                await JS.InvokeVoidAsync("gameAudio.playSfx", "correct");
            }
            catch { }

            StateHasChanged();

            await Task.Delay(1800);
            if (OnQuestionCompleted.HasDelegate)
            {
                await OnQuestionCompleted.InvokeAsync(!_hasFailed);
            }
        }
        else
        {
            _hasFailed = true;
            _isLocked = true;
            IsErrorShaking = true;

            try
            {
                await JS.InvokeVoidAsync("gameAudio.playSfx", "wrong");
            }
            catch { }

            StateHasChanged();

            await Task.Delay(800);
            IsErrorShaking = false;

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
