using Microsoft.AspNetCore.Components;
using FamilyShoppingApp.ViewModels;

namespace FamilyShoppingApp.Pages;

public partial class Recipes
{
    private List<RecipeViewModel> recipes = new();
    private List<RecipeViewModel> filteredRecipes = new();
    private List<ProductViewModel> availableProducts = new();
    private RecipeViewModel currentRecipe = new();
    private bool showCreateEditForm = false;
    private int? editingRecipeId = null;
    private bool hasNameBeenTouched = false;
    private bool hasServingsBeenTouched = false;
    private int selectedProductFilterId = 0;
    
    private bool IsNameValid => !string.IsNullOrWhiteSpace(currentRecipe.Name);
    private bool IsServingsValid => currentRecipe.Servings >= 1;
    private bool IsFormValid => IsNameValid && IsServingsValid;

    protected override async Task OnInitializedAsync()
    {
        await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        try
        {
            await LoadRecipesAsync();
            await LoadProductsAsync();
            ApplyFilter();
        }
        catch (Exception ex)
        {
            await ExceptionHandler.HandleExceptionAsync<Recipes>(ex, nameof(LoadDataAsync));
        }
    }
    
    private async Task LoadRecipesAsync()
    {
        try
        {
            recipes = await RecipeService.GetAllRecipesAsync();
        }
        catch (Exception ex)
        {
            await ExceptionHandler.HandleExceptionAsync<Recipes>(ex, nameof(LoadRecipesAsync));
        }
    }
    
    private async Task LoadProductsAsync()
    {
        try
        {
            availableProducts = await ProductService.GetAllProductsAsync();
        }
        catch (Exception ex)
        {
            await ExceptionHandler.HandleExceptionAsync<Recipes>(ex, nameof(LoadProductsAsync));
        }
    }
    
    private void ApplyFilter()
    {
        if (selectedProductFilterId == 0)
        {
            filteredRecipes = recipes.ToList();
        }
        else
        {
            filteredRecipes = recipes.Where(r => 
                r.SelectedProductIds.Contains(selectedProductFilterId)
            ).ToList();
        }
    }
    
    private void ShowCreateForm()
    {
        currentRecipe = new RecipeViewModel { Servings = 1 };
        editingRecipeId = null;
        hasNameBeenTouched = false;
        hasServingsBeenTouched = false;
        showCreateEditForm = true;
    }
    
    private async Task EditRecipe(int recipeId)
    {
        try
        {
            var recipe = await RecipeService.GetRecipeByIdAsync(recipeId);
            if (recipe != null)
            {
                currentRecipe = recipe;
                editingRecipeId = recipeId;
                hasNameBeenTouched = false;
                hasServingsBeenTouched = false;
                showCreateEditForm = true;
            }
        }
        catch (Exception ex)
        {
            await ExceptionHandler.HandleExceptionAsync<Recipes>(ex, nameof(EditRecipe));
        }
    }
    
    private void CancelEdit()
    {
        showCreateEditForm = false;
        currentRecipe = new RecipeViewModel();
        editingRecipeId = null;
        hasNameBeenTouched = false;
        hasServingsBeenTouched = false;
    }
    
    private void OnNameChanged(ChangeEventArgs e)
    {
        hasNameBeenTouched = true;
        currentRecipe.Name = e.Value?.ToString() ?? string.Empty;
    }
    
    private void OnServingsChanged(ChangeEventArgs e)
    {
        hasServingsBeenTouched = true;
        if (int.TryParse(e.Value?.ToString(), out var servings))
        {
            currentRecipe.Servings = servings;
        }
    }
    
    private void OnProductSelectionChanged(int productId, ChangeEventArgs e)
    {
        var isChecked = e.Value?.ToString()?.ToLower() == "true";
        
        if (isChecked)
        {
            currentRecipe.SelectedProductIds.Add(productId);
        }
        else
        {
            currentRecipe.SelectedProductIds.Remove(productId);
        }
    }
    
    private async Task HandleSubmit()
    {
        if (!IsFormValid) return;
        
        try
        {
            if (editingRecipeId.HasValue)
            {
                await RecipeService.UpdateRecipeAsync(currentRecipe);
            }
            else
            {
                await RecipeService.CreateRecipeAsync(currentRecipe);
            }
            
            await LoadRecipesAsync();
            ApplyFilter();
            CancelEdit();
        }
        catch (Exception ex)
        {
            await ExceptionHandler.HandleExceptionAsync<Recipes>(ex, nameof(HandleSubmit));
        }
    }
    
    private async Task DeleteRecipe(int recipeId)
    {
        try
        {
            await RecipeService.DeleteRecipeAsync(recipeId);
            await LoadRecipesAsync();
            ApplyFilter();
            
            if (editingRecipeId == recipeId)
            {
                CancelEdit();
            }
        }
        catch (Exception ex)
        {
            await ExceptionHandler.HandleExceptionAsync<Recipes>(ex, nameof(DeleteRecipe));
        }
    }
    
    private async Task OnProductFilterChanged(ChangeEventArgs e)
    {
        if (int.TryParse(e.Value?.ToString(), out var productId))
        {
            selectedProductFilterId = productId;
            
            if (productId > 0)
            {
                try
                {
                    filteredRecipes = await RecipeService.GetRecipesByProductIdAsync(productId);
                }
                catch (Exception ex)
                {
                    await ExceptionHandler.HandleExceptionAsync<Recipes>(ex, nameof(OnProductFilterChanged));
                }
            }
            else
            {
                ApplyFilter();
            }
        }
    }
    
    private void ClearProductFilter()
    {
        selectedProductFilterId = 0;
        ApplyFilter();
    }
}
