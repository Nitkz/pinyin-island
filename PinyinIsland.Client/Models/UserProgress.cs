namespace PinyinIsland.Client.Models;

public class UserProgress
{
    public int TotalStars { get; set; } = 0;
    public HashSet<int> UnlockedIslands { get; set; } = new() { 1 };
    public HashSet<int> UnlockedStages { get; set; } = new() { 1 };
    public Dictionary<int, int> StageStars { get; set; } = new(); // StageId -> Stars (0-3)

    public int GetStageStars(int stageId)
    {
        return StageStars.TryGetValue(stageId, out var stars) ? stars : 0;
    }

    public bool IsIslandUnlocked(int islandId)
    {
        return UnlockedIslands.Contains(islandId);
    }

    public bool IsStageUnlocked(int stageId)
    {
        return UnlockedStages.Contains(stageId);
    }
}
