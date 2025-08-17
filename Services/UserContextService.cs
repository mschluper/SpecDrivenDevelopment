namespace SpecDrivenDevelopment2.Services
{
    public interface IUserContextService
    {
        string GetCurrentUser();
    }

    public class UserContextService : IUserContextService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UserContextService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public string GetCurrentUser()
        {
            var identity = _httpContextAccessor.HttpContext?.User?.Identity;
            if (identity?.IsAuthenticated == true)
            {
                return identity.Name ?? "Unknown";
            }
            return Environment.UserName;
        }
    }
}
