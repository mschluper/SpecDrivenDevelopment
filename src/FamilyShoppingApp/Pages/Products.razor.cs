using Microsoft.AspNetCore.Components;
using FamilyShoppingApp.Services;
using FamilyShoppingApp.ViewModels;

namespace FamilyShoppingApp.Pages;

public partial class Products : ComponentBase
{
    private List<ProductViewModel> products = new();
    private List<ProductViewModel> filteredProducts = new();
    private List<StoreViewModel> availableStores = new();
    private ProductViewModel currentProduct = new();
    private bool showCreateEditForm = false;
    private int? editingProductId = null;
    private string searchTerm = string.Empty;
    private bool hasNameBeenTouched = false;
    private int totalProductCount = 0;
    
    private bool IsNameValid => !string.IsNullOrWhiteSpace(currentProduct.Name);
    private bool IsFormValid => IsNameValid;
    
    protected override async Task OnInitializedAsync()
    {
        await LoadDataAsync();
    }
    
    private async Task LoadDataAsync()
    {
        try
        {
            await LoadProductsAsync();
            await LoadStoresAsync();
            ApplyFilter();
        }
        catch (Exception ex)
        {
            await ExceptionHandler.HandleExceptionAsync<Products>(ex, nameof(LoadDataAsync));
        }
    }
    
    private async Task LoadProductsAsync()
    {
        try
        {
            products = await ProductService.GetAllProductsAsync();
            totalProductCount = products.Count;
        }
        catch (Exception ex)
        {
            await ExceptionHandler.HandleExceptionAsync<Products>(ex, nameof(LoadProductsAsync));
        }
    }
    
    private async Task LoadStoresAsync()
    {
        try
        {
            availableStores = await StoreService.GetAllStoresAsync();
        }
        catch (Exception ex)
        {
            await ExceptionHandler.HandleExceptionAsync<Products>(ex, nameof(LoadStoresAsync));
        }
    }
    
    private void ApplyFilter()
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            filteredProducts = products.ToList();
        }
        else
        {
            filteredProducts = products.Where(p => 
                p.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                (p.Notes != null && p.Notes.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
            ).ToList();
        }
    }
    
    private void ShowCreateForm()
    {
        currentProduct = new ProductViewModel();
        editingProductId = null;
        hasNameBeenTouched = false;
        showCreateEditForm = true;
    }
    
    private async Task EditProduct(int productId)
    {
        try
        {
            var product = await ProductService.GetProductByIdAsync(productId);
            if (product != null)
            {
                currentProduct = product;
                editingProductId = productId;
                hasNameBeenTouched = false;
                showCreateEditForm = true;
            }
        }
        catch (Exception ex)
        {
            await ExceptionHandler.HandleExceptionAsync<Products>(ex, nameof(EditProduct));
        }
    }
    
    private async Task HandleSubmit()
    {
        if (!IsFormValid) return;
        
        try
        {
            if (editingProductId.HasValue)
            {
                await ProductService.UpdateProductAsync(currentProduct);
            }
            else
            {
                await ProductService.CreateProductAsync(currentProduct);
            }
            
            await LoadProductsAsync();
            ApplyFilter();
            CancelEdit();
        }
        catch (Exception ex)
        {
            await ExceptionHandler.HandleExceptionAsync<Products>(ex, nameof(HandleSubmit));
        }
    }
    
    private async Task DeleteProduct(int productId)
    {
        try
        {
            await ProductService.DeleteProductAsync(productId);
            await LoadProductsAsync();
            ApplyFilter();
        }
        catch (Exception ex)
        {
            await ExceptionHandler.HandleExceptionAsync<Products>(ex, nameof(DeleteProduct));
        }
    }
    
    private async Task SearchProducts()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                await LoadProductsAsync();
                ApplyFilter();
            }
            else
            {
                // Get total count if we don't have it yet
                if (totalProductCount == 0)
                {
                    var allProducts = await ProductService.GetAllProductsAsync();
                    totalProductCount = allProducts.Count;
                }
                
                products = await ProductService.SearchProductsAsync(searchTerm);
                filteredProducts = products.ToList(); // Use search results directly
            }
        }
        catch (Exception ex)
        {
            await ExceptionHandler.HandleExceptionAsync<Products>(ex, nameof(SearchProducts));
        }
    }
    
    private void CancelEdit()
    {
        showCreateEditForm = false;
        currentProduct = new ProductViewModel();
        editingProductId = null;
        hasNameBeenTouched = false;
    }
    
    private void ClearSearch()
    {
        searchTerm = string.Empty;
        ApplyFilter();
    }
    
    private void OnNameChanged(ChangeEventArgs e)
    {
        currentProduct.Name = e.Value?.ToString() ?? string.Empty;
        hasNameBeenTouched = true;
        StateHasChanged();
    }
    
    private void OnNotesChanged(ChangeEventArgs e)
    {
        currentProduct.Notes = e.Value?.ToString();
        StateHasChanged();
    }
    
    private void OnSearchChanged(ChangeEventArgs e)
    {
        searchTerm = e.Value?.ToString() ?? string.Empty;
        StateHasChanged();
    }
    
    private void OnStoreSelectionChanged(int storeId, bool isSelected)
    {
        if (isSelected)
        {
            currentProduct.SelectedStoreIds.Add(storeId);
        }
        else
        {
            currentProduct.SelectedStoreIds.Remove(storeId);
        }
        StateHasChanged();
    }
}
