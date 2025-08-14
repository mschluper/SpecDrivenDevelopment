using Microsoft.EntityFrameworkCore;
using FamilyShoppingApp.Data;

namespace FamilyShoppingApp.Tests.Helpers;

public class TestDbContextFactory : IDbContextFactory<ApplicationDbContext>
{
    private readonly DbContextOptions<ApplicationDbContext> _options;

    public TestDbContextFactory(DbContextOptions<ApplicationDbContext> options)
    {
        _options = options;
    }

    public ApplicationDbContext CreateDbContext()
    {
        return new ApplicationDbContext(_options);
    }
    
    public async Task<ApplicationDbContext> CreateDbContextAsync()
    {
        return await Task.FromResult(new ApplicationDbContext(_options));
    }
}
