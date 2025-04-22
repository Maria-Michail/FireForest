using ForestFireWebApp.Models;

namespace ForestFireWebApp.Services;
public interface IWeatherService
{
    Task<List<string>> GetStatesAsync();
    Task<List<FireRiskResult>> GetWeatherAndFirePredictionAsync(string state);
}
