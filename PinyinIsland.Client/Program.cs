using System.Runtime.InteropServices.JavaScript;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using PinyinIsland.Client;
using PinyinIsland.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

if (OperatingSystem.IsBrowser())
{
    try
    {
        await JSHost.ImportAsync("interop", "./js/interop.js");
        if (NativeInterop.IsStandalone())
        {
            builder.RootComponents.Add<Routes>("#app");
            builder.RootComponents.Add<HeadOutlet>("head::after");
        }
    }
    catch
    {
        // In Blazor Web App mode, root components are handled by App.razor
    }
}

builder.Services.AddMudServices();
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddScoped<IProgressService, ProgressService>();
builder.Services.AddScoped<IStageDataService, StageDataService>();

await builder.Build().RunAsync();

public partial class NativeInterop
{
    [JSImport("isStandalone", "interop")]
    public static partial bool IsStandalone();
}
