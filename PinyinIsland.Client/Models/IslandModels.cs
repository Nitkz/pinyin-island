namespace PinyinIsland.Client.Models;

public class IslandInfo
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string ThemeColor { get; set; } = "#4CAF50";
    public string BadgeIcon { get; set; } = "🏝️";
    public List<StageInfo> Stages { get; set; } = new();
}

public class StageInfo
{
    public int StageId { get; set; }
    public int IslandId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string FocusChars { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string GameMode { get; set; } = "Listen & Tap";
    public int RequiredStarsToUnlock { get; set; } = 0;
}

public class QuestionItem
{
    public int QuestionId { get; set; }
    public string Type { get; set; } = "listen_pick"; // listen_pick, pick_sound, sequence, card_match
    public string TargetLetter { get; set; } = "";
    public string CorrectAnswer { get; set; } = "";
    public string? PromptText { get; set; }
    public string? PromptAudio { get; set; }
    public string? ThaiSound { get; set; }
    public List<string> Options { get; set; } = new();
    public List<string>? OptionsAudio { get; set; }
    public List<string> SequenceItems { get; set; } = new();
    public List<string> CorrectOrder { get; set; } = new();
    public List<CardMatchPair> Pairs { get; set; } = new();
}
