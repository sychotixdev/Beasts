using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Beasts.Api;

public static class PoeNinja
{
    private static readonly string PoeNinjaUrl = "https://poe.ninja/api/data/itemoverview?league=Mercenaries&type=Beast";

    public static async Task<Dictionary<string, float>> GetBeastsPrices()
    {
        using var httpClient = new HttpClient();
        var response = await httpClient.GetAsync(PoeNinjaUrl);
        if (!response.IsSuccessStatusCode) throw new HttpRequestException("Failed to get poe.ninja response");

        var json = await response.Content.ReadAsStringAsync();
        var poeNinjaResponse = JsonConvert.DeserializeObject<PoeNinjaResponse>(json);

        return poeNinjaResponse.Lines.ToDictionary(line => line.Name, line => line.ChaosValue);
    }

    private class PoeNinjaLine
    {
        [JsonProperty("chaosValue")] public float ChaosValue;
        [JsonProperty("name")] public string Name;
    }

    private class PoeNinjaResponse
    {
        [JsonProperty("lines")] public List<PoeNinjaLine> Lines;
    }
}