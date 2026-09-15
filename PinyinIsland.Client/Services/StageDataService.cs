using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using PinyinIsland.Client.Models;

namespace PinyinIsland.Client.Services;

public interface IStageDataService
{
    Task<List<StageData>> GetAllStagesAsync();
    Task<StageData?> GetStageDataAsync(int stageId);
}

public class StageDataService : IStageDataService
{
    private readonly HttpClient _http;
    private readonly NavigationManager _nav;
    private StageRepository? _cachedRepository;

    public StageDataService(HttpClient http, NavigationManager nav)
    {
        _http = http;
        _nav = nav;
    }

    public async Task<List<StageData>> GetAllStagesAsync()
    {
        if (_cachedRepository != null)
        {
            return _cachedRepository.Stages;
        }

        try
        {
            // Ensure absolute URI using NavigationManager.BaseUri for both Server Prerendering & Client WASM
            var baseUri = _http.BaseAddress?.ToString() ?? _nav.BaseUri;
            if (!baseUri.EndsWith("/")) baseUri += "/";
            var targetUri = new Uri(new Uri(baseUri), "assets/data/stages.json").ToString();

            var repo = await _http.GetFromJsonAsync<StageRepository>(targetUri);
            if (repo != null)
            {
                _cachedRepository = repo;
                return _cachedRepository.Stages;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading stages.json via HTTP ({_nav.BaseUri}): {ex.Message}");
        }

        return new List<StageData>();
    }

    public async Task<StageData?> GetStageDataAsync(int stageId)
    {
        var stages = await GetAllStagesAsync();
        return stages.FirstOrDefault(s => s.Id == stageId);
    }
}
