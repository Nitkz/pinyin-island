using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using PinyinIsland.Client.Models;

namespace PinyinIsland.Client.Components.GameModes;

public partial class CardMatch : ComponentBase
{
    [Inject] private IJSRuntime JS { get; set; } = default!;

    [Parameter] public QuestionItem Question { get; set; } = default!;
    [Parameter] public EventCallback<bool> OnQuestionCompleted { get; set; }

    public class MatchCard
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string CardType { get; set; } = "char"; // "char" or "sound"
        public string DisplayText { get; set; } = "";
        public string AudioPath { get; set; } = "";
        public string ThaiSound { get; set; } = "";
        public string MatchKey { get; set; } = "";
        public bool IsFlipped { get; set; } = false;
        public bool IsMatched { get; set; } = false;
    }

    public List<MatchCard> Cards { get; private set; } = new();
    public List<MatchCard> CurrentlyFlippedCards { get; private set; } = new();
    public int MatchedPairsCount { get; private set; } = 0;
    public int TotalRequiredPairs { get; private set; } = 0;

    private bool _isLocked = false;
    private bool _hasFailed = false;
    private int _lastQuestionId = -1;

    protected override void OnParametersSet()
    {
        if (Question != null && Question.QuestionId != _lastQuestionId)
        {
            _lastQuestionId = Question.QuestionId;
            InitCards();
        }
    }

    private void InitCards()
    {
        var rng = new Random();
        Cards = new List<MatchCard>();
        CurrentlyFlippedCards = new List<MatchCard>();
        MatchedPairsCount = 0;
        TotalRequiredPairs = Question.Pairs.Count;
        _isLocked = false;
        _hasFailed = false;

        foreach (var pair in Question.Pairs)
        {
            // Card A: Character card
            Cards.Add(new MatchCard
            {
                CardType = "char",
                DisplayText = pair.Char,
                AudioPath = pair.Audio,
                ThaiSound = pair.Thai,
                MatchKey = pair.Char
            });

            // Card B: Sound/Thai card
            Cards.Add(new MatchCard
            {
                CardType = "sound",
                DisplayText = string.IsNullOrEmpty(pair.Thai) ? "🔊" : pair.Thai,
                AudioPath = pair.Audio,
                ThaiSound = pair.Thai,
                MatchKey = pair.Char
            });
        }

        Cards = Cards.OrderBy(_ => rng.Next()).ToList();
    }

    public async Task TapCard(MatchCard card)
    {
        if (_isLocked || card.IsMatched || card.IsFlipped || CurrentlyFlippedCards.Count >= 2) return;

        // Flip card
        card.IsFlipped = true;
        CurrentlyFlippedCards.Add(card);

        try
        {
            await JS.InvokeVoidAsync("gameAudio.playSfx", "cardFlip");
            if (!string.IsNullOrEmpty(card.AudioPath))
            {
                await JS.InvokeVoidAsync("gameAudio.playVoiceAudio", card.AudioPath);
            }
        }
        catch { }

        StateHasChanged();

        if (CurrentlyFlippedCards.Count == 2)
        {
            var first = CurrentlyFlippedCards[0];
            var second = CurrentlyFlippedCards[1];

            if (first.MatchKey == second.MatchKey && first.CardType != second.CardType)
            {
                // MATCHED!
                first.IsMatched = true;
                second.IsMatched = true;
                MatchedPairsCount++;

                try
                {
                    await JS.InvokeVoidAsync("gameAudio.playSfx", "cardMatch");
                }
                catch { }

                CurrentlyFlippedCards.Clear();
                StateHasChanged();

                if (MatchedPairsCount >= TotalRequiredPairs)
                {
                    _isLocked = true;
                    await Task.Delay(1000);
                    if (OnQuestionCompleted.HasDelegate)
                    {
                        await OnQuestionCompleted.InvokeAsync(!_hasFailed);
                    }
                }
            }
            else
            {
                // NOT MATCHED
                _hasFailed = true;
                _isLocked = true;
                await Task.Delay(900);

                first.IsFlipped = false;
                second.IsFlipped = false;
                CurrentlyFlippedCards.Clear();

                try
                {
                    await JS.InvokeVoidAsync("gameAudio.playSfx", "wrong");
                }
                catch { }

                _isLocked = false;
                StateHasChanged();
            }
        }
    }
}
