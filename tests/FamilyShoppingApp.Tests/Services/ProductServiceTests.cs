using Xunit;
using Microsoft.EntityFrameworkCore;
using FamilyShoppingApp.Data;
using FamilyShoppingApp.Services;
using FamilyShoppingApp.ViewModels;
using FamilyShoppingApp.Models;
using FamilyShoppingApp.Tests.Helpers;

namespace FamilyShoppingApp.Tests.Services;

public class ProductServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly ProductService _productService;
    private readonly string _dbName;

    public ProductServiceTests()
    {
        _dbName = Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: _dbName)
            .Options;

        _context = new ApplicationDbContext(options);
        
        var factory = new TestDbContextFactory(options);
        _contextFactory = factory;
        _productService = new ProductService(_contextFactory);
    }

    [Fact]
    public async Task GetAllProductsAsync_ReturnsEmptyList_WhenNoProducts()
    {
        // Act
        var result = await _productService.GetAllProductsAsync();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllProductsAsync_ReturnsProducts_OrderedByName()
    {
        // Arrange
        var store1 = new Store { Name = "Store 1" };
        var store2 = new Store { Name = "Store 2" };
        _context.Stores.AddRange(store1, store2);
        
        var product1 = new Product { Name = "Zebra Product", Notes = "Last product" };
        var product2 = new Product { Name = "Alpha Product", Notes = "First product" };
        _context.Products.AddRange(product1, product2);
        await _context.SaveChangesAsync();

        _context.ProductStores.AddRange(
            new ProductStore { ProductId = product1.Id, StoreId = store1.Id },
            new ProductStore { ProductId = product2.Id, StoreId = store1.Id },
            new ProductStore { ProductId = product2.Id, StoreId = store2.Id }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _productService.GetAllProductsAsync();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("Alpha Product", result[0].Name);
        Assert.Equal("Zebra Product", result[1].Name);
        Assert.Equal(2, result[0].SelectedStoreIds.Count);
        Assert.Single(result[1].SelectedStoreIds);
    }

    [Fact]
    public async Task CreateProductAsync_CreatesProduct_ReturnsId()
    {
        // Arrange
        var store1 = new Store { Name = "Store 1" };
        var store2 = new Store { Name = "Store 2" };
        _context.Stores.AddRange(store1, store2);
        await _context.SaveChangesAsync();

        var productViewModel = new ProductViewModel
        {
            Name = "Test Product",
            Notes = "Test notes",
            SelectedStoreIds = new HashSet<int> { store1.Id, store2.Id }
        };

        // Act
        var id = await _productService.CreateProductAsync(productViewModel);

        // Assert
        Assert.True(id > 0);
        
        var createdProduct = await _context.Products
            .Include(p => p.ProductStores)
            .FirstOrDefaultAsync(p => p.Id == id);
            
        Assert.NotNull(createdProduct);
        Assert.Equal("Test Product", createdProduct.Name);
        Assert.Equal("Test notes", createdProduct.Notes);
        Assert.Equal(2, createdProduct.ProductStores.Count);
        Assert.Contains(createdProduct.ProductStores, ps => ps.StoreId == store1.Id);
        Assert.Contains(createdProduct.ProductStores, ps => ps.StoreId == store2.Id);
    }

    [Fact]
    public async Task GetProductByIdAsync_ReturnsProduct_WhenExists()
    {
        // Arrange
        var store1 = new Store { Name = "Store 1" };
        _context.Stores.Add(store1);
        
        var product = new Product { Name = "Test Product", Notes = "Test notes" };
        _context.Products.Add(product);
        await _context.SaveChangesAsync();
        
        _context.ProductStores.Add(new ProductStore { ProductId = product.Id, StoreId = store1.Id });
        await _context.SaveChangesAsync();

        // Act
        var result = await _productService.GetProductByIdAsync(product.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test Product", result.Name);
        Assert.Equal("Test notes", result.Notes);
        Assert.Single(result.SelectedStoreIds);
        Assert.Contains(store1.Id, result.SelectedStoreIds);
    }

    [Fact]
    public async Task GetProductByIdAsync_ReturnsNull_WhenNotExists()
    {
        // Act
        var result = await _productService.GetProductByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateProductAsync_UpdatesProduct_WhenExists()
    {
        // Arrange
        var store1 = new Store { Name = "Store 1" };
        var store2 = new Store { Name = "Store 2" };
        var store3 = new Store { Name = "Store 3" };
        _context.Stores.AddRange(store1, store2, store3);
        
        var product = new Product { Name = "Original Name", Notes = "Original notes" };
        _context.Products.Add(product);
        await _context.SaveChangesAsync();
        
        _context.ProductStores.Add(new ProductStore { ProductId = product.Id, StoreId = store1.Id });
        await _context.SaveChangesAsync();

        var updateViewModel = new ProductViewModel
        {
            Id = product.Id,
            Name = "Updated Name",
            Notes = "Updated notes",
            SelectedStoreIds = new HashSet<int> { store2.Id, store3.Id }
        };

        // Act
        await _productService.UpdateProductAsync(updateViewModel);

        // Assert - Create a new context to ensure we see the updated data
        using var verifyContext = await _contextFactory.CreateDbContextAsync();
        var updatedProduct = await verifyContext.Products
            .Include(p => p.ProductStores)
            .FirstOrDefaultAsync(p => p.Id == product.Id);
            
        Assert.NotNull(updatedProduct);
        Assert.Equal("Updated Name", updatedProduct.Name);
        Assert.Equal("Updated notes", updatedProduct.Notes);
        Assert.Equal(2, updatedProduct.ProductStores.Count);
        Assert.Contains(updatedProduct.ProductStores, ps => ps.StoreId == store2.Id);
        Assert.Contains(updatedProduct.ProductStores, ps => ps.StoreId == store3.Id);
        Assert.DoesNotContain(updatedProduct.ProductStores, ps => ps.StoreId == store1.Id);
    }

    [Fact]
    public async Task DeleteProductAsync_DeletesProduct_WhenExists()
    {
        // Arrange
        var store1 = new Store { Name = "Store 1" };
        _context.Stores.Add(store1);
        
        var product = new Product { Name = "Product to Delete", Notes = "Will be deleted" };
        _context.Products.Add(product);
        await _context.SaveChangesAsync();
        
        _context.ProductStores.Add(new ProductStore { ProductId = product.Id, StoreId = store1.Id });
        await _context.SaveChangesAsync();

        // Act
        await _productService.DeleteProductAsync(product.Id);

        // Assert - Create a new context to ensure we see the updated data
        using var verifyContext = await _contextFactory.CreateDbContextAsync();
        var deletedProduct = await verifyContext.Products.FindAsync(product.Id);
        Assert.Null(deletedProduct);
        
        var productStores = await verifyContext.ProductStores
            .Where(ps => ps.ProductId == product.Id)
            .ToListAsync();
        Assert.Empty(productStores);
    }

    [Fact]
    public async Task SearchProductsAsync_ReturnsAllProducts_WhenSearchTermEmpty()
    {
        // Arrange
        var product1 = new Product { Name = "Apple", Notes = "Fruit" };
        var product2 = new Product { Name = "Bread", Notes = "Bakery" };
        _context.Products.AddRange(product1, product2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _productService.SearchProductsAsync("");

        // Assert
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task SearchProductsAsync_FiltersProducts_ByName()
    {
        // Arrange
        var product1 = new Product { Name = "Apple Pie", Notes = "Dessert" };
        var product2 = new Product { Name = "Banana Bread", Notes = "Bakery" };
        var product3 = new Product { Name = "Orange Juice", Notes = "Beverage" };
        _context.Products.AddRange(product1, product2, product3);
        await _context.SaveChangesAsync();

        // Act
        var result = await _productService.SearchProductsAsync("Bread");

        // Assert
        Assert.Single(result);
        Assert.Equal("Banana Bread", result[0].Name);
    }

    [Fact]
    public async Task SearchProductsAsync_FiltersProducts_ByNotes()
    {
        // Arrange
        var product1 = new Product { Name = "Apple Pie", Notes = "Sweet dessert" };
        var product2 = new Product { Name = "Banana Bread", Notes = "Sweet bakery item" };
        var product3 = new Product { Name = "Orange Juice", Notes = "Citrus beverage" };
        _context.Products.AddRange(product1, product2, product3);
        await _context.SaveChangesAsync();

        // Act
        var result = await _productService.SearchProductsAsync("Sweet");

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, p => p.Name == "Apple Pie");
        Assert.Contains(result, p => p.Name == "Banana Bread");
    }

    [Fact]
    public async Task SearchProductsAsync_ReturnsEmpty_WhenNoMatches()
    {
        // Arrange
        var product = new Product { Name = "Apple", Notes = "Fruit" };
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        // Act
        var result = await _productService.SearchProductsAsync("xyz");

        // Assert
        Assert.Empty(result);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
