using System.Text.Json.Serialization;

namespace PinyinIsland.Client.Models;

public class StageRepository
{
    [JsonPropertyName("stages")]
    public List<StageData> Stages { get; set; } = new();
}

public class StageData
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("island_id")]
    public int IslandId { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("focus_chars")]
    public string FocusChars { get; set; } = string.Empty;

    [JsonPropertyName("game_mode")]
    public string GameMode { get; set; } = string.Empty;

    [JsonPropertyName("questions")]
    public List<QuestionData> Questions { get; set; } = new();
}

public class QuestionData
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; } = "listen_pick"; // listen_pick, sequence, match

    [JsonPropertyName("prompt_text")]
    public string? PromptText { get; set; }

    [JsonPropertyName("prompt_audio")]
    public string? PromptAudio { get; set; }

    [JsonPropertyName("thai_sound")]
    public string? ThaiSound { get; set; }

    [JsonPropertyName("target_char")]
    public string? TargetChar { get; set; }

    [JsonPropertyName("display_text")]
    public string? DisplayText { get; set; }

    [JsonPropertyName("options")]
    public object? RawOptions { get; set; }

    [JsonPropertyName("answer")]
    public string? Answer { get; set; }

    [JsonPropertyName("items")]
    public List<string>? Items { get; set; }

    [JsonPropertyName("correct_order")]
    public List<string>? CorrectOrder { get; set; }

    [JsonPropertyName("variants")]
    public List<QuestionVariant>? Variants { get; set; }
}

public class QuestionVariant
{
    [JsonPropertyName("options")]
    public List<string> Options { get; set; } = new();

    [JsonPropertyName("answer")]
    public string Answer { get; set; } = string.Empty;
}

public class OptionWithAudio
{
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("audio")]
    public string Audio { get; set; } = string.Empty;
}
