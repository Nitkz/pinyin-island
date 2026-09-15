using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using PinyinIsland.Client.Models;

namespace PinyinIsland.Client.Services;

public interface IProgressService
{
    Task<UserProgress> GetProgressAsync();
    Task SaveProgressAsync(UserProgress progress);
    Task CompleteStageAsync(int stageId, int starsEarned);
    Task ResetProgressAsync();
    Task UnlockAllProgressAsync();
    List<IslandInfo> GetAllIslands();
    IslandInfo? GetIsland(int islandId);
    StageInfo? GetStage(int stageId);
    Task<List<PinyinTreasureItem>> GetAllTreasureItemsAsync();
    Task<HashSet<string>> GetUnlockedTreasureLettersAsync(UserProgress progress);
    bool IsBossStage(int stageId);
}

public class ProgressService : IProgressService
{
    private const string StorageKey = "pinyin_island_progress_v1";
    private readonly IJSRuntime _js;
    private readonly HttpClient _http;
    private readonly NavigationManager _nav;
    private UserProgress? _cachedProgress;
    private List<PinyinTreasureItem>? _cachedTreasures;

    public ProgressService(IJSRuntime js, HttpClient http, NavigationManager nav)
    {
        _js = js;
        _http = http;
        _nav = nav;
    }

    public async Task<UserProgress> GetProgressAsync()
    {
        if (_cachedProgress != null)
        {
            return _cachedProgress;
        }

        try
        {
            var json = await _js.InvokeAsync<string?>("localStorage.getItem", StorageKey);
            if (!string.IsNullOrWhiteSpace(json))
            {
                var loaded = JsonSerializer.Deserialize<UserProgress>(json);
                if (loaded != null)
                {
                    _cachedProgress = loaded;
                    EnsureDefaultProgress(_cachedProgress);
                    return _cachedProgress;
                }
            }
        }
        catch
        {
            // If localStorage not accessible or SSR
        }

        _cachedProgress = CreateInitialProgress();
        return _cachedProgress;
    }

    public async Task SaveProgressAsync(UserProgress progress)
    {
        _cachedProgress = progress;
        _cachedProgress.TotalStars = _cachedProgress.StageStars.Values.Sum();
        try
        {
            var json = JsonSerializer.Serialize(progress);
            await _js.InvokeVoidAsync("localStorage.setItem", StorageKey, json);
        }
        catch
        {
            // Silently fallback if localStorage restricted
        }
    }

    public async Task CompleteStageAsync(int stageId, int starsEarned)
    {
        var progress = await GetProgressAsync();
        starsEarned = Math.Clamp(starsEarned, 0, 3);

        if (!progress.StageStars.TryGetValue(stageId, out var existingStars) || starsEarned > existingStars)
        {
            progress.StageStars[stageId] = starsEarned;
        }

        // Unlock next stage (1 to 14)
        if (stageId < 14)
        {
            progress.UnlockedStages.Add(stageId + 1);
        }

        // Unlock islands based on stage milestones
        if (stageId >= 3) progress.UnlockedIslands.Add(2);
        if (stageId >= 7) progress.UnlockedIslands.Add(3);
        if (stageId >= 10) progress.UnlockedIslands.Add(4);

        await SaveProgressAsync(progress);
    }

    public async Task ResetProgressAsync()
    {
        _cachedProgress = CreateInitialProgress();
        try
        {
            await _js.InvokeVoidAsync("localStorage.removeItem", StorageKey);
        }
        catch
        {
            // Silently ignore
        }
    }

