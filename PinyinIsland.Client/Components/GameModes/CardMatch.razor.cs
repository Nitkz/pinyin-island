using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using PinyinIsland.Client.Models;

namespace PinyinIsland.Client.Components.GameModes;

public partial class CardMatch : ComponentBase
{
    [Inject] private IJSRuntime JS { get; set; } = default!;

    [Parameter] public QuestionItem Question { get; set; } = default!;
    [Parameter] public int IslandId { get; set; } = 1;
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
    
    // Scoring & Attempts tracking
    public int TotalFlipsCount { get; private set; } = 0;
    public int MistakesCount { get; private set; } = 0;
    public int ComboStreak { get; private set; } = 0;
    public int Score { get; private set; } = 0;
    
    // Celebration Banner
    public string BannerText { get; private set; } = "";
    public bool IsComboActive { get; private set; } = false;
    public int BannerTriggerKey { get; private set; } = 0;

    private bool _isLocked = false;
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
        TotalRequiredPairs = Question.Pairs?.Count ?? 0;
        TotalFlipsCount = 0;
        MistakesCount = 0;
        ComboStreak = 0;
        Score = 0;
        BannerText = "";
        IsComboActive = false;
        BannerTriggerKey = 0;
        _isLocked = false;

        if (Question.Pairs != null)
        {
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
        }

        Cards = Cards.OrderBy(_ => rng.Next()).ToList();
    }

    public async Task TapCard(MatchCard card)
    {
        if (_isLocked || card.IsMatched || card.IsFlipped || CurrentlyFlippedCards.Count >= 2) return;

        // Flip card
        card.IsFlipped = true;
        CurrentlyFlippedCards.Add(card);
        TotalFlipsCount++;

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
                ComboStreak++;

                var pointsEarned = 100 + (ComboStreak > 1 ? (ComboStreak - 1) * 50 : 0);
                Score += pointsEarned;

                if (ComboStreak > 1)
                {
                    BannerText = $"🔥 COMBO x{ComboStreak}! +{pointsEarned} แต้ม 🔥";
                    IsComboActive = true;
                }
                else
                {
                    BannerText = $"✨ จับคู่ถูกต้อง! +{pointsEarned} แต้ม";
                    IsComboActive = false;
                }
                BannerTriggerKey++;

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
                    await Task.Delay(1400);

                    // Fair star criteria for memory cards:
                    // Max allowed mistakes for full credit: max(2, TotalRequiredPairs - 1)
                    var allowedMistakes = Math.Max(2, TotalRequiredPairs - 1);
                    var isFirstTrySuccess = MistakesCount <= allowedMistakes;

                    if (OnQuestionCompleted.HasDelegate)
                    {
                        await OnQuestionCompleted.InvokeAsync(isFirstTrySuccess);
                    }
                }
            }
            else
            {
                // NOT MATCHED
                MistakesCount++;
                ComboStreak = 0;
                _isLocked = true;
                StateHasChanged();

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
