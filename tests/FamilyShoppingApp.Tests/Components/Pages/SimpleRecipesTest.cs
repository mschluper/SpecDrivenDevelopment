using Bunit;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using FamilyShoppingApp.Pages;
using FamilyShoppingApp.Data;
using FamilyShoppingApp.Services;
using FamilyShoppingApp.Models;
using FamilyShoppingApp.Tests.Helpers;

namespace FamilyShoppingApp.Tests.Components.Pages;

public class SimpleRecipesTest : TestContext, IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

    public SimpleRecipesTest()
    {
        var dbName = Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        _context = new ApplicationDbContext(options);
        
        var factory = new TestDbContextFactory(options);
        _contextFactory = factory;

        Services.AddScoped<IDbContextFactory<ApplicationDbContext>>(_ => _contextFactory);
        Services.AddScoped<RecipeService>();
        Services.AddScoped<ProductService>();
        Services.AddScoped<ExceptionHandlerService>();
    }

    [Fact]
    public void SimpleRecipesTest_CanRender()
    {
        // Act
        var component = RenderComponent<Recipes>();

        // Assert - Just check that it renders without throwing
        Assert.NotNull(component);
        
        // Print the markup to see what's actually rendered
        var markup = component.Markup;
        Assert.NotEmpty(markup);
        
        // Output the markup for debugging
        System.Console.WriteLine("Rendered markup:");
        System.Console.WriteLine(markup);
        
        // Check if we can find basic HTML structure
        Assert.Contains("div", markup);
    }

    public new void Dispose()
    {
        _context.Dispose();
        base.Dispose();
    }
}
