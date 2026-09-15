using System.Text.Json.Serialization;

namespace PinyinIsland.Client.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PinyinCategory
{
    [JsonStringEnumMemberName("initial")]
    Initial,        // พยัญชนะต้น 23 ตัว

    [JsonStringEnumMemberName("single_final")]
    SingleFinal,    // สระเดี่ยว 6 ตัว

    [JsonStringEnumMemberName("compound_final")]
    CompoundFinal   // สระผสมและสระนาสิก 18 ตัว
}

public class PinyinTreasureItem
{
    [JsonPropertyName("letter")]
    public string Letter { get; set; } = string.Empty;

    [JsonPropertyName("display_letter")]
    public string DisplayLetter { get; set; } = string.Empty;

    [JsonPropertyName("category")]
    public PinyinCategory Category { get; set; }

    [JsonPropertyName("stage_origin")]
    public int StageOrigin { get; set; }

    [JsonPropertyName("island_origin")]
    public int IslandOrigin { get; set; }

    [JsonPropertyName("thai_sound")]
    public string ThaiSound { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonIgnore]
    public string CategoryName => Category switch
    {
        PinyinCategory.Initial => "พยัญชนะต้น",
        PinyinCategory.SingleFinal => "สระเดี่ยว",
        PinyinCategory.CompoundFinal => "สระผสม/นาสิก",
        _ => "พินอิน"
    };

    [JsonIgnore]
    public string BadgeEmoji => Category switch
    {
        PinyinCategory.Initial => "🪙",
        PinyinCategory.SingleFinal => "💎",
        PinyinCategory.CompoundFinal => "✨",
        _ => "⭐"
    };
}
