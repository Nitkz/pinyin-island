using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using PinyinIsland.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddMudServices();
builder.Services.AddScoped<IProgressService, ProgressService>();

await builder.Build().RunAsync();

