using ForestFireApi.Services.Weather;
using Microsoft.AspNetCore.Mvc;

namespace ForestFireApi.Controllers
{
    [ApiController]
    [Route("api")]
    public class WeatherController : ControllerBase
    {
        private readonly IWeatherService _weatherService;

        public WeatherController(IWeatherService weatherService)
        {
            _weatherService = weatherService;
        }

        [HttpGet("states")]
        public async Task<IActionResult> GetStates()
        {
            var result = _weatherService.GetStates();
            return Ok(result);
        }

        [HttpGet("weather/{state}")]
        public async Task<IActionResult> GetWeather(string state)
        {
            var result = await _weatherService.GetWeatherAndFirePredictionAsync(state);
            if (result == null)
                return BadRequest("Invalid state name or failed to fetch weather data.");

            return Ok(result);
        }
    }
}