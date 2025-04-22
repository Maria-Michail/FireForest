using ForestFireWebApp.Data;
using ForestFireWebApp.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;

namespace ForestFireWebApp.Components.Pages;

public partial class Dashboard: ComponentBase
{
    [Inject] private FireRiskService FireRiskService { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;
    [Inject] private UserManager<ApplicationUser> UserManager { get; set; } = default!;

    protected string? UserId;
    protected bool IsLoading = true;
    protected bool ShowManageModal = false;
    protected List<string> SavedStates = new();
    protected  List<string> AllStates = new();

    protected override async Task OnInitializedAsync()
    {
        var user = (await AuthenticationStateProvider.GetAuthenticationStateAsync()).User;
        UserId = UserManager.GetUserId(user);

        AllStates = await FireRiskService.GetStatesAsync();
        SavedStates = await FireRiskService.GetUserStatesAsync(UserId);

        IsLoading = false;
    }

    protected async Task SaveStatesAsync(List<string> selectedStates)
    {
        await FireRiskService.SaveUserStatesAsync(UserId, selectedStates);
        SavedStates = selectedStates;
        ShowManageModal = false;
    }
}
