using Xunit;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using FamilyShoppingApp.Data;
using FamilyShoppingApp.Services;
using FamilyShoppingApp.Pages;
using FamilyShoppingApp.ViewModels;
using FamilyShoppingApp.Models;
using FamilyShoppingApp.Tests.Helpers;

namespace FamilyShoppingApp.Tests.Pages;

public class ProductsPageTests : TestContext
{
    private readonly ApplicationDbContext _context;
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

    public ProductsPageTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _contextFactory = new TestDbContextFactory(options);
        
        Services.AddScoped<ProductService>(_ => new ProductService(_contextFactory));
        Services.AddScoped<StoreService>(_ => new StoreService(_contextFactory));
        Services.AddScoped<ExceptionHandlerService>();
    }

    [Fact]
    public void ProductsPage_RendersCorrectly_WhenNoProducts()
    {
        // Act
        var component = RenderComponent<Products>();

        // Assert
        Assert.Contains("Products", component.Find("h1").InnerHtml);
        Assert.NotNull(component.Find("[data-testid='search-input']"));
        Assert.NotNull(component.Find("[data-testid='search-button']"));
        Assert.NotNull(component.Find("[data-testid='create-product-button']"));
        Assert.NotNull(component.Find("[data-testid='no-products-message']"));
        Assert.Contains("No products found. Click \"Add New Product\" to get started.", 
            component.Find("[data-testid='no-products-message']").TextContent.Trim());
    }

    [Fact]
    public void CreateProductButton_ShowsForm_WhenClicked()
    {
        // Arrange
        var component = RenderComponent<Products>();

        // Act
        component.Find("[data-testid='create-product-button']").Click();

        // Assert
        Assert.NotNull(component.Find("[data-testid='product-form']"));
        Assert.NotNull(component.Find("[data-testid='product-name-input']"));
        Assert.NotNull(component.Find("[data-testid='product-notes-input']"));
        Assert.NotNull(component.Find("[data-testid='store-checkboxes']"));
        Assert.NotNull(component.Find("[data-testid='save-product-button']"));
        Assert.NotNull(component.Find("[data-testid='cancel-button']"));
    }

    [Fact]
    public void ProductForm_DisablesSubmit_WhenNameEmpty()
    {
        // Arrange
        var component = RenderComponent<Products>();
        component.Find("[data-testid='create-product-button']").Click();

        // Assert
        var submitButton = component.Find("[data-testid='save-product-button']");
        Assert.True(submitButton.HasAttribute("disabled"));
    }

    [Fact]
    public void ProductForm_EnablesSubmit_WhenNameProvided()
    {
        // Arrange
        var component = RenderComponent<Products>();
        component.Find("[data-testid='create-product-button']").Click();

        // Act
        var nameInput = component.Find("[data-testid='product-name-input']");
        nameInput.Change("Test Product");

        // Assert
        component.WaitForAssertion(() =>
        {
            var submitButton = component.Find("[data-testid='save-product-button']");
            Assert.False(submitButton.HasAttribute("disabled"));
        });
    }

    [Fact]
    public void ProductForm_ShowsValidationError_WhenNameEmptyAfterInput()
    {
        // Arrange
        var component = RenderComponent<Products>();
        component.Find("[data-testid='create-product-button']").Click();

        // Act
        var nameInput = component.Find("[data-testid='product-name-input']");
        nameInput.Change("Test Product");
        nameInput.Change("");

        // Assert
        component.WaitForAssertion(() =>
        {
            var validationError = component.Find("[data-testid='name-validation-error']");
            Assert.Equal("Product name is required.", validationError.TextContent.Trim());
        });
    }

    [Fact]
    public async Task ProductForm_CreatesProduct_WhenValidDataSubmitted()
    {
        // Arrange
        var store1 = new Store { Name = "Store 1", Notes = "Test Store 1" };
        var store2 = new Store { Name = "Store 2", Notes = "Test Store 2" };
        _context.Stores.AddRange(store1, store2);
        await _context.SaveChangesAsync();

        var component = RenderComponent<Products>();
        component.Find("[data-testid='create-product-button']").Click();

        // Act
        var nameInput = component.Find("[data-testid='product-name-input']");
        var notesInput = component.Find("[data-testid='product-notes-input']");
        
        nameInput.Change("Test Product");
        notesInput.Change("Test Notes");
        
        // Select stores
        component.WaitForAssertion(() =>
        {
            var storeCheckbox = component.Find($"[data-testid='store-checkbox-{store1.Id}']");
            storeCheckbox.Change(true);
        });
        
        component.Find("[data-testid='save-product-button']").Click();

        // Assert
        component.WaitForAssertion(() =>
        {
            var products = _context.Products.Include(p => p.ProductStores).ToList();
            Assert.Single(products);
            Assert.Equal("Test Product", products[0].Name);
            Assert.Equal("Test Notes", products[0].Notes);
            Assert.Single(products[0].ProductStores);
            Assert.Equal(store1.Id, products[0].ProductStores.First().StoreId);
        });
        
        // Form should be hidden after successful creation
        component.WaitForAssertion(() =>
        {
            Assert.Throws<Bunit.ElementNotFoundException>(() => component.Find("[data-testid='product-form']"));
        });
    }

    [Fact]
    public async Task ProductsPage_DisplaysProducts_WhenProductsExist()
    {
        // Arrange
        var store1 = new Store { Name = "Store 1", Notes = "Test Store 1" };
        var store2 = new Store { Name = "Store 2", Notes = "Test Store 2" };
        _context.Stores.AddRange(store1, store2);

        var product1 = new Product { Name = "Product 1", Notes = "Notes 1" };
        var product2 = new Product { Name = "Product 2", Notes = "Notes 2" };
        _context.Products.AddRange(product1, product2);
        
        await _context.SaveChangesAsync();
        
        _context.ProductStores.Add(new ProductStore { ProductId = product1.Id, StoreId = store1.Id });
        _context.ProductStores.Add(new ProductStore { ProductId = product1.Id, StoreId = store2.Id });
        _context.ProductStores.Add(new ProductStore { ProductId = product2.Id, StoreId = store1.Id });
        await _context.SaveChangesAsync();

        // Act
        var component = RenderComponent<Products>();

        // Assert
        component.WaitForAssertion(() =>
        {
            var table = component.Find("[data-testid='products-table']");
            Assert.NotNull(table);
            
            var rows = component.FindAll("[data-testid='product-row']");
            Assert.Equal(2, rows.Count);
            
            var productNames = component.FindAll("[data-testid='product-name']");
            Assert.Contains("Product 1", productNames.Select(n => n.TextContent));
            Assert.Contains("Product 2", productNames.Select(n => n.TextContent));
            
            var productStores = component.FindAll("[data-testid='product-stores']");
            Assert.Contains("Store 1, Store 2", productStores.Select(s => s.TextContent));
            Assert.Contains("Store 1", productStores.Select(s => s.TextContent));
        });
    }

    [Fact]
    public async Task SearchFunctionality_FiltersProducts_WhenSearchTermEntered()
    {
        // Arrange
        var product1 = new Product { Name = "Apple", Notes = "Fruit" };
        var product2 = new Product { Name = "Banana", Notes = "Yellow fruit" };
        var product3 = new Product { Name = "Bread", Notes = "Bakery item" };
        _context.Products.AddRange(product1, product2, product3);
        await _context.SaveChangesAsync();

        var component = RenderComponent<Products>();

        // Act - Search for "fruit"
        var searchInput = component.Find("[data-testid='search-input']");
        searchInput.Change("fruit");
        component.Find("[data-testid='search-button']").Click();

        // Assert
        component.WaitForAssertion(() =>
        {
            var rows = component.FindAll("[data-testid='product-row']");
            Assert.Equal(2, rows.Count); // Apple and Banana should match
            
            var searchInfo = component.Find("[data-testid='search-results-info']");
            Assert.Contains("Showing 2 of 3 products", searchInfo.TextContent);
        });
    }

    [Fact]
    public async Task SearchFunctionality_ShowsNoResults_WhenNoMatches()
    {
        // Arrange
        var product = new Product { Name = "Apple", Notes = "Fruit" };
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        var component = RenderComponent<Products>();

        // Act
        var searchInput = component.Find("[data-testid='search-input']");
        searchInput.Change("xyz");
        component.Find("[data-testid='search-button']").Click();

        // Assert
        component.WaitForAssertion(() =>
        {
            var noResultsMessage = component.Find("[data-testid='no-search-results-message']");
            Assert.Contains("No products found matching \"xyz\"", noResultsMessage.TextContent);
        });
    }

    [Fact]
    public async Task EditButton_LoadsProductData_WhenClicked()
    {
        // Arrange
        var store1 = new Store { Name = "Test Store", Notes = "Test" };
        _context.Stores.Add(store1);
        
        var product = new Product { Name = "Test Product", Notes = "Test Notes" };
        _context.Products.Add(product);
        await _context.SaveChangesAsync();
        
        _context.ProductStores.Add(new ProductStore { ProductId = product.Id, StoreId = store1.Id });
        await _context.SaveChangesAsync();

        var component = RenderComponent<Products>();

        // Act
        component.WaitForAssertion(() =>
        {
            var editButton = component.Find("[data-testid='edit-product-button']");
            editButton.Click();
        });

        // Assert
        component.WaitForAssertion(() =>
        {
            var nameInput = component.Find("[data-testid='product-name-input']");
            var notesInput = component.Find("[data-testid='product-notes-input']");
            var storeCheckbox = component.Find($"[data-testid='store-checkbox-{store1.Id}']");
            
            Assert.Equal("Test Product", nameInput.GetAttribute("value"));
            Assert.Equal("Test Notes", notesInput.GetAttribute("value") ?? notesInput.TextContent);
            Assert.True(storeCheckbox.HasAttribute("checked"));
        });
    }

    [Fact]
    public async Task DeleteButton_RemovesProduct_WhenClicked()
    {
        // Arrange
        var product = new Product { Name = "Product to Delete", Notes = "Will be deleted" };
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        var component = RenderComponent<Products>();

        // Act
        component.WaitForAssertion(() =>
        {
            var deleteButton = component.Find("[data-testid='delete-product-button']");
            deleteButton.Click();
        });

        // Assert
        component.WaitForAssertion(() =>
        {
            var products = _context.Products.ToList();
            Assert.Empty(products);
        });
        
        component.WaitForAssertion(() =>
        {
            var noProductsMessage = component.Find("[data-testid='no-products-message']");
            Assert.NotNull(noProductsMessage);
        });
    }

    [Fact]
    public void CancelButton_HidesForm_WhenClicked()
    {
        // Arrange
        var component = RenderComponent<Products>();
        component.Find("[data-testid='create-product-button']").Click();

        // Act
        component.Find("[data-testid='cancel-button']").Click();

        // Assert
        Assert.Throws<Bunit.ElementNotFoundException>(() => component.Find("[data-testid='product-form']"));
    }

    [Fact]
    public async Task StoreAssignment_WorksCorrectly_WithMultipleStores()
    {
        // Arrange
        var store1 = new Store { Name = "Store 1" };
        var store2 = new Store { Name = "Store 2" };
        var store3 = new Store { Name = "Store 3" };
        _context.Stores.AddRange(store1, store2, store3);
        await _context.SaveChangesAsync();

        var component = RenderComponent<Products>();
        component.Find("[data-testid='create-product-button']").Click();

        // Act
        var nameInput = component.Find("[data-testid='product-name-input']");
        nameInput.Change("Multi-store Product");
        
        component.WaitForAssertion(() =>
        {
            var checkbox1 = component.Find($"[data-testid='store-checkbox-{store1.Id}']");
            var checkbox3 = component.Find($"[data-testid='store-checkbox-{store3.Id}']");
            checkbox1.Change(true);
            checkbox3.Change(true);
        });
        
        component.Find("[data-testid='save-product-button']").Click();

        // Assert
        component.WaitForAssertion(() =>
        {
            var product = _context.Products.Include(p => p.ProductStores).First();
            Assert.Equal(2, product.ProductStores.Count);
            Assert.Contains(product.ProductStores, ps => ps.StoreId == store1.Id);
            Assert.Contains(product.ProductStores, ps => ps.StoreId == store3.Id);
        });
    }

    [Fact]
    public async Task NoStoresWarning_Shows_WhenNoStoresAvailable()
    {
        // Arrange - No stores in database
        var component = RenderComponent<Products>();
        component.Find("[data-testid='create-product-button']").Click();

        // Assert
        component.WaitForAssertion(() =>
        {
            var warning = component.Find("[data-testid='no-stores-warning']");
            Assert.Contains("No stores available. Please add stores first.", warning.TextContent);
        });
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _context.Dispose();
        }
        base.Dispose(disposing);
    }
}
