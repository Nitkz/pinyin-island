using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace PinyinIsland.Client.Components.Dialogs;

public partial class StageClearDialog : ComponentBase
{
    [Parameter] public int StarsEarned { get; set; } = 3;
    [Parameter] public int TotalQuestions { get; set; } = 4;
    [Parameter] public int CorrectFirstTry { get; set; } = 4;
    [Parameter] public bool IsBossStage { get; set; } = false;
    [Parameter] public int IslandId { get; set; } = 1;
    [Parameter] public string IslandName { get; set; } = "เกาะมหาสมบัติ";
    [Parameter] public EventCallback OnReplay { get; set; }
    [Parameter] public EventCallback OnNextStage { get; set; }

    [Inject] public IJSRuntime JS { get; set; } = default!;

    public bool IsKeyInserted { get; private set; } = false;
    public bool IsChestOpen { get; private set; } = false;
    public bool ShowSparkles { get; private set; } = false;

    protected override async Task OnInitializedAsync()
    {
        if (IsBossStage)
        {
            // Start the boss chest unlocking sequence
            await TriggerBossChestAnimation();
        }
    }

    public async Task TriggerBossChestAnimation()
    {
        IsKeyInserted = false;
        IsChestOpen = false;
        ShowSparkles = false;
        StateHasChanged();

        // 1. Key flies in and turns
        await Task.Delay(400);
        IsKeyInserted = true;
        StateHasChanged();
        try
        {
            await JS.InvokeVoidAsync("gameAudio.playSfx", "keyUnlock");
        }
        catch { }

        // 2. Chest pops open + Grand fanfare
        await Task.Delay(850);
        IsChestOpen = true;
        ShowSparkles = true;
        StateHasChanged();
        try
        {
            await JS.InvokeVoidAsync("gameAudio.playSfx", "chestOpen");
            await JS.InvokeVoidAsync("gameAudio.playSfx", "coinShower");
        }
        catch { }
    }

    public async Task OnReplayClicked()
    {
        await OnReplay.InvokeAsync();
    }

    public async Task OnNextStageClicked()
    {
        await OnNextStage.InvokeAsync();
    }
}
