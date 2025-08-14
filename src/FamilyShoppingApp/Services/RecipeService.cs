using Microsoft.EntityFrameworkCore;
using FamilyShoppingApp.Data;
using FamilyShoppingApp.Models;
using FamilyShoppingApp.ViewModels;

namespace FamilyShoppingApp.Services;

public class RecipeService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

    public RecipeService(IDbContextFactory<ApplicationDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<List<RecipeViewModel>> GetAllRecipesAsync()
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var recipes = await context.Recipes
            .Include(r => r.RecipeProducts)
            .OrderBy(r => r.Name)
            .ToListAsync();

        return recipes.Select(r => new RecipeViewModel
        {
            Id = r.Id,
            Name = r.Name,
            Servings = r.Servings,
            SelectedProductIds = r.RecipeProducts.Select(rp => rp.ProductId).ToHashSet()
        }).ToList();
    }

    public async Task<RecipeViewModel?> GetRecipeByIdAsync(int id)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var recipe = await context.Recipes
            .Include(r => r.RecipeProducts)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (recipe == null)
            return null;

        return new RecipeViewModel
        {
            Id = recipe.Id,
            Name = recipe.Name,
            Servings = recipe.Servings,
            SelectedProductIds = recipe.RecipeProducts.Select(rp => rp.ProductId).ToHashSet()
        };
    }

    public async Task<int> CreateRecipeAsync(RecipeViewModel recipeViewModel)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        
        var recipe = new Recipe
        {
            Name = recipeViewModel.Name,
            Servings = recipeViewModel.Servings
        };
        
        context.Recipes.Add(recipe);
        await context.SaveChangesAsync();

        // Add recipe-product relationships
        foreach (var productId in recipeViewModel.SelectedProductIds)
        {
            context.RecipeProducts.Add(new RecipeProduct
            {
                RecipeId = recipe.Id,
                ProductId = productId
            });
        }

        await context.SaveChangesAsync();
        
        return recipe.Id;
    }

    public async Task UpdateRecipeAsync(RecipeViewModel recipeViewModel)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var recipe = await context.Recipes
            .Include(r => r.RecipeProducts)
            .FirstOrDefaultAsync(r => r.Id == recipeViewModel.Id);
        
        if (recipe != null)
        {
            recipe.Name = recipeViewModel.Name;
            recipe.Servings = recipeViewModel.Servings;
            
            // Remove existing recipe-product relationships
            context.RecipeProducts.RemoveRange(recipe.RecipeProducts);
            
            // Add new recipe-product relationships
            foreach (var productId in recipeViewModel.SelectedProductIds)
            {
                context.RecipeProducts.Add(new RecipeProduct
                {
                    RecipeId = recipe.Id,
                    ProductId = productId
                });
            }
            
            await context.SaveChangesAsync();
        }
    }

    public async Task DeleteRecipeAsync(int id)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var recipe = await context.Recipes
            .Include(r => r.RecipeProducts)
            .FirstOrDefaultAsync(r => r.Id == id);
        
        if (recipe != null)
        {
            context.RecipeProducts.RemoveRange(recipe.RecipeProducts);
            context.Recipes.Remove(recipe);
            await context.SaveChangesAsync();
        }
    }

    public async Task<List<RecipeViewModel>> GetRecipesByProductIdAsync(int productId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var recipes = await context.Recipes
            .Include(r => r.RecipeProducts)
            .Where(r => r.RecipeProducts.Any(rp => rp.ProductId == productId))
            .OrderBy(r => r.Name)
            .ToListAsync();

        return recipes.Select(r => new RecipeViewModel
        {
            Id = r.Id,
            Name = r.Name,
            Servings = r.Servings,
            SelectedProductIds = r.RecipeProducts.Select(rp => rp.ProductId).ToHashSet()
        }).ToList();
    }
}
