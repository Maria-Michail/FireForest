using ForestFireApi.Models;

namespace ForestFireApi.Services.Weather
{
    public interface IWeatherService
    {
        List<string> GetStates();
        Task<List<FireRiskResult>> GetWeatherAndFirePredictionAsync(string state);
    }
}
