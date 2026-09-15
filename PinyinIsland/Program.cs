using MudBlazor.Services;
using PinyinIsland.Client.Pages;
using PinyinIsland.Client.Services;
using PinyinIsland.Components;

var builder = WebApplication.CreateBuilder(args);

// Add MudBlazor services
builder.Services.AddMudServices();
builder.Services.AddHttpClient();
builder.Services.AddScoped<IProgressService, ProgressService>();
builder.Services.AddScoped<IStageDataService, StageDataService>();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}


app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(PinyinIsland.Client._Imports).Assembly);

app.Run();
