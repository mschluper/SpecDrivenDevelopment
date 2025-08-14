using Xunit;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using FamilyShoppingApp.Pages;
using FamilyShoppingApp.Data;
using FamilyShoppingApp.Models;
using FamilyShoppingApp.Services;
using FamilyShoppingApp.Tests.Helpers;

namespace FamilyShoppingApp.Tests.Pages;

public class DashboardPageTests : TestContext, IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

    public DashboardPageTests()
    {
        var dbName = Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        _context = new ApplicationDbContext(options);
        
        var factory = new TestDbContextFactory(options);
        _contextFactory = factory;

        Services.AddScoped<IDbContextFactory<ApplicationDbContext>>(_ => _contextFactory);
        Services.AddScoped<ProductService>();
        Services.AddScoped<ShoppingService>();
        Services.AddScoped<ExceptionHandlerService>();
    }

    [Fact]
    public void Dashboard_RendersCorrectly_WhenEmpty()
    {
        // Act
        var component = RenderComponent<Dashboard>();
        component.WaitForAssertion(() =>
        {
            Assert.Contains("Shopping Dashboard", component.Markup);
        });

        // Assert
        Assert.Contains("Shopping Dashboard", component.Markup);
        var addButton = component.Find("[data-testid='show-add-form-button']");
        Assert.NotNull(addButton);
        
        var emptyMessage = component.Find("[data-testid='empty-shopping-list-message']");
        Assert.Contains("Your shopping list is empty", emptyMessage.TextContent);
    }

    [Fact]
    public void ShowAddForm_DisplaysAddProductForm_WhenClicked()
    {
        // Arrange
        var component = RenderComponent<Dashboard>();
        component.WaitForAssertion(() =>
        {
            Assert.Contains("Shopping Dashboard", component.Markup);
        });

        // Act
        var addButton = component.Find("[data-testid='show-add-form-button']");
        addButton.Click();

        // Assert
        var form = component.Find("[data-testid='add-product-form']");
        Assert.NotNull(form);
        Assert.Contains("Add Product to Shopping List", form.TextContent);
        
        var productSelect = component.Find("[data-testid='product-select']");
        var quantityInput = component.Find("[data-testid='quantity-input']");
        var submitButton = component.Find("[data-testid='add-to-list-button']");
        
        Assert.NotNull(productSelect);
        Assert.NotNull(quantityInput);
        Assert.NotNull(submitButton);
        Assert.True(submitButton.HasAttribute("disabled"));
    }

    [Fact]
    public async Task AddProductForm_EnablesSubmitButton_WhenFormValid()
    {
        // Arrange
        var product = new Product { Name = "Test Product", Notes = "Test notes" };
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        var component = RenderComponent<Dashboard>();
        component.WaitForAssertion(() =>
        {
            Assert.Contains("Shopping Dashboard", component.Markup);
        });
        var addButton = component.Find("[data-testid='show-add-form-button']");
        addButton.Click();

        // Act
        var productSelect = component.Find("[data-testid='product-select']");
        var quantityInput = component.Find("[data-testid='quantity-input']");
        
        productSelect.Change(product.Id.ToString());
        quantityInput.Change("2");

        // Assert
        component.WaitForAssertion(() =>
        {
            var submitButton = component.Find("[data-testid='add-to-list-button']");
            Assert.False(submitButton.HasAttribute("disabled"));
        });
    }

    [Fact]
    public void AddProductForm_ShowsValidation_WhenProductNotSelected()
    {
        // Arrange
        var component = RenderComponent<Dashboard>();
        component.WaitForAssertion(() =>
        {
            Assert.Contains("Shopping Dashboard", component.Markup);
        });
        var addButton = component.Find("[data-testid='show-add-form-button']");
        addButton.Click();

        // Act
        var productSelect = component.Find("[data-testid='product-select']");
        productSelect.Change("0"); // Invalid selection

        // Try to submit the form
        var submitButton = component.Find("[data-testid='add-to-list-button']");
        
        // Assert
        Assert.True(submitButton.HasAttribute("disabled"));
    }

    [Fact]
    public async Task AddProductForm_AddsProductToList_WhenSubmitted()
    {
        // Arrange
        var product = new Product { Name = "Test Product", Notes = "Test notes" };
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        var component = RenderComponent<Dashboard>();
        component.WaitForAssertion(() =>
        {
            Assert.Contains("Shopping Dashboard", component.Markup);
        });
        var addButton = component.Find("[data-testid='show-add-form-button']");
        addButton.Click();

        var productSelect = component.Find("[data-testid='product-select']");
        var quantityInput = component.Find("[data-testid='quantity-input']");
        productSelect.Change(product.Id.ToString());
        quantityInput.Change("3");

        // Act
        var submitButton = component.Find("[data-testid='add-to-list-button']");
        submitButton.Click();

        // Assert
        component.WaitForAssertion(() =>
        {
            var shoppingList = component.Find("[data-testid='shopping-list']");
            Assert.NotNull(shoppingList);
            
            var productRows = component.FindAll("[data-testid='shopping-item-row']");
            Assert.Single(productRows);
            Assert.Contains("Test Product", productRows[0].TextContent);
            Assert.Contains("3", productRows[0].TextContent);
        });
    }

    [Fact]
    public void CancelAddForm_HidesForm_WhenClicked()
    {
        // Arrange
        var component = RenderComponent<Dashboard>();
        component.WaitForAssertion(() =>
        {
            Assert.Contains("Shopping Dashboard", component.Markup);
        });
        var addButton = component.Find("[data-testid='show-add-form-button']");
        addButton.Click();

        // Act
        var cancelButton = component.Find("[data-testid='cancel-add-button']");
        cancelButton.Click();

        // Assert
        component.WaitForAssertion(() =>
        {
            Assert.Throws<Bunit.ElementNotFoundException>(() => 
                component.Find("[data-testid='add-product-form']"));
            
            var showAddButton = component.Find("[data-testid='show-add-form-button']");
            Assert.NotNull(showAddButton);
        });
    }

    [Fact]
    public async Task QuantityControls_UpdateQuantity_WhenClicked()
    {
        // Arrange
        var product = new Product { Name = "Test Product" };
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        var shoppingItem = new ShoppingItem 
        { 
            ProductId = product.Id, 
            Quantity = 2, 
            IsPurchased = false, 
            CreatedAt = DateTime.UtcNow 
        };
        _context.ShoppingItems.Add(shoppingItem);
        await _context.SaveChangesAsync();

        var component = RenderComponent<Dashboard>();
        component.WaitForAssertion(() =>
        {
            Assert.Contains("Shopping Dashboard", component.Markup);
        });

        // Act - Increment quantity
        var incrementButton = component.Find("[data-testid='increment-button']");
        incrementButton.Click();

        // Assert
        component.WaitForAssertion(() =>
        {
            var quantityDisplay = component.Find("[data-testid='quantity-display']");
            Assert.Equal("3", quantityDisplay.TextContent);
        });

        // Act - Decrement quantity
        var decrementButton = component.Find("[data-testid='decrement-button']");
        decrementButton.Click();

        // Assert
        component.WaitForAssertion(() =>
        {
            var quantityDisplay = component.Find("[data-testid='quantity-display']");
            Assert.Equal("2", quantityDisplay.TextContent);
        });
    }

    [Fact]
    public async Task MarkAsPurchased_RemovesItemFromActiveList_WhenClicked()
    {
        // Arrange
        var product = new Product { Name = "Test Product" };
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        var shoppingItem = new ShoppingItem 
        { 
            ProductId = product.Id, 
            Quantity = 1, 
            IsPurchased = false, 
            CreatedAt = DateTime.UtcNow 
        };
        _context.ShoppingItems.Add(shoppingItem);
        await _context.SaveChangesAsync();

        var component = RenderComponent<Dashboard>();
        component.WaitForAssertion(() =>
        {
            Assert.Contains("Shopping Dashboard", component.Markup);
        });

        // Act
        var markPurchasedButton = component.Find("[data-testid='mark-purchased-button']");
        markPurchasedButton.Click();

        // Assert - item should be removed from active list
        component.WaitForAssertion(() =>
        {
            Assert.Throws<Bunit.ElementNotFoundException>(() => 
                component.Find("[data-testid='shopping-item-row']"));
            
            // Should show clear purchased button now
            var clearButton = component.Find("[data-testid='clear-purchased-button']");
            Assert.NotNull(clearButton);
        });
    }

    [Fact]
    public async Task RemoveItem_RemovesItemFromList_WhenClicked()
    {
        // Arrange
        var product = new Product { Name = "Test Product" };
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        var shoppingItem = new ShoppingItem 
        { 
            ProductId = product.Id, 
            Quantity = 1, 
            IsPurchased = false, 
            CreatedAt = DateTime.UtcNow 
        };
        _context.ShoppingItems.Add(shoppingItem);
        await _context.SaveChangesAsync();

        var component = RenderComponent<Dashboard>();

        // Act
        var removeButton = component.Find("[data-testid='remove-item-button']");
        removeButton.Click();

        // Assert
        component.WaitForAssertion(() =>
        {
            Assert.Throws<Bunit.ElementNotFoundException>(() => 
                component.Find("[data-testid='shopping-item-row']"));
            
            var emptyMessage = component.Find("[data-testid='empty-shopping-list-message']");
            Assert.NotNull(emptyMessage);
        });
    }

    [Fact]
    public async Task StoreAvailabilityDisplay_ShowsCorrectAvailability()
    {
        // Arrange
        var store1 = new Store { Name = "Store 1" };
        var store2 = new Store { Name = "Store 2" };
        _context.Stores.AddRange(store1, store2);

        var product1 = new Product { Name = "Product 1" };
        var product2 = new Product { Name = "Product 2" };
        _context.Products.AddRange(product1, product2);
        await _context.SaveChangesAsync();

        // Store 1 has both products, Store 2 has only product 1
        _context.ProductStores.AddRange(
            new ProductStore { ProductId = product1.Id, StoreId = store1.Id },
            new ProductStore { ProductId = product2.Id, StoreId = store1.Id },
            new ProductStore { ProductId = product1.Id, StoreId = store2.Id }
        );

        _context.ShoppingItems.AddRange(
            new ShoppingItem { ProductId = product1.Id, Quantity = 1, IsPurchased = false, CreatedAt = DateTime.UtcNow },
            new ShoppingItem { ProductId = product2.Id, Quantity = 1, IsPurchased = false, CreatedAt = DateTime.UtcNow }
        );
        await _context.SaveChangesAsync();

        var component = RenderComponent<Dashboard>();

        // Assert
        component.WaitForAssertion(() =>
        {
            var coverageRows = component.FindAll("[data-testid='store-coverage-row']");
            Assert.Equal(2, coverageRows.Count);

            var store1Row = coverageRows.FirstOrDefault(r => r.TextContent.Contains("Store 1"));
            var store2Row = coverageRows.FirstOrDefault(r => r.TextContent.Contains("Store 2"));
            
            Assert.NotNull(store1Row);
            Assert.NotNull(store2Row);
            Assert.Contains("2 of 2 items available", store1Row.TextContent);
            Assert.Contains("1 of 2 items available", store2Row.TextContent);
        });
    }

    [Fact]
    public void QuickLinks_ProvideNavigationToOtherPages()
    {
        // Arrange
        var component = RenderComponent<Dashboard>();

        // Assert
        var productsLink = component.Find("[data-testid='products-link']");
        var storesLink = component.Find("[data-testid='stores-link']");
        
        Assert.NotNull(productsLink);
        Assert.NotNull(storesLink);
        Assert.Equal("/products", productsLink.GetAttribute("href"));
        Assert.Equal("/stores", storesLink.GetAttribute("href"));
    }

    [Fact]
    public async Task ClearPurchasedButton_ShowsOnlyWhenPurchasedItemsExist()
    {
        // Arrange
        var product = new Product { Name = "Test Product" };
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        // Add an active item first
        var activeItem = new ShoppingItem 
        { 
            ProductId = product.Id, 
            Quantity = 1, 
            IsPurchased = false, 
            CreatedAt = DateTime.UtcNow 
        };
        _context.ShoppingItems.Add(activeItem);
        await _context.SaveChangesAsync();

        var component = RenderComponent<Dashboard>();

        // Initially no clear button should be visible (no purchased items)
        Assert.Throws<Bunit.ElementNotFoundException>(() => 
            component.Find("[data-testid='clear-purchased-button']"));

        // Act - Mark the item as purchased by clicking the button
        var markPurchasedButton = component.Find("[data-testid='mark-purchased-button']");
        markPurchasedButton.Click();

        // Assert - clear button should now be visible
        component.WaitForAssertion(() =>
        {
            var clearButton = component.Find("[data-testid='clear-purchased-button']");
            Assert.NotNull(clearButton);
        });
    }

    public new void Dispose()
    {
        _context.Dispose();
        base.Dispose();
    }
}
