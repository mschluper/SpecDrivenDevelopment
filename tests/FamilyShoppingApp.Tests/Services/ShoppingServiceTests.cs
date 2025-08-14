using Xunit;
using Microsoft.EntityFrameworkCore;
using FamilyShoppingApp.Data;
using FamilyShoppingApp.Services;
using FamilyShoppingApp.ViewModels;
using FamilyShoppingApp.Models;
using FamilyShoppingApp.Tests.Helpers;

namespace FamilyShoppingApp.Tests.Services;

public class ShoppingServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly ShoppingService _shoppingService;

    public ShoppingServiceTests()
    {
        var dbName = Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        _context = new ApplicationDbContext(options);
        
        var factory = new TestDbContextFactory(options);
        _contextFactory = factory;
        _shoppingService = new ShoppingService(_contextFactory);
    }

    [Fact]
    public async Task GetActiveShoppingItemsAsync_ReturnsEmptyList_WhenNoItems()
    {
        // Act
        var result = await _shoppingService.GetActiveShoppingItemsAsync();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetActiveShoppingItemsAsync_ReturnsOnlyActiveItems()
    {
        // Arrange
        var store = new Store { Name = "Store 1" };
        _context.Stores.Add(store);
        
        var product1 = new Product { Name = "Product 1", Notes = "Notes 1" };
        var product2 = new Product { Name = "Product 2", Notes = "Notes 2" };
        _context.Products.AddRange(product1, product2);
        await _context.SaveChangesAsync();
        
        _context.ProductStores.Add(new ProductStore { ProductId = product1.Id, StoreId = store.Id });
        await _context.SaveChangesAsync();
        
        var activeItem = new ShoppingItem 
        { 
            ProductId = product1.Id, 
            Quantity = 2, 
            IsPurchased = false, 
            CreatedAt = DateTime.UtcNow 
        };
        var purchasedItem = new ShoppingItem 
        { 
            ProductId = product2.Id, 
            Quantity = 1, 
            IsPurchased = true, 
            CreatedAt = DateTime.UtcNow 
        };
        _context.ShoppingItems.AddRange(activeItem, purchasedItem);
        await _context.SaveChangesAsync();

        // Act
        var result = await _shoppingService.GetActiveShoppingItemsAsync();

        // Assert
        Assert.Single(result);
        Assert.Equal("Product 1", result[0].ProductName);
        Assert.Equal(2, result[0].Quantity);
        Assert.False(result[0].IsPurchased);
        Assert.Single(result[0].AvailableStoreIds);
        Assert.Contains(store.Id, result[0].AvailableStoreIds);
    }

    [Fact]
    public async Task AddShoppingItemAsync_CreatesNewItem_WhenNotExists()
    {
        // Arrange
        var product = new Product { Name = "Test Product" };
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        // Act
        var result = await _shoppingService.AddShoppingItemAsync(product.Id, 3);

        // Assert
        Assert.True(result > 0);
        
        using var verifyContext = await _contextFactory.CreateDbContextAsync();
        var createdItem = await verifyContext.ShoppingItems.FindAsync(result);
        Assert.NotNull(createdItem);
        Assert.Equal(product.Id, createdItem.ProductId);
        Assert.Equal(3, createdItem.Quantity);
        Assert.False(createdItem.IsPurchased);
    }

    [Fact]
    public async Task AddShoppingItemAsync_UpdatesQuantity_WhenActiveItemExists()
    {
        // Arrange
        var product = new Product { Name = "Test Product" };
        _context.Products.Add(product);
        await _context.SaveChangesAsync();
        
        var existingItem = new ShoppingItem 
        { 
            ProductId = product.Id, 
            Quantity = 2, 
            IsPurchased = false, 
            CreatedAt = DateTime.UtcNow 
        };
        _context.ShoppingItems.Add(existingItem);
        await _context.SaveChangesAsync();

        // Act
        var result = await _shoppingService.AddShoppingItemAsync(product.Id, 3);

        // Assert
        Assert.Equal(existingItem.Id, result);
        
        using var verifyContext = await _contextFactory.CreateDbContextAsync();
        var updatedItem = await verifyContext.ShoppingItems.FindAsync(existingItem.Id);
        Assert.NotNull(updatedItem);
        Assert.Equal(5, updatedItem.Quantity); // 2 + 3
    }

    [Fact]
    public async Task UpdateQuantityAsync_UpdatesQuantity_WhenQuantityValid()
    {
        // Arrange
        var product = new Product { Name = "Test Product" };
        _context.Products.Add(product);
        
        var shoppingItem = new ShoppingItem 
        { 
            ProductId = product.Id, 
            Quantity = 2, 
            IsPurchased = false, 
            CreatedAt = DateTime.UtcNow 
        };
        _context.ShoppingItems.Add(shoppingItem);
        await _context.SaveChangesAsync();

        // Act
        await _shoppingService.UpdateQuantityAsync(shoppingItem.Id, 5);

        // Assert
        using var verifyContext = await _contextFactory.CreateDbContextAsync();
        var updatedItem = await verifyContext.ShoppingItems.FindAsync(shoppingItem.Id);
        Assert.NotNull(updatedItem);
        Assert.Equal(5, updatedItem.Quantity);
    }

    [Fact]
    public async Task UpdateQuantityAsync_RemovesItem_WhenQuantityZeroOrNegative()
    {
        // Arrange
        var product = new Product { Name = "Test Product" };
        _context.Products.Add(product);
        
        var shoppingItem = new ShoppingItem 
        { 
            ProductId = product.Id, 
            Quantity = 2, 
            IsPurchased = false, 
            CreatedAt = DateTime.UtcNow 
        };
        _context.ShoppingItems.Add(shoppingItem);
        await _context.SaveChangesAsync();

        // Act
        await _shoppingService.UpdateQuantityAsync(shoppingItem.Id, 0);

        // Assert
        using var verifyContext = await _contextFactory.CreateDbContextAsync();
        var deletedItem = await verifyContext.ShoppingItems.FindAsync(shoppingItem.Id);
        Assert.Null(deletedItem);
    }

    [Fact]
    public async Task MarkAsPurchasedAsync_MarksItemAsPurchased()
    {
        // Arrange
        var product = new Product { Name = "Test Product" };
        _context.Products.Add(product);
        
        var shoppingItem = new ShoppingItem 
        { 
            ProductId = product.Id, 
            Quantity = 2, 
            IsPurchased = false, 
            CreatedAt = DateTime.UtcNow 
        };
        _context.ShoppingItems.Add(shoppingItem);
        await _context.SaveChangesAsync();

        // Act
        await _shoppingService.MarkAsPurchasedAsync(shoppingItem.Id);

        // Assert
        using var verifyContext = await _contextFactory.CreateDbContextAsync();
        var updatedItem = await verifyContext.ShoppingItems.FindAsync(shoppingItem.Id);
        Assert.NotNull(updatedItem);
        Assert.True(updatedItem.IsPurchased);
        Assert.NotNull(updatedItem.PurchasedAt);
    }

    [Fact]
    public async Task RemoveShoppingItemAsync_RemovesItem()
    {
        // Arrange
        var product = new Product { Name = "Test Product" };
        _context.Products.Add(product);
        
        var shoppingItem = new ShoppingItem 
        { 
            ProductId = product.Id, 
            Quantity = 2, 
            IsPurchased = false, 
            CreatedAt = DateTime.UtcNow 
        };
        _context.ShoppingItems.Add(shoppingItem);
        await _context.SaveChangesAsync();

        // Act
        await _shoppingService.RemoveShoppingItemAsync(shoppingItem.Id);

        // Assert
        using var verifyContext = await _contextFactory.CreateDbContextAsync();
        var deletedItem = await verifyContext.ShoppingItems.FindAsync(shoppingItem.Id);
        Assert.Null(deletedItem);
    }

    [Fact]
    public async Task GetStoreAvailabilityAsync_ReturnsEmptyList_WhenNoActiveItems()
    {
        // Act
        var result = await _shoppingService.GetStoreAvailabilityAsync();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetStoreAvailabilityAsync_CalculatesAvailabilityCorrectly()
    {
        // Arrange
        var store1 = new Store { Name = "Store 1" };
        var store2 = new Store { Name = "Store 2" };
        _context.Stores.AddRange(store1, store2);
        
        var product1 = new Product { Name = "Product 1" };
        var product2 = new Product { Name = "Product 2" };
        var product3 = new Product { Name = "Product 3" };
        _context.Products.AddRange(product1, product2, product3);
        await _context.SaveChangesAsync();
        
        // Store 1 has products 1 and 2, Store 2 has only product 1
        _context.ProductStores.AddRange(
            new ProductStore { ProductId = product1.Id, StoreId = store1.Id },
            new ProductStore { ProductId = product2.Id, StoreId = store1.Id },
            new ProductStore { ProductId = product1.Id, StoreId = store2.Id }
        );
        await _context.SaveChangesAsync();
        
        // Add shopping items for all 3 products
        _context.ShoppingItems.AddRange(
            new ShoppingItem { ProductId = product1.Id, Quantity = 1, IsPurchased = false, CreatedAt = DateTime.UtcNow },
            new ShoppingItem { ProductId = product2.Id, Quantity = 1, IsPurchased = false, CreatedAt = DateTime.UtcNow },
            new ShoppingItem { ProductId = product3.Id, Quantity = 1, IsPurchased = false, CreatedAt = DateTime.UtcNow }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _shoppingService.GetStoreAvailabilityAsync();

        // Assert
        Assert.Equal(2, result.Count);
        
        var store1Availability = result.First(s => s.StoreName == "Store 1");
        Assert.Equal(3, store1Availability.TotalItemsCount);
        Assert.Equal(2, store1Availability.AvailableItemsCount);
        Assert.Equal(66.7m, store1Availability.CoveragePercentage);
        
        var store2Availability = result.First(s => s.StoreName == "Store 2");
        Assert.Equal(3, store2Availability.TotalItemsCount);
        Assert.Equal(1, store2Availability.AvailableItemsCount);
        Assert.Equal(33.3m, store2Availability.CoveragePercentage);
    }

    [Fact]
    public async Task ClearAllPurchasedAsync_RemovesPurchasedItems()
    {
        // Arrange
        var product = new Product { Name = "Test Product" };
        _context.Products.Add(product);
        await _context.SaveChangesAsync();
        
        var activeItem = new ShoppingItem 
        { 
            ProductId = product.Id, 
            Quantity = 1, 
            IsPurchased = false, 
            CreatedAt = DateTime.UtcNow 
        };
        var purchasedItem = new ShoppingItem 
        { 
            ProductId = product.Id, 
            Quantity = 2, 
            IsPurchased = true, 
            CreatedAt = DateTime.UtcNow,
            PurchasedAt = DateTime.UtcNow
        };
        _context.ShoppingItems.AddRange(activeItem, purchasedItem);
        await _context.SaveChangesAsync();

        // Act
        await _shoppingService.ClearAllPurchasedAsync();

        // Assert
        using var verifyContext = await _contextFactory.CreateDbContextAsync();
        var remainingItems = await verifyContext.ShoppingItems.ToListAsync();
        Assert.Single(remainingItems);
        Assert.False(remainingItems[0].IsPurchased);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
