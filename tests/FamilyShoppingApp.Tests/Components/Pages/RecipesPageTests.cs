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

public class RecipesPageTests : TestContext, IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

    public RecipesPageTests()
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
    public void Recipes_RendersCorrectly_WhenEmpty()
    {
        // Act
        var component = RenderComponent<Recipes>();

        // Assert
        Assert.Contains("Recipes", component.Find("h1").TextContent);
        Assert.Contains("No recipes found", component.Markup);
        
        var addButton = component.Find("[data-testid='create-recipe-button']");
        Assert.NotNull(addButton);
        Assert.Equal("Add New Recipe", addButton.TextContent.Trim());
    }

    [Fact]
    public async Task Recipes_RendersRecipesList_WhenRecipesExist()
    {
        // Arrange
        var product1 = new Product { Name = "Flour" };
        var product2 = new Product { Name = "Sugar" };
        _context.Products.AddRange(product1, product2);
        
        var recipe1 = new Recipe { Name = "Pancakes", Servings = 4 };
        var recipe2 = new Recipe { Name = "Cake", Servings = 8 };
        _context.Recipes.AddRange(recipe1, recipe2);
        await _context.SaveChangesAsync();
        
        _context.RecipeProducts.AddRange(
            new RecipeProduct { RecipeId = recipe1.Id, ProductId = product1.Id },
            new RecipeProduct { RecipeId = recipe2.Id, ProductId = product1.Id },
            new RecipeProduct { RecipeId = recipe2.Id, ProductId = product2.Id }
        );
        await _context.SaveChangesAsync();

        // Act
        var component = RenderComponent<Recipes>();

        // Assert
        var recipeRows = component.FindAll("[data-testid='recipe-row']");
        Assert.Equal(2, recipeRows.Count);
        
        // Should be ordered by name: Cake, Pancakes
        Assert.Contains("Cake", recipeRows[0].TextContent);
        Assert.Contains("8", recipeRows[0].TextContent); // servings
        Assert.Contains("Pancakes", recipeRows[1].TextContent);
        Assert.Contains("4", recipeRows[1].TextContent); // servings
    }

    [Fact]
    public async Task Recipes_ShowsIngredients_InRecipeRows()
    {
        // Arrange
        var product1 = new Product { Name = "Flour" };
        var product2 = new Product { Name = "Sugar" };
        var product3 = new Product { Name = "Eggs" };
        _context.Products.AddRange(product1, product2, product3);
        
        var recipe = new Recipe { Name = "Pancakes", Servings = 4 };
        _context.Recipes.Add(recipe);
        await _context.SaveChangesAsync();
        
        _context.RecipeProducts.AddRange(
            new RecipeProduct { RecipeId = recipe.Id, ProductId = product1.Id },
            new RecipeProduct { RecipeId = recipe.Id, ProductId = product3.Id }
        );
        await _context.SaveChangesAsync();

        // Act
        var component = RenderComponent<Recipes>();

        // Assert
        var recipeRow = component.Find("[data-testid='recipe-row']");
        var ingredientsSpan = recipeRow.QuerySelector("[data-testid='recipe-ingredients']");
        Assert.NotNull(ingredientsSpan);
        Assert.Contains("Eggs", ingredientsSpan.TextContent);
        Assert.Contains("Flour", ingredientsSpan.TextContent);
        Assert.DoesNotContain("Sugar", ingredientsSpan.TextContent);
    }

    [Fact]
    public void Recipes_ShowsAddForm_WhenAddButtonClicked()
    {
        // Arrange
        var component = RenderComponent<Recipes>();

        // Act
        var addButton = component.Find("[data-testid='create-recipe-button']");
        addButton.Click();

        // Assert
        var form = component.Find("[data-testid='recipe-form']");
        Assert.NotNull(form);
        
        var nameInput = component.Find("[data-testid='recipe-name-input']");
        Assert.NotNull(nameInput);
        
        var servingsInput = component.Find("[data-testid='servings-input']");
        Assert.NotNull(servingsInput);
        
        var saveButton = component.Find("[data-testid='save-recipe-button']");
        Assert.NotNull(saveButton);
        
        var cancelButton = component.Find("[data-testid='cancel-button']");
        Assert.NotNull(cancelButton);
    }

    [Fact]
    public async Task Recipes_CreatesRecipe_WhenFormSubmittedWithValidData()
    {
        // Arrange
        var product1 = new Product { Name = "Flour" };
        var product2 = new Product { Name = "Sugar" };
        _context.Products.AddRange(product1, product2);
        await _context.SaveChangesAsync();

        var component = RenderComponent<Recipes>();
        
        // Open form
        var addButton = component.Find("[data-testid='create-recipe-button']");
        addButton.Click();

        // Act - Fill form
        var nameInput = component.Find("[data-testid='recipe-name-input']");
        nameInput.Change("Test Recipe");
        
        var servingsInput = component.Find("[data-testid='servings-input']");
        servingsInput.Change("4");
        
        // Select ingredients
        var checkbox1 = component.Find($"[data-testid='product-checkbox-{product1.Id}']");
        checkbox1.Change(true);
        
        var checkbox2 = component.Find($"[data-testid='product-checkbox-{product2.Id}']");
        checkbox2.Change(true);
        
        // Submit form
        var saveButton = component.Find("[data-testid='save-recipe-button']");
        saveButton.Click();

        // Assert
        component.WaitForAssertion(() =>
        {
            using var verifyContext = _contextFactory.CreateDbContext();
            var recipe = verifyContext.Recipes
                .Include(r => r.RecipeProducts)
                .FirstOrDefault(r => r.Name == "Test Recipe");
                
            Assert.NotNull(recipe);
            Assert.Equal(4, recipe.Servings);
            Assert.Equal(2, recipe.RecipeProducts.Count);
        });
        
        // Form should be hidden after successful save
        component.WaitForAssertion(() =>
        {
            Assert.Throws<ElementNotFoundException>(() => component.Find("[data-testid='recipe-form']"));
        });
    }

    [Fact]
    public void Recipes_ShowsValidationErrors_WhenFormSubmittedWithInvalidData()
    {
        // Arrange
        var component = RenderComponent<Recipes>();
        
        // Open form
        var addButton = component.Find("[data-testid='create-recipe-button']");
        addButton.Click();

        // Make sure Save button is disabled
        var saveButton = component.Find("[data-testid='save-recipe-button']");
        Assert.True(saveButton.HasAttribute("disabled"));

        // Enter Name
        var nameInput = component.Find("[data-testid='recipe-name-input']");
        nameInput.Change("Test Recipe");

        saveButton = component.Find("[data-testid='save-recipe-button']");
        Assert.False(saveButton.HasAttribute("disabled"));

        // Erase Name
        nameInput.Change("");

        var servingsInput = component.Find("[data-testid='servings-input']");
        servingsInput.Change("0");

        // Act - Submit empty form
        saveButton = component.Find("[data-testid='save-recipe-button']");
        saveButton.Click();

        // Assert
        nameInput = component.Find("[data-testid='recipe-name-input']");
        Assert.True(nameInput.ClassList.Contains("is-invalid"));
        
        servingsInput = component.Find("[data-testid='servings-input']");
        Assert.True(servingsInput.ClassList.Contains("is-invalid"));
    }

    [Fact]
    public async Task Recipes_ShowsEditForm_WhenEditButtonClicked()
    {
        // Arrange
        var product = new Product { Name = "Flour" };
        _context.Products.Add(product);
        
        var recipe = new Recipe { Name = "Test Recipe", Servings = 4 };
        _context.Recipes.Add(recipe);
        await _context.SaveChangesAsync();
        
        _context.RecipeProducts.Add(new RecipeProduct { RecipeId = recipe.Id, ProductId = product.Id });
        await _context.SaveChangesAsync();

        var component = RenderComponent<Recipes>();

        // Act
        var editButton = component.Find("[data-testid='edit-recipe-button']");
        editButton.Click();

        // Assert
        var form = component.Find("[data-testid='recipe-form']");
        Assert.NotNull(form);
        
        var nameInput = component.Find("[data-testid='recipe-name-input']");
        Assert.Equal("Test Recipe", nameInput.GetAttribute("value"));
        
        var servingsInput = component.Find("[data-testid='servings-input']");
        Assert.Equal("4", servingsInput.GetAttribute("value"));
        
        // Ingredient should be selected
        var checkbox = component.Find($"[data-testid='product-checkbox-{product.Id}']");
        Assert.True(checkbox.HasAttribute("checked"));
    }

    [Fact]
    public async Task Recipes_UpdatesRecipe_WhenEditFormSubmitted()
    {
        // Arrange
        var product1 = new Product { Name = "Flour" };
        var product2 = new Product { Name = "Sugar" };
        _context.Products.AddRange(product1, product2);
        
        var recipe = new Recipe { Name = "Original Recipe", Servings = 2 };
        _context.Recipes.Add(recipe);
        await _context.SaveChangesAsync();
        
        _context.RecipeProducts.Add(new RecipeProduct { RecipeId = recipe.Id, ProductId = product1.Id });
        await _context.SaveChangesAsync();

        var component = RenderComponent<Recipes>();
        
        // Open edit form
        var editButton = component.Find("[data-testid='edit-recipe-button']");
        editButton.Click();

        // Act - Update form
        var nameInput = component.Find("[data-testid='recipe-name-input']");
        nameInput.Change("Updated Recipe");
        
        var servingsInput = component.Find("[data-testid='servings-input']");
        servingsInput.Change("6");
        
        // Add second ingredient
        var checkbox2 = component.Find($"[data-testid='product-checkbox-{product2.Id}']");
        checkbox2.Change(true);
        
        // Submit form
        var saveButton = component.Find("[data-testid='save-recipe-button']");
        saveButton.Click();

        // Assert
        component.WaitForAssertion(() =>
        {
            using var verifyContext = _contextFactory.CreateDbContext();
            var updatedRecipe = verifyContext.Recipes
                .Include(r => r.RecipeProducts)
                .FirstOrDefault(r => r.Id == recipe.Id);
                
            Assert.NotNull(updatedRecipe);
            Assert.Equal("Updated Recipe", updatedRecipe.Name);
            Assert.Equal(6, updatedRecipe.Servings);
            Assert.Equal(2, updatedRecipe.RecipeProducts.Count);
        });
    }

    [Fact]
    public async Task Recipes_DeletesRecipe_WhenDeleteButtonClicked()
    {
        // Arrange
        var product = new Product { Name = "Flour" };
        _context.Products.Add(product);
        
        var recipe = new Recipe { Name = "Test Recipe", Servings = 4 };
        _context.Recipes.Add(recipe);
        await _context.SaveChangesAsync();
        
        _context.RecipeProducts.Add(new RecipeProduct { RecipeId = recipe.Id, ProductId = product.Id });
        await _context.SaveChangesAsync();

        var component = RenderComponent<Recipes>();

        // Act
        var deleteButton = component.Find("[data-testid='delete-recipe-button']");
        deleteButton.Click();

        // Assert
        component.WaitForAssertion(() =>
        {
            using var verifyContext = _contextFactory.CreateDbContext();
            var deletedRecipe = verifyContext.Recipes.Find(recipe.Id);
            Assert.Null(deletedRecipe);
        });
        
        // Recipe should be removed from UI
        component.WaitForAssertion(() =>
        {
            var recipeRows = component.FindAll("[data-testid='recipe-row']");
            Assert.Empty(recipeRows);
        });
    }

    [Fact]
    public void Recipes_CancelsForm_WhenCancelButtonClicked()
    {
        // Arrange
        var component = RenderComponent<Recipes>();
        
        // Open form
        var addButton = component.Find("[data-testid='create-recipe-button']");
        addButton.Click();
        
        // Fill some data
        var nameInput = component.Find("[data-testid='recipe-name-input']");
        nameInput.Change("Test Recipe");

        // Act
        var cancelButton = component.Find("[data-testid='cancel-button']");
        cancelButton.Click();

        // Assert
        Assert.Throws<ElementNotFoundException>(() => component.Find("[data-testid='recipe-form']"));
    }

    [Fact]
    public async Task Recipes_FiltersRecipesByProduct_WhenProductSelected()
    {
        // Arrange
        var product1 = new Product { Name = "Flour" };
        var product2 = new Product { Name = "Sugar" };
        _context.Products.AddRange(product1, product2);
        
        var recipe1 = new Recipe { Name = "Pancakes", Servings = 4 };
        var recipe2 = new Recipe { Name = "Cake", Servings = 8 };
        var recipe3 = new Recipe { Name = "Salad", Servings = 2 };
        _context.Recipes.AddRange(recipe1, recipe2, recipe3);
        await _context.SaveChangesAsync();
        
        // Only recipe1 and recipe2 use flour
        _context.RecipeProducts.AddRange(
            new RecipeProduct { RecipeId = recipe1.Id, ProductId = product1.Id },
            new RecipeProduct { RecipeId = recipe2.Id, ProductId = product1.Id },
            new RecipeProduct { RecipeId = recipe2.Id, ProductId = product2.Id }
        );
        await _context.SaveChangesAsync();

        var component = RenderComponent<Recipes>();

        // Act
        var filterSelect = component.Find("[data-testid='product-filter-select']");
        filterSelect.Change(product1.Id.ToString());

        // Assert
        component.WaitForAssertion(() =>
        {
            var recipeRows = component.FindAll("[data-testid='recipe-row']");
            Assert.Equal(2, recipeRows.Count);
            Assert.Contains("Cake", component.Markup);
            Assert.Contains("Pancakes", component.Markup);
            Assert.DoesNotContain("Salad", component.Markup);
        });
    }

    [Fact]
    public async Task Recipes_ShowsAllRecipes_WhenFilterCleared()
    {
        // Arrange
        var product = new Product { Name = "Flour" };
        _context.Products.Add(product);
        
        var recipe1 = new Recipe { Name = "Pancakes", Servings = 4 };
        var recipe2 = new Recipe { Name = "Salad", Servings = 2 };
        _context.Recipes.AddRange(recipe1, recipe2);
        await _context.SaveChangesAsync();
        
        _context.RecipeProducts.Add(new RecipeProduct { RecipeId = recipe1.Id, ProductId = product.Id });
        await _context.SaveChangesAsync();

        var component = RenderComponent<Recipes>();
        
        // First apply filter
        var filterSelect = component.Find("[data-testid='product-filter-select']");
        filterSelect.Change(product.Id.ToString());

        // Act - Clear filter
        filterSelect.Change("0");
        component.WaitForAssertion(() => Assert.Contains("All recipes", component.Markup));

        // Assert
        component.WaitForAssertion(() =>
        {
            var recipeRows = component.FindAll("[data-testid='recipe-row']");
            Assert.Equal(2, recipeRows.Count);
            Assert.Contains("Pancakes", component.Markup);
            Assert.Contains("Salad", component.Markup);
        });
    }

    public new void Dispose()
    {
        _context.Dispose();
        base.Dispose();
    }
}