    public async Task UnlockAllProgressAsync()
    {
        var all = new UserProgress
        {
            TotalStars = 42,
            UnlockedIslands = new HashSet<int> { 1, 2, 3, 4 },
            UnlockedStages = new HashSet<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14 },
            StageStars = new Dictionary<int, int>()
        };
        for (int i = 1; i <= 14; i++)
        {
            all.StageStars[i] = 3;
        }
        await SaveProgressAsync(all);
    }

    private static UserProgress CreateInitialProgress()
    {
        // Fresh start for a new player: 0 stars, only Island 1 Stage 1 unlocked
        return new UserProgress
        {
            TotalStars = 0,
            UnlockedIslands = new HashSet<int> { 1 },
            UnlockedStages = new HashSet<int> { 1 },
            StageStars = new Dictionary<int, int>()
        };
    }

    private static void EnsureDefaultProgress(UserProgress p)
    {
        p.UnlockedIslands.Add(1);
        p.UnlockedStages.Add(1);
        if (p.StageStars.Count > 0)
        {
            p.TotalStars = p.StageStars.Values.Sum();
        }
    }

    public List<IslandInfo> GetAllIslands() => _islands;

    public IslandInfo? GetIsland(int islandId) => _islands.FirstOrDefault(i => i.Id == islandId);

    public StageInfo? GetStage(int stageId) => _islands.SelectMany(i => i.Stages).FirstOrDefault(s => s.StageId == stageId);

    public bool IsBossStage(int stageId) => stageId is 3 or 7 or 10 or 14;

    public async Task<List<PinyinTreasureItem>> GetAllTreasureItemsAsync()
    {
        if (_cachedTreasures != null && _cachedTreasures.Count > 0)
        {
            return _cachedTreasures;
        }

        try
        {
            var baseUri = _http.BaseAddress?.ToString() ?? _nav.BaseUri;
            if (!baseUri.EndsWith("/")) baseUri += "/";
            var targetUri = new Uri(new Uri(baseUri), "assets/data/pinyin_treasures.json").ToString();

            var items = await _http.GetFromJsonAsync<List<PinyinTreasureItem>>(targetUri);
            if (items != null && items.Count > 0)
            {
                _cachedTreasures = items;
                return _cachedTreasures;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading pinyin_treasures.json via HTTP: {ex.Message}");
        }

        _cachedTreasures = new List<PinyinTreasureItem>();
        return _cachedTreasures;
    }

    public async Task<HashSet<string>> GetUnlockedTreasureLettersAsync(UserProgress progress)
    {
        var items = await GetAllTreasureItemsAsync();
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in items)
        {
            // If user has unlocked or completed the stage where this letter is taught
            if (progress.IsStageUnlocked(item.StageOrigin) || progress.GetStageStars(item.StageOrigin) > 0)
            {
                set.Add(item.Letter);
                set.Add(item.DisplayLetter);
            }
        }
        return set;
    }

    private static readonly List<IslandInfo> _islands = new()
    {
        new IslandInfo
        {
            Id = 1,
            Name = "เกาะป่าผลไม้พินอิน",
            Subtitle = "Fruit Forest Island",
            Description = "พยัญชนะต้นพื้นฐาน b, p, m, f, d, t, n, l",
            ImageUrl = "assets/images/island-1-forest.jpeg",
            ThemeColor = "#43A047",
            BadgeIcon = "🍎",
            Stages = new()
            {
                new StageInfo { StageId = 1, IslandId = 1, Title = "ด่าน 1: ริมหาดผลไม้", FocusChars = "b, p, m, f", Description = "ฝึกฟังเสียงริมฝีปากสุดสนุก", GameMode = "Listen & Tap" },
                new StageInfo { StageId = 2, IslandId = 1, Title = "ด่าน 2: ดงกล้วยหรรษา", FocusChars = "d, t, n, l", Description = "แตะฟังเสียงปลายลิ้นแตะเหงือก", GameMode = "Listen & Tap" },
                new StageInfo { StageId = 3, IslandId = 1, Title = "ด่าน 3: สะพานส้มซ่า", FocusChars = "g, k, h", Description = "เสียงโคนลิ้นสุดตื่นเต้น", GameMode = "Treasure Match" }
            }
        },
        new IslandInfo
        {
            Id = 2,
            Name = "เกาะภูผาหินวิเศษ",
            Subtitle = "Fantasy Rock Mountain",
            Description = "พยัญชนะสุดท้าทาย j, q, x, zh, ch, sh, r, z, c, s, y, w",
            ImageUrl = "assets/images/island-2-mountain.jpeg",
            ThemeColor = "#FB8C00",
            BadgeIcon = "⛰️",
            Stages = new()
            {
                new StageInfo { StageId = 4, IslandId = 2, Title = "ด่าน 4: น้ำตกลิ้นเรียบ", FocusChars = "j, q, x", Description = "เสียงหน้าลิ้นยิ้มกว้างๆ", GameMode = "Listen & Tap" },
                new StageInfo { StageId = 5, IslandId = 2, Title = "ด่าน 5: ยอดเขาม้วนลิ้น", FocusChars = "zh, ch, sh, r", Description = "ห่อลิ้นขึ้นไปแตะเพดานปากแข็ง", GameMode = "Pinyin Train" },
                new StageInfo { StageId = 6, IslandId = 2, Title = "ด่าน 6: ลานหินฟันสู้ฟัน", FocusChars = "z, c, s", Description = "ยิงฟันพ่นลมให้ถูกต้อง", GameMode = "Treasure Match" },
                new StageInfo { StageId = 7, IslandId = 2, Title = "ด่าน 7: ซุ้มประตูคู่หู", FocusChars = "y, w", Description = "พยัญชนะกึ่งสระพาเพลิน", GameMode = "Listen & Tap" }
            }
        },
        new IslandInfo
        {
            Id = 3,
            Name = "หุบเขาสระเดี่ยววิเศษ",
            Subtitle = "Single Vowel Valley",
            Description = "สระเดี่ยวหลัก 6 ตัว a, o, e, i, u, ü",
            ImageUrl = "assets/images/island-3-valley.jpeg",
            ThemeColor = "#039BE5",
            BadgeIcon = "🌊",
            Stages = new()
            {
                new StageInfo { StageId = 8, IslandId = 3, Title = "ด่าน 8: ปากกว้างร้องอ้า", FocusChars = "a, o, e", Description = "อ้าปาก กลมปาก เหยียดปาก", GameMode = "Listen & Tap" },
                new StageInfo { StageId = 9, IslandId = 3, Title = "ด่าน 9: บึงน้ำตาโต", FocusChars = "i, u, ü", Description = "ยิ้มเห็นฟัน ห่อปากจู๋ และทำปากจู๋ตาโต", GameMode = "Treasure Match" },
                new StageInfo { StageId = 10, IslandId = 3, Title = "ด่าน 10: รวมพลัง 6 สระเดี่ยว", FocusChars = "a, o, e, i, u, ü", Description = "ทบทวนสระเดี่ยวครบทั้ง 6 ตัว", GameMode = "Pinyin Train" }
            }
        },
        new IslandInfo
        {
            Id = 4,
            Name = "ถ้ำมหาสมบัติสระผสม",
            Subtitle = "Treasure Volcano Cave",
            Description = "สระผสมและสระนาสิกสุดอัศจรรย์ ai, ei, ui, ao, ou, iu, an, en, in...",
            ImageUrl = "assets/images/island-4-cave.jpeg",
            ThemeColor = "#E53935",
            BadgeIcon = "💎",
            Stages = new()
            {
                new StageInfo { StageId = 11, IslandId = 4, Title = "ด่าน 11: ห้องโถงอัญมณีคู่", FocusChars = "ai, ei, ui, ao", Description = "สระผสมเสียงลื่นไหล", GameMode = "Listen & Tap" },
                new StageInfo { StageId = 12, IslandId = 4, Title = "ด่าน 12: ทางลับลาวา", FocusChars = "ou, iu, ie, üe, er", Description = "สระผสมแปลงร่าง", GameMode = "Treasure Match" },
                new StageInfo { StageId = 13, IslandId = 4, Title = "ด่าน 13: เสียงขึ้นจมูก", FocusChars = "an, en, in, un, ün", Description = "สระนาสิกหน้า เสียงก้องกังวาน", GameMode = "Pinyin Train" },
                new StageInfo { StageId = 14, IslandId = 4, Title = "ด่าน 14: ขุมทรัพย์ราชาพินอิน", FocusChars = "ang, eng, ing, ong", Description = "สระนาสิกหลัง ปลดล็อกหีบสมบัติใหญ่!", GameMode = "Treasure Match" }
            }
        }
    };
}
