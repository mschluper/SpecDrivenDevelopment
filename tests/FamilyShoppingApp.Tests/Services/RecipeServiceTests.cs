using Xunit;
using Microsoft.EntityFrameworkCore;
using FamilyShoppingApp.Data;
using FamilyShoppingApp.Services;
using FamilyShoppingApp.ViewModels;
using FamilyShoppingApp.Models;
using FamilyShoppingApp.Tests.Helpers;

namespace FamilyShoppingApp.Tests.Services;

public class RecipeServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly RecipeService _recipeService;

    public RecipeServiceTests()
    {
        var dbName = Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        _context = new ApplicationDbContext(options);
        
        var factory = new TestDbContextFactory(options);
        _contextFactory = factory;
        _recipeService = new RecipeService(_contextFactory);
    }

    [Fact]
    public async Task GetAllRecipesAsync_ReturnsEmptyList_WhenNoRecipes()
    {
        // Act
        var result = await _recipeService.GetAllRecipesAsync();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllRecipesAsync_ReturnsRecipes_OrderedByName()
    {
        // Arrange
        var product1 = new Product { Name = "Flour" };
        var product2 = new Product { Name = "Sugar" };
        _context.Products.AddRange(product1, product2);
        
        var recipe1 = new Recipe { Name = "Zebra Cake", Servings = 8 };
        var recipe2 = new Recipe { Name = "Apple Pie", Servings = 6 };
        _context.Recipes.AddRange(recipe1, recipe2);
        await _context.SaveChangesAsync();
        
        _context.RecipeProducts.AddRange(
            new RecipeProduct { RecipeId = recipe1.Id, ProductId = product1.Id },
            new RecipeProduct { RecipeId = recipe2.Id, ProductId = product1.Id },
            new RecipeProduct { RecipeId = recipe2.Id, ProductId = product2.Id }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _recipeService.GetAllRecipesAsync();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("Apple Pie", result[0].Name);
        Assert.Equal("Zebra Cake", result[1].Name);
        Assert.Equal(6, result[0].Servings);
        Assert.Equal(8, result[1].Servings);
        Assert.Equal(2, result[0].SelectedProductIds.Count);
        Assert.Single(result[1].SelectedProductIds);
    }

    [Fact]
    public async Task CreateRecipeAsync_CreatesRecipe_ReturnsId()
    {
        // Arrange
        var product1 = new Product { Name = "Eggs" };
        var product2 = new Product { Name = "Milk" };
        _context.Products.AddRange(product1, product2);
        await _context.SaveChangesAsync();

        var recipeViewModel = new RecipeViewModel
        {
            Name = "Scrambled Eggs",
            Servings = 2,
            SelectedProductIds = new HashSet<int> { product1.Id, product2.Id }
        };

        // Act
        var id = await _recipeService.CreateRecipeAsync(recipeViewModel);

        // Assert
        Assert.True(id > 0);
        
        using var verifyContext = await _contextFactory.CreateDbContextAsync();
        var createdRecipe = await verifyContext.Recipes
            .Include(r => r.RecipeProducts)
            .FirstOrDefaultAsync(r => r.Id == id);
            
        Assert.NotNull(createdRecipe);
        Assert.Equal("Scrambled Eggs", createdRecipe.Name);
        Assert.Equal(2, createdRecipe.Servings);
        Assert.Equal(2, createdRecipe.RecipeProducts.Count);
        Assert.Contains(createdRecipe.RecipeProducts, rp => rp.ProductId == product1.Id);
        Assert.Contains(createdRecipe.RecipeProducts, rp => rp.ProductId == product2.Id);
    }

    [Fact]
    public async Task GetRecipeByIdAsync_ReturnsRecipe_WhenExists()
    {
        // Arrange
        var product = new Product { Name = "Tomatoes" };
        _context.Products.Add(product);
        
        var recipe = new Recipe { Name = "Tomato Soup", Servings = 4 };
        _context.Recipes.Add(recipe);
        await _context.SaveChangesAsync();
        
        _context.RecipeProducts.Add(new RecipeProduct { RecipeId = recipe.Id, ProductId = product.Id });
        await _context.SaveChangesAsync();

        // Act
        var result = await _recipeService.GetRecipeByIdAsync(recipe.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Tomato Soup", result.Name);
        Assert.Equal(4, result.Servings);
        Assert.Single(result.SelectedProductIds);
        Assert.Contains(product.Id, result.SelectedProductIds);
    }

    [Fact]
    public async Task GetRecipeByIdAsync_ReturnsNull_WhenNotExists()
    {
        // Act
        var result = await _recipeService.GetRecipeByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateRecipeAsync_UpdatesRecipe_WhenExists()
    {
        // Arrange
        var product1 = new Product { Name = "Chicken" };
        var product2 = new Product { Name = "Rice" };
        var product3 = new Product { Name = "Vegetables" };
        _context.Products.AddRange(product1, product2, product3);
        
        var recipe = new Recipe { Name = "Original Recipe", Servings = 2 };
        _context.Recipes.Add(recipe);
        await _context.SaveChangesAsync();
        
        _context.RecipeProducts.Add(new RecipeProduct { RecipeId = recipe.Id, ProductId = product1.Id });
        await _context.SaveChangesAsync();

        var updateViewModel = new RecipeViewModel
        {
            Id = recipe.Id,
            Name = "Chicken Fried Rice",
            Servings = 4,
            SelectedProductIds = new HashSet<int> { product1.Id, product2.Id, product3.Id }
        };

        // Act
        await _recipeService.UpdateRecipeAsync(updateViewModel);

        // Assert
        using var verifyContext = await _contextFactory.CreateDbContextAsync();
        var updatedRecipe = await verifyContext.Recipes
            .Include(r => r.RecipeProducts)
            .FirstOrDefaultAsync(r => r.Id == recipe.Id);
            
        Assert.NotNull(updatedRecipe);
        Assert.Equal("Chicken Fried Rice", updatedRecipe.Name);
        Assert.Equal(4, updatedRecipe.Servings);
        Assert.Equal(3, updatedRecipe.RecipeProducts.Count);
        Assert.Contains(updatedRecipe.RecipeProducts, rp => rp.ProductId == product1.Id);
        Assert.Contains(updatedRecipe.RecipeProducts, rp => rp.ProductId == product2.Id);
        Assert.Contains(updatedRecipe.RecipeProducts, rp => rp.ProductId == product3.Id);
    }

    [Fact]
    public async Task DeleteRecipeAsync_DeletesRecipe_WhenExists()
    {
        // Arrange
        var product = new Product { Name = "Pasta" };
        _context.Products.Add(product);
        
        var recipe = new Recipe { Name = "Spaghetti", Servings = 3 };
        _context.Recipes.Add(recipe);
        await _context.SaveChangesAsync();
        
        _context.RecipeProducts.Add(new RecipeProduct { RecipeId = recipe.Id, ProductId = product.Id });
        await _context.SaveChangesAsync();

        // Act
        await _recipeService.DeleteRecipeAsync(recipe.Id);

        // Assert
        using var verifyContext = await _contextFactory.CreateDbContextAsync();
        var deletedRecipe = await verifyContext.Recipes.FindAsync(recipe.Id);
        Assert.Null(deletedRecipe);
        
        var recipeProducts = await verifyContext.RecipeProducts
            .Where(rp => rp.RecipeId == recipe.Id)
            .ToListAsync();
        Assert.Empty(recipeProducts);
    }

    [Fact]
    public async Task GetRecipesByProductIdAsync_ReturnsMatchingRecipes()
    {
        // Arrange
        var product1 = new Product { Name = "Flour" };
        var product2 = new Product { Name = "Sugar" };
        var product3 = new Product { Name = "Eggs" };
        _context.Products.AddRange(product1, product2, product3);
        
        var recipe1 = new Recipe { Name = "Pancakes", Servings = 4 };
        var recipe2 = new Recipe { Name = "Cake", Servings = 8 };
        var recipe3 = new Recipe { Name = "Pasta", Servings = 2 };
        _context.Recipes.AddRange(recipe1, recipe2, recipe3);
        await _context.SaveChangesAsync();
        
        // Both recipe1 and recipe2 use flour, recipe3 doesn't
        _context.RecipeProducts.AddRange(
            new RecipeProduct { RecipeId = recipe1.Id, ProductId = product1.Id },
            new RecipeProduct { RecipeId = recipe1.Id, ProductId = product3.Id },
            new RecipeProduct { RecipeId = recipe2.Id, ProductId = product1.Id },
            new RecipeProduct { RecipeId = recipe2.Id, ProductId = product2.Id },
            new RecipeProduct { RecipeId = recipe3.Id, ProductId = product3.Id }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _recipeService.GetRecipesByProductIdAsync(product1.Id);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, r => r.Name == "Cake");
        Assert.Contains(result, r => r.Name == "Pancakes");
        Assert.DoesNotContain(result, r => r.Name == "Pasta");
        
        // Verify they're ordered by name
        Assert.Equal("Cake", result[0].Name);
        Assert.Equal("Pancakes", result[1].Name);
    }

    [Fact]
    public async Task GetRecipesByProductIdAsync_ReturnsEmptyList_WhenNoMatches()
    {
        // Arrange
        var product1 = new Product { Name = "Flour" };
        var product2 = new Product { Name = "Sugar" };
        _context.Products.AddRange(product1, product2);
        
        var recipe = new Recipe { Name = "Pasta", Servings = 2 };
        _context.Recipes.Add(recipe);
        await _context.SaveChangesAsync();
        
        // Recipe doesn't use product2
        _context.RecipeProducts.Add(new RecipeProduct { RecipeId = recipe.Id, ProductId = product1.Id });
        await _context.SaveChangesAsync();

        // Act
        var result = await _recipeService.GetRecipesByProductIdAsync(product2.Id);

        // Assert
        Assert.Empty(result);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
