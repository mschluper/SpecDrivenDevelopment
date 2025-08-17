using Microsoft.AspNetCore.Components;
using SpecDrivenDevelopment.Services;

namespace SpecDrivenDevelopment.Components.Layout
{
    public partial class MainLayout
    {
        [Inject] private IUserContextService UserContextService { get; set; } = null!;
    }
}