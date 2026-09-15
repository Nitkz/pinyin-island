using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace PinyinIsland.Client.Components.Dialogs;

public partial class StageClearDialog : ComponentBase, IAsyncDisposable
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
    public int RevealedStarsCount { get; private set; } = 0;

    public string ChestImageUrl
    {
        get
        {
            var island = Math.Clamp(IslandId, 1, 4);
            var state = IsChestOpen ? "open" : "closed";
            return $"assets/images/chests/chest-island-{island}-{state}.jpeg";
        }
    }

    protected override async Task OnInitializedAsync()
    {
        if (IsBossStage)
        {
            // Start the boss chest unlocking sequence followed by stars & victory BGM
            await TriggerBossChestAnimation();
        }
        else
        {
            // Standard stage: play progressive star pop sounds immediately then victory BGM
            await PlayStarRevealSounds();
        }
    }

    public async Task PlayStarRevealSounds()
    {
        RevealedStarsCount = 0;
        StateHasChanged();

        // Brief breathing delay so dialog appears before first star pops
        await Task.Delay(350);

        for (int i = 1; i <= StarsEarned; i++)
        {
            RevealedStarsCount = i;
            StateHasChanged();
            try
            {
                await JS.InvokeVoidAsync("gameAudio.playStarSound", i);
            }
            catch { }
            await Task.Delay(400);
        }

        if (StarsEarned >= 3)
        {
            await Task.Delay(150);
            try
            {
                await JS.InvokeVoidAsync("gameAudio.playSfx", "cheer");
            }
            catch { }
        }

        // Start celebration Victory BGM after stars and fanfare
        await Task.Delay(200);
        try
        {
            await JS.InvokeVoidAsync("gameAudio.playVictoryBgm", IsBossStage);
        }
        catch { }
    }

    public async Task TriggerBossChestAnimation()
    {
        IsKeyInserted = false;
        IsChestOpen = false;
        ShowSparkles = false;
        StateHasChanged();

        // 1. Key hovers and then glides across the screen, reaching keyhole at 2.3s
        await Task.Delay(2300);
        IsKeyInserted = true;
        StateHasChanged();
        try
        {
            await JS.InvokeVoidAsync("gameAudio.playSfx", "keyUnlock");
        }
        catch { }

        // 2. Key turns 90deg and chest pops open at 3.0s
        await Task.Delay(700);
        IsChestOpen = true;
        ShowSparkles = true;
        StateHasChanged();
        try
        {
            await JS.InvokeVoidAsync("gameAudio.playSfx", "chestOpen");
            await JS.InvokeVoidAsync("gameAudio.playSfx", "coinShower");
        }
        catch { }

        // 3. Reveal stars with progressive chimes after chest opening & start boss celebration BGM
        await Task.Delay(400);
        await PlayStarRevealSounds();
    }

    public async Task OnReplayClicked()
    {
        try
        {
            await JS.InvokeVoidAsync("gameAudio.fadeOutBgm", 300);
        }
        catch { }
        await OnReplay.InvokeAsync();
    }

    public async Task OnNextStageClicked()
    {
        try
        {
            await JS.InvokeVoidAsync("gameAudio.fadeOutBgm", 300);
        }
        catch { }
        await OnNextStage.InvokeAsync();
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            await JS.InvokeVoidAsync("gameAudio.fadeOutBgm", 300);
        }
        catch { }
    }
}
