using Microsoft.AspNetCore.Components;

namespace PinyinIsland.Client.Components.Dialogs;

public partial class StageClearDialog : ComponentBase
{
    [Parameter] public int StarsEarned { get; set; } = 3;
    [Parameter] public int TotalQuestions { get; set; } = 4;
    [Parameter] public int CorrectFirstTry { get; set; } = 4;
    [Parameter] public EventCallback OnReplay { get; set; }
    [Parameter] public EventCallback OnNextStage { get; set; }

    public async Task OnReplayClicked()
    {
        await OnReplay.InvokeAsync();
    }

    public async Task OnNextStageClicked()
    {
        await OnNextStage.InvokeAsync();
    }
}
