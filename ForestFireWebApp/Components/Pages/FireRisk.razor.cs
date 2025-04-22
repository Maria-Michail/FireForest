using ForestFireWebApp.Models;
using ForestFireWebApp.Services;
using Microsoft.AspNetCore.Components;

namespace ForestFireWebApp.Components.Pages;

public partial class FireRisk : ComponentBase
{
    [Inject]
    private FireRiskService FireRiskService { get; set; } = default!;

    protected List<string> States = new();
    protected string SelectedState = string.Empty;
    protected string DisplayedState = string.Empty;
    protected List<FireRiskResult> Predictions = new();

    protected override async Task OnInitializedAsync()
    {
        States = await FireRiskService.GetStatesAsync();
    }

    protected async Task GetFireRisk()
    {
        if (!string.IsNullOrWhiteSpace(SelectedState))
        {
            Predictions = await FireRiskService.GetFireRiskAsync(SelectedState);
            DisplayedState = SelectedState;
        }
    }

    protected string GetRiskCssClass(string riskLevel) => riskLevel switch
    {
        "High Risk" => "high-risk",
        "Medium Risk" => "medium-risk",
        "Low Risk" => "low-risk",
        "No Fire" => "no-fire",
        _ => string.Empty
    };
}
