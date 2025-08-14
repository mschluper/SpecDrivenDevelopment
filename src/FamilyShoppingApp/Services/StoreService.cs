using Microsoft.EntityFrameworkCore;
using FamilyShoppingApp.Data;
using FamilyShoppingApp.Models;
using FamilyShoppingApp.ViewModels;

namespace FamilyShoppingApp.Services;

public class StoreService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    
    public StoreService(IDbContextFactory<ApplicationDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }
    
    public async Task<List<StoreViewModel>> GetAllStoresAsync()
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var stores = await context.Stores
            .OrderBy(s => s.Name)
            .ToListAsync();
            
        return stores.Select(s => new StoreViewModel
        {
            Id = s.Id,
            Name = s.Name,
            Notes = s.Notes
        }).ToList();
    }
    
    public async Task<StoreViewModel?> GetStoreByIdAsync(int id)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var store = await context.Stores.FindAsync(id);
        
        if (store == null) return null;
        
        return new StoreViewModel
        {
            Id = store.Id,
            Name = store.Name,
            Notes = store.Notes
        };
    }
    
    public async Task<int> CreateStoreAsync(StoreViewModel storeViewModel)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var store = new Store
        {
            Name = storeViewModel.Name,
            Notes = storeViewModel.Notes
        };
        
        context.Stores.Add(store);
        await context.SaveChangesAsync();
        
        return store.Id;
    }
    
    public async Task UpdateStoreAsync(StoreViewModel storeViewModel)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var store = await context.Stores.FindAsync(storeViewModel.Id);
        
        if (store != null)
        {
            store.Name = storeViewModel.Name;
            store.Notes = storeViewModel.Notes;
            
            await context.SaveChangesAsync();
        }
    }
    
    public async Task DeleteStoreAsync(int id)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var store = await context.Stores.FindAsync(id);
        
        if (store != null)
        {
            context.Stores.Remove(store);
            await context.SaveChangesAsync();
        }
    }
}
