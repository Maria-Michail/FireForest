using ForestFireWebApp.Models;
using ForestFireWebApp.Data;
using Microsoft.EntityFrameworkCore;

namespace ForestFireWebApp.Services;

public class FireRiskService
{
    private readonly ApplicationDbContext _context;
    private IWeatherService _weatherService;

    public FireRiskService(ApplicationDbContext context, IWeatherService weatherService)
    {
        _context = context;
        _weatherService = weatherService;
    }

    public Task<List<string>> GetStatesAsync() =>
         _weatherService.GetStatesAsync();

    public Task<List<FireRiskResult>> GetFireRiskAsync(string state) =>
        _weatherService.GetWeatherAndFirePredictionAsync(state);

    public async Task SaveUserStatesAsync(string userId, List<string> states)
    {
        var existing = _context.UserPreferences.Where(p => p.UserId == userId);
        _context.UserPreferences.RemoveRange(existing);

        var entries = states.Select(state => new UserPreference
        {
            UserId = userId,
            State = state
        });

        await _context.UserPreferences.AddRangeAsync(entries);
        await _context.SaveChangesAsync();
    }

    public async Task<List<string>> GetUserStatesAsync(string userId)
    {
        return await _context.UserPreferences
            .Where(p => p.UserId == userId)
            .Select(p => p.State)
            .ToListAsync();
    }
}
