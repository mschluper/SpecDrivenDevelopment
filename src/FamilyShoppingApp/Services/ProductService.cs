using Microsoft.EntityFrameworkCore;
using FamilyShoppingApp.Data;
using FamilyShoppingApp.Models;
using FamilyShoppingApp.ViewModels;

namespace FamilyShoppingApp.Services;

public class ProductService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    
    public ProductService(IDbContextFactory<ApplicationDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }
    
    public async Task<List<ProductViewModel>> GetAllProductsAsync()
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var products = await context.Products
            .Include(p => p.ProductStores)
            .OrderBy(p => p.Name)
            .ToListAsync();
            
        return products.Select(p => new ProductViewModel
        {
            Id = p.Id,
            Name = p.Name,
            Notes = p.Notes,
            SelectedStoreIds = p.ProductStores.Select(ps => ps.StoreId).ToHashSet()
        }).ToList();
    }
    
    public async Task<List<ProductViewModel>> SearchProductsAsync(string searchTerm)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.Products
            .Include(p => p.ProductStores)
            .AsQueryable();
            
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(p => EF.Functions.Like(p.Name, $"%{searchTerm}%") || 
                                   (p.Notes != null && EF.Functions.Like(p.Notes, $"%{searchTerm}%")));
        }
        
        var products = await query
            .OrderBy(p => p.Name)
            .ToListAsync();
            
        return products.Select(p => new ProductViewModel
        {
            Id = p.Id,
            Name = p.Name,
            Notes = p.Notes,
            SelectedStoreIds = p.ProductStores.Select(ps => ps.StoreId).ToHashSet()
        }).ToList();
    }
    
    public async Task<ProductViewModel?> GetProductByIdAsync(int id)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var product = await context.Products
            .Include(p => p.ProductStores)
            .FirstOrDefaultAsync(p => p.Id == id);
        
        if (product == null) return null;
        
        return new ProductViewModel
        {
            Id = product.Id,
            Name = product.Name,
            Notes = product.Notes,
            SelectedStoreIds = product.ProductStores.Select(ps => ps.StoreId).ToHashSet()
        };
    }
    
    public async Task<int> CreateProductAsync(ProductViewModel productViewModel)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var product = new Product
        {
            Name = productViewModel.Name,
            Notes = productViewModel.Notes
        };
        
        context.Products.Add(product);
        await context.SaveChangesAsync();
        
        // Add store associations
        foreach (var storeId in productViewModel.SelectedStoreIds)
        {
            context.ProductStores.Add(new ProductStore
            {
                ProductId = product.Id,
                StoreId = storeId
            });
        }
        
        await context.SaveChangesAsync();
        
        return product.Id;
    }
    
    public async Task UpdateProductAsync(ProductViewModel productViewModel)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var product = await context.Products
            .Include(p => p.ProductStores)
            .FirstOrDefaultAsync(p => p.Id == productViewModel.Id);
        
        if (product != null)
        {
            product.Name = productViewModel.Name;
            product.Notes = productViewModel.Notes;
            
            // Remove existing store associations
            context.ProductStores.RemoveRange(product.ProductStores);
            
            // Add new store associations
            foreach (var storeId in productViewModel.SelectedStoreIds)
            {
                context.ProductStores.Add(new ProductStore
                {
                    ProductId = product.Id,
                    StoreId = storeId
                });
            }
            
            await context.SaveChangesAsync();
        }
    }
    
    public async Task DeleteProductAsync(int id)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var product = await context.Products
            .Include(p => p.ProductStores)
            .FirstOrDefaultAsync(p => p.Id == id);
        
        if (product != null)
        {
            context.ProductStores.RemoveRange(product.ProductStores);
            context.Products.Remove(product);
            await context.SaveChangesAsync();
        }
    }
}
