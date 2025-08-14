using Xunit;
using Microsoft.EntityFrameworkCore;
using FamilyShoppingApp.Data;
using FamilyShoppingApp.Services;
using FamilyShoppingApp.ViewModels;
using FamilyShoppingApp.Models;
using FamilyShoppingApp.Tests.Helpers;

namespace FamilyShoppingApp.Tests.Services;

public class StoreServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly StoreService _storeService;
    private readonly string _dbName;

    public StoreServiceTests()
    {
        _dbName = Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: _dbName)
            .Options;

        _context = new ApplicationDbContext(options);
        
        var factory = new TestDbContextFactory(options);
        _contextFactory = factory;
        _storeService = new StoreService(_contextFactory);
    }

    [Fact]
    public async Task GetAllStoresAsync_ReturnsEmptyList_WhenNoStores()
    {
        // Act
        var result = await _storeService.GetAllStoresAsync();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllStoresAsync_ReturnsStores_OrderedByName()
    {
        // Arrange
        var store1 = new Store { Name = "Zebra Store", Notes = "Last store" };
        var store2 = new Store { Name = "Alpha Store", Notes = "First store" };
        
        _context.Stores.AddRange(store1, store2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _storeService.GetAllStoresAsync();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("Alpha Store", result[0].Name);
        Assert.Equal("Zebra Store", result[1].Name);
    }

    [Fact]
    public async Task CreateStoreAsync_CreatesStore_ReturnsId()
    {
        // Arrange
        var storeViewModel = new StoreViewModel
        {
            Name = "Test Store",
            Notes = "Test notes"
        };

        // Act
        var id = await _storeService.CreateStoreAsync(storeViewModel);

        // Assert
        Assert.True(id > 0);
        
        var createdStore = await _context.Stores.FindAsync(id);
        Assert.NotNull(createdStore);
        Assert.Equal("Test Store", createdStore.Name);
        Assert.Equal("Test notes", createdStore.Notes);
    }

    [Fact]
    public async Task GetStoreByIdAsync_ReturnsStore_WhenExists()
    {
        // Arrange
        var store = new Store { Name = "Test Store", Notes = "Test notes" };
        _context.Stores.Add(store);
        await _context.SaveChangesAsync();

        // Act
        var result = await _storeService.GetStoreByIdAsync(store.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test Store", result.Name);
        Assert.Equal("Test notes", result.Notes);
    }

    [Fact]
    public async Task GetStoreByIdAsync_ReturnsNull_WhenNotExists()
    {
        // Act
        var result = await _storeService.GetStoreByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateStoreAsync_UpdatesStore_WhenExists()
    {
        // Arrange
        var store = new Store { Name = "Original Name", Notes = "Original notes" };
        _context.Stores.Add(store);
        await _context.SaveChangesAsync();

        var updateViewModel = new StoreViewModel
        {
            Id = store.Id,
            Name = "Updated Name",
            Notes = "Updated notes"
        };

        // Act
        await _storeService.UpdateStoreAsync(updateViewModel);

        // Assert
        var updatedStore = await _storeService.GetStoreByIdAsync(store.Id);
        Assert.NotNull(updatedStore);
        Assert.Equal("Updated Name", updatedStore.Name);
        Assert.Equal("Updated notes", updatedStore.Notes);
    }

    [Fact]
    public async Task DeleteStoreAsync_DeletesStore_WhenExists()
    {
        // Arrange
        var store = new Store { Name = "Store to Delete", Notes = "Will be deleted" };
        _context.Stores.Add(store);
        await _context.SaveChangesAsync();
        var cnt = _context.Stores.Count();

        // Act
        await _storeService.DeleteStoreAsync(store.Id);

        // Assert
        var deletedStore = await _storeService.GetStoreByIdAsync(store.Id);
        Assert.Null(deletedStore);   
        Assert.Equal(1, cnt);
        Assert.Equal(0, _context.Stores.Count());
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
