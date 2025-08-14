using Microsoft.EntityFrameworkCore;
using FamilyShoppingApp.Data;
using FamilyShoppingApp.Models;
using FamilyShoppingApp.ViewModels;

namespace FamilyShoppingApp.Services;

public class ShoppingService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

    public ShoppingService(IDbContextFactory<ApplicationDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<List<ShoppingItemViewModel>> GetActiveShoppingItemsAsync()
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var items = await context.ShoppingItems
            .Include(si => si.Product)
            .ThenInclude(p => p.ProductStores)
            .Where(si => !si.IsPurchased)
            .OrderBy(si => si.CreatedAt)
            .ToListAsync();

        return items.Select(si => new ShoppingItemViewModel
        {
            Id = si.Id,
            ProductId = si.ProductId,
            ProductName = si.Product.Name,
            ProductNotes = si.Product.Notes ?? string.Empty,
            Quantity = si.Quantity,
            IsPurchased = si.IsPurchased,
            CreatedAt = si.CreatedAt,
            PurchasedAt = si.PurchasedAt,
            AvailableStoreIds = si.Product.ProductStores.Select(ps => ps.StoreId).ToHashSet()
        }).ToList();
    }

    public async Task<bool> HasPurchasedItemsAsync()
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.ShoppingItems.AnyAsync(si => si.IsPurchased);
    }

    public async Task<int> AddShoppingItemAsync(int productId, int quantity)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        
        // Check if item already exists and is not purchased
        var existingItem = await context.ShoppingItems
            .FirstOrDefaultAsync(si => si.ProductId == productId && !si.IsPurchased);
            
        if (existingItem != null)
        {
            existingItem.Quantity += quantity;
            await context.SaveChangesAsync();
            return existingItem.Id;
        }
        
        var shoppingItem = new ShoppingItem
        {
            ProductId = productId,
            Quantity = quantity,
            IsPurchased = false,
            CreatedAt = DateTime.UtcNow
        };
        
        context.ShoppingItems.Add(shoppingItem);
        await context.SaveChangesAsync();
        
        return shoppingItem.Id;
    }

    public async Task UpdateQuantityAsync(int shoppingItemId, int quantity)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var item = await context.ShoppingItems.FindAsync(shoppingItemId);
        
        if (item != null)
        {
            if (quantity <= 0)
            {
                context.ShoppingItems.Remove(item);
            }
            else
            {
                item.Quantity = quantity;
            }
            await context.SaveChangesAsync();
        }
    }

    public async Task MarkAsPurchasedAsync(int shoppingItemId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var item = await context.ShoppingItems.FindAsync(shoppingItemId);
        
        if (item != null)
        {
            item.IsPurchased = true;
            item.PurchasedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
        }
    }

    public async Task ClearAllPurchasedAsync()
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var purchasedItems = await context.ShoppingItems
            .Where(si => si.IsPurchased)
            .ToListAsync();
            
        context.ShoppingItems.RemoveRange(purchasedItems);
        await context.SaveChangesAsync();
    }

    public async Task<List<StoreAvailabilityViewModel>> GetStoreAvailabilityAsync()
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        
        var activeItems = await context.ShoppingItems
            .Include(si => si.Product)
            .ThenInclude(p => p.ProductStores)
            .Where(si => !si.IsPurchased)
            .ToListAsync();
            
        if (!activeItems.Any())
        {
            return new List<StoreAvailabilityViewModel>();
        }
        
        var stores = await context.Stores.ToListAsync();
        
        return stores.Select(store => new StoreAvailabilityViewModel
        {
            StoreId = store.Id,
            StoreName = store.Name,
            TotalItemsCount = activeItems.Count,
            AvailableItemsCount = activeItems.Count(item => 
                item.Product.ProductStores.Any(ps => ps.StoreId == store.Id))
        }).OrderByDescending(s => s.CoveragePercentage)
        .ToList();
    }

    public async Task RemoveShoppingItemAsync(int shoppingItemId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var item = await context.ShoppingItems.FindAsync(shoppingItemId);
        
        if (item != null)
        {
            context.ShoppingItems.Remove(item);
            await context.SaveChangesAsync();
        }
    }
}
