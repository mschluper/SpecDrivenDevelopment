using Microsoft.AspNetCore.Components;
using SpecDrivenDevelopment2.Services;

namespace SpecDrivenDevelopment2.Components.Layout
{
    public partial class MainLayout
    {
        [Inject] private IUserContextService UserContextService { get; set; } = null!;
    }
}