using ForestFireApi.Models;
using Microsoft.ML;
using Newtonsoft.Json;

namespace ForestFireApi.Services.Weather
{
    public class WeatherService : IWeatherService
    {
        private readonly string _apiKey;
        private readonly PredictionEngine<ForestFireInput, ForestFirePrediction> _predictionEngine;
        private static readonly Dictionary<string, (double, double)> StateCoordinates = new()
        {
            { "Acre", (-9.0238, -70.8120) }, { "Amazonas", (-3.4168, -65.8561) },
            { "Bahia", (-12.5797, -41.7007) }, { "Ceará", (-5.4984, -39.3206) },
            { "Distrito Federal", (-15.7998, -47.8645) }, { "Espírito Santo", (-19.1834, -40.3089) },
            { "Goiás", (-15.8270, -49.8362) }, { "Maranhão", (-5.2083, -45.3930) },
            { "Mato Grosso", (-12.6819, -56.9211) }, { "Mato Grosso do Sul", (-20.7722, -54.7852) },
            { "Minas Gerais", (-18.5122, -44.5550) }, { "Pará", (-3.4168, -52.2188) },
            { "Paraíba", (-7.2399, -36.7819) }, { "Paraná", (-24.8948, -51.5491) },
            { "Pernambuco", (-8.8137, -36.9541) }, { "Piauí", (-7.7183, -42.7289) },
            { "Rio de Janeiro", (-22.9083, -43.1964) }, { "Rio Grande do Norte", (-5.7945, -36.9541) },
            { "Rio Grande do Sul", (-30.0346, -51.2177) }, { "Rondônia", (-10.9077, -62.3779) },
            { "Roraima", (2.7376, -61.2780) }, { "Santa Catarina", (-27.2423, -50.2189) },
            { "São Paulo", (-23.5505, -46.6333) }, { "Sergipe", (-10.5741, -37.3857) },
            { "Tocantins", (-10.1753, -48.2982) }
        };

        private static readonly Dictionary<string, int> StateEncoding = new()
        {
            { "Acre", 0 },
            { "Alagoas", 1 },
            { "Amapá", 2 },
            { "Amazonas", 3 },
            { "Bahia", 4 },
            { "Ceará", 5 },
            { "Distrito Federal", 6 },
            { "Espírito Santo", 7 },
            { "Goiás", 8 },
            { "Maranhão", 9 },
            { "Mato Grosso", 10 },
            { "Mato Grosso do Sul", 11 },
            { "Minas Gerais", 12 },
            { "Pará", 13 },
            { "Paraíba", 14 },
            { "Paraná", 15 },
            { "Pernambuco", 16 },
            { "Piauí", 17 },
            { "Rio de Janeiro", 18 },
            { "Rio Grande do Norte", 19 },
            { "Rio Grande do Sul", 20 },
            { "Rondônia", 21 },
            { "Roraima", 22 },
            { "Santa Catarina", 23 },
            { "São Paulo", 24 },
            { "Sergipe", 25 },
            { "Tocantins", 26 }
        };

        private static readonly List<string> States = new()
        {
            "Acre", "Alagoas", "Amapá", "Amazonas", "Bahia", "Ceará", "Distrito Federal", "Espírito Santo",
            "Goiás", "Maranhão", "Mato Grosso", "Mato Grosso do Sul", "Minas Gerais", "Pará", "Paraíba",
            "Paraná", "Pernambuco", "Piauí", "Rio de Janeiro", "Rio Grande do Norte", "Rio Grande do Sul",
            "Rondônia", "Roraima", "Santa Catarina", "São Paulo", "Sergipe", "Tocantins"
        };

        public WeatherService(IConfiguration configuration)
        {
            _apiKey = configuration["OpenWeather:ApiKey"];
            var mlContext = new MLContext();
            var modelPath = Path.Combine(Directory.GetCurrentDirectory(), "MLModels", "forest_fire_model.onnx");
            var pipeline = mlContext.Transforms.ApplyOnnxModel(
                modelFile: modelPath,
                inputColumnNames: new[] { "float_input" },
                outputColumnNames: new[] { "probabilities" }
            );

            var emptyData = mlContext.Data.LoadFromEnumerable(new List<ForestFireInput>());
            var model = pipeline.Fit(emptyData);

            _predictionEngine = mlContext.Model.CreatePredictionEngine<ForestFireInput, ForestFirePrediction>(model);
        }

        public List<string> GetStates()
        {
            return States;
        }

        public async Task<Dictionary<string, float>> GetWeatherAndFirePredictionAsync(string state)
        {
            if (!StateCoordinates.TryGetValue(state, out var coordinates))
                return null;

            int stateEncoded = StateEncoding.TryGetValue(state, out var encodedValue) ? encodedValue : -1;

            string url = $"https://api.openweathermap.org/data/2.5/forecast?lat={coordinates.Item1}&lon={coordinates.Item2}&units=metric&appid={_apiKey}";

            using HttpClient client = new();
            HttpResponseMessage response = await client.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                return null;

            string responseBody = await response.Content.ReadAsStringAsync();
            var weatherData = JsonConvert.DeserializeObject<dynamic>(responseBody);

            var dailyData = new Dictionary<string, DailyWeather>();
            foreach (var entry in weatherData.list)
            {
                string date = DateTimeOffset.FromUnixTimeSeconds((long)entry.dt).UtcDateTime.ToString("yyyy-MM-dd");
                double temp = entry.main.temp;
                double tempMax = entry.main.temp_max;
                double tempMin = entry.main.temp_min;
                int humidity = entry.main.humidity;
                double windSpeed = entry.wind.speed;
                double rain = entry.rain?.ContainsKey("3h") == true ? (double)entry.rain["3h"] : 0;

                if (!dailyData.ContainsKey(date))
                    dailyData[date] = new DailyWeather();

                var data = dailyData[date];
                data.RainMax += rain;
                data.TempAvgSum += temp;
                data.TempAvgCount++;
                data.TempMax = Math.Max(data.TempMax, tempMax);
                data.TempMin = Math.Min(data.TempMin, tempMin);
                data.HumMax = Math.Max(data.HumMax, humidity);
                data.HumMin = Math.Min(data.HumMin, humidity);
                data.WindMax = Math.Max(data.WindMax, windSpeed);
                data.WindAvgSum += windSpeed;
                data.WindCount++;
            }

            var result = new Dictionary<string, float>();
            foreach (var kvp in dailyData)
            {
                var summary = new DailyWeatherSummary
                {
                    RainMax = kvp.Value.RainMax,
                    TempAvg = kvp.Value.TempAvgSum / kvp.Value.TempAvgCount,
                    TempMax = kvp.Value.TempMax,
                    TempMin = kvp.Value.TempMin,
                    HumMax = kvp.Value.HumMax,
                    HumMin = kvp.Value.HumMin,
                    WindMax = kvp.Value.WindMax,
                    WindAvg = kvp.Value.WindAvgSum / kvp.Value.WindCount
                };

                var input = new ForestFireInput
                {
                    Features = new float[]
                    {
                        (float)summary.RainMax,
                        (float)summary.TempAvg,
                        (float)summary.TempMax,
                        (float)summary.TempMin,
                        summary.HumMax,
                        summary.HumMin,
                        (float)summary.WindMax,
                        (float)summary.WindAvg,
                        DateTime.Parse(kvp.Key).Month,
                        stateEncoded
                    }
                };

                var prediction = _predictionEngine.Predict(input);
                float fireProbability = prediction.Probabilities[1];
                result[kvp.Key] = (fireProbability);
            }

            return result;
        }
    }
}
