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
