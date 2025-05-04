using System.Net.Http;
using ForestFireWebApp.Models;
using Microsoft.ML;
using Newtonsoft.Json;

namespace ForestFireWebApp.Services;
public class WeatherService : IWeatherService
{
    private readonly string _apiKey;
    private readonly IFireRiskOnnxPredictor _predictor;
    private readonly HttpClient _httpClient;

    private static readonly Dictionary<string, (double, double)> StateCoordinates = new()
    {
        { "Acre", (-9.0238, -70.8120) }, { "Amazonas", (-3.4168, -65.8561) },
        { "Bahia", (-12.5797, -41.7007) }, { "Ceara", (-5.4984, -39.3206) },
        { "Distrito Federal", (-15.7998, -47.8645) }, { "Espirito Santo", (-19.1834, -40.3089) },
        { "Goias", (-15.8270, -49.8362) }, { "Maranhao", (-5.2083, -45.3930) },
        { "Mato Grosso", (-12.6819, -56.9211) }, { "Mato Grosso do Sul", (-20.7722, -54.7852) },
        { "Minas Gerais", (-18.5122, -44.5550) }, { "Para", (-3.4168, -52.2188) },
        { "Paraiba", (-7.2399, -36.7819) }, { "Parana", (-24.8948, -51.5491) },
        { "Pernambuco", (-8.8137, -36.9541) }, { "Piaui", (-7.7183, -42.7289) },
        { "Rio", (-22.9083, -43.1964) }, { "Rio Grande do Norte", (-5.7945, -36.9541) },
        { "Rio Grande do Sul", (-30.0346, -51.2177) }, { "Rondonia", (-10.9077, -62.3779) },
        { "Roraima", (2.7376, -61.2780) }, { "Santa Catarina", (-27.2423, -50.2189) },
        { "Sao Paulo", (-23.5505, -46.6333) }, { "Sergipe", (-10.5741, -37.3857) },
        { "Tocantins", (-10.1753, -48.2982) }
    };

    private static readonly Dictionary<string, int> StateEncoding = new()
    {
        { "Acre", 0 }, { "Alagoas", 1 }, { "Amapa", 2 }, { "Amazonas", 3 }, { "Bahia", 4 }, { "Ceara", 5 },
        { "Distrito Federal", 6 }, { "Espirito Santo", 7 }, { "Goias", 8 }, { "Maranhao", 9 },
        { "Mato Grosso", 10 }, { "Mato Grosso do Sul", 11 }, { "Minas Gerais", 12 }, { "Para", 13 },
        { "Paraiba", 14 }, { "Parana", 15 }, { "Pernambuco", 16 }, { "Piaui", 17 }, { "Rio", 18 },
        { "Rio Grande do Norte", 19 }, { "Rio Grande do Sul", 20 }, { "Rondonia", 21 }, { "Roraima", 22 },
        { "Santa Catarina", 23 }, { "Sao Paulo", 24 }, { "Sergipe", 25 }, { "Tocantins", 26 }
    };

    public WeatherService(IConfiguration configuration, IFireRiskOnnxPredictor predictor, HttpClient httpClient)
    {
        _apiKey = configuration["OpenWeather:ApiKey"];
        _predictor = predictor;
        _httpClient = httpClient;
    }

    public Task<List<string>> GetStatesAsync() =>
        Task.FromResult(StateCoordinates.Keys.ToList());

    public async Task<List<FireRiskResult>> GetWeatherAndFirePredictionAsync(string state)
    {
        if (!StateCoordinates.TryGetValue(state, out var coordinates))
            return null;

        int stateEncoded = StateEncoding.TryGetValue(state, out var encodedValue) ? encodedValue : -1;

        string url = $"https://api.openweathermap.org/data/2.5/forecast?lat={coordinates.Item1}&lon={coordinates.Item2}&units=metric&appid={_apiKey}";

        HttpResponseMessage response = await _httpClient.GetAsync(url);
        if (!response.IsSuccessStatusCode)
            return null;

        string responseBody = await response.Content.ReadAsStringAsync();
        var weatherData = JsonConvert.DeserializeObject<dynamic>(responseBody);

        var dailyAggregatedData = new Dictionary<string, List<dynamic>>();

        foreach (var entry in weatherData.list)
        {
            string date = DateTimeOffset.FromUnixTimeSeconds((long)entry.dt).UtcDateTime.ToString("yyyy-MM-dd");

            if (!dailyAggregatedData.ContainsKey(date))
                dailyAggregatedData[date] = new List<dynamic>();

            dailyAggregatedData[date].Add(entry);
        }

        var results = new List<FireRiskResult>();

        foreach (var kvp in dailyAggregatedData)
        {
            var dailyEntries = kvp.Value;

            float tempMax = dailyEntries.Max(e => (float)e.main.temp_max);
            float tempMin = dailyEntries.Min(e => (float)e.main.temp_min);
            float humMax = dailyEntries.Max(e => (float)e.main.humidity);
            float humMin = dailyEntries.Min(e => (float)e.main.humidity);
            float windMax = dailyEntries.Max(e => (float)e.wind.speed);

            int month = DateTime.Parse(kvp.Key).Month;

            var input = new ForestFireInput
            {
                Features = new float[]
                {
            month,
            tempMax,
            tempMin,
            humMax,
            humMin,
            windMax,
            stateEncoded
                }
            };

            float predictedFires = _predictor.Predict(new float[]
            {
                month,
                tempMax,
                tempMin,
                humMax,
                humMin,
                windMax,
                stateEncoded
            });

            float probability = Math.Clamp((float)(Math.Log10(predictedFires + 1) / 3) * 100f, 0f, 100f);

            string riskLevel = predictedFires switch
            {
                <= 10 => "No Fire",
                <= 50 => "Low Risk",
                <= 200 => "Medium Risk",
                _ => "High Risk"
            };

            results.Add(new FireRiskResult
            {
                Date = kvp.Key,
                Probability = probability,
                RiskLevel = riskLevel
            });
        }

        return results.OrderBy(r => r.Date).ToList();
    }
}
