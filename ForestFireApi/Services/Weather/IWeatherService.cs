namespace ForestFireApi.Services.Weather
{
    public interface IWeatherService
    {
        List<string> GetStates();
        Task<Dictionary<string, float>> GetWeatherAndFirePredictionAsync(string state);
    }
}
