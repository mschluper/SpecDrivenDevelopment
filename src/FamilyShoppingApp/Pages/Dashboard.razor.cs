using Microsoft.AspNetCore.Components;
using FamilyShoppingApp.ViewModels;

namespace FamilyShoppingApp.Pages;

public partial class Dashboard
{
    private DashboardViewModel viewModel = new();
    private bool showAddForm = false;
    private bool hasProductBeenTouched = false;
    private bool hasQuantityBeenTouched = false;
    
    private bool IsProductValid => viewModel.SelectedProductId > 0;
    private bool IsQuantityValid => viewModel.SelectedQuantity >= 1;
    private bool IsFormValid => IsProductValid && IsQuantityValid;

    protected override async Task OnInitializedAsync()
    {
        await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        try
        {
            await LoadAvailableProductsAsync();
            await LoadShoppingItemsAsync();
            await LoadStoreAvailabilityAsync();
            await LoadPurchasedItemsStatusAsync();
        }
        catch (Exception ex)
        {
            await ExceptionHandler.HandleExceptionAsync<Dashboard>(ex, nameof(LoadDataAsync));
        }
    }
    
    private async Task LoadAvailableProductsAsync()
    {
        try
        {
            viewModel.AvailableProducts = await ProductService.GetAllProductsAsync();
        }
        catch (Exception ex)
        {
            await ExceptionHandler.HandleExceptionAsync<Dashboard>(ex, nameof(LoadAvailableProductsAsync));
        }
    }
    
    private async Task LoadShoppingItemsAsync()
    {
        try
        {
            viewModel.ShoppingItems = await ShoppingService.GetActiveShoppingItemsAsync();
        }
        catch (Exception ex)
        {
            await ExceptionHandler.HandleExceptionAsync<Dashboard>(ex, nameof(LoadShoppingItemsAsync));
        }
    }
    
    private async Task LoadStoreAvailabilityAsync()
    {
        try
        {
            viewModel.StoreAvailability = await ShoppingService.GetStoreAvailabilityAsync();
        }
        catch (Exception ex)
        {
            await ExceptionHandler.HandleExceptionAsync<Dashboard>(ex, nameof(LoadStoreAvailabilityAsync));
        }
    }
    
    private async Task LoadPurchasedItemsStatusAsync()
    {
        try
        {
            viewModel.HasPurchasedItems = await ShoppingService.HasPurchasedItemsAsync();
        }
        catch (Exception ex)
        {
            await ExceptionHandler.HandleExceptionAsync<Dashboard>(ex, nameof(LoadPurchasedItemsStatusAsync));
        }
    }
    
    private void ShowAddForm()
    {
        showAddForm = true;
        viewModel.SelectedProductId = 0;
        viewModel.SelectedQuantity = 1;
        hasProductBeenTouched = false;
        hasQuantityBeenTouched = false;
    }
    
    private void CancelAdd()
    {
        showAddForm = false;
        viewModel.SelectedProductId = 0;
        viewModel.SelectedQuantity = 1;
        hasProductBeenTouched = false;
        hasQuantityBeenTouched = false;
    }
    
    private void OnProductChanged(ChangeEventArgs e)
    {
        hasProductBeenTouched = true;
        if (int.TryParse(e.Value?.ToString(), out var productId))
        {
            viewModel.SelectedProductId = productId;
        }
    }
    
    private void OnQuantityChanged(ChangeEventArgs e)
    {
        hasQuantityBeenTouched = true;
        if (int.TryParse(e.Value?.ToString(), out var quantity))
        {
            viewModel.SelectedQuantity = quantity;
        }
    }
    
    private async Task HandleAddProduct()
    {
        if (!IsFormValid) return;
        
        try
        {
            await ShoppingService.AddShoppingItemAsync(viewModel.SelectedProductId, viewModel.SelectedQuantity);
            await LoadShoppingItemsAsync();
            await LoadStoreAvailabilityAsync();
            await LoadPurchasedItemsStatusAsync();
            CancelAdd();
        }
        catch (Exception ex)
        {
            await ExceptionHandler.HandleExceptionAsync<Dashboard>(ex, nameof(HandleAddProduct));
        }
    }
    
    private async Task IncrementQuantity(int shoppingItemId)
    {
        try
        {
            var item = viewModel.ShoppingItems.FirstOrDefault(si => si.Id == shoppingItemId);
            if (item != null)
            {
                await ShoppingService.UpdateQuantityAsync(shoppingItemId, item.Quantity + 1);
                await LoadShoppingItemsAsync();
                await LoadStoreAvailabilityAsync();
                await LoadPurchasedItemsStatusAsync();
            }
        }
        catch (Exception ex)
        {
            await ExceptionHandler.HandleExceptionAsync<Dashboard>(ex, nameof(IncrementQuantity));
        }
    }
    
    private async Task DecrementQuantity(int shoppingItemId)
    {
        try
        {
            var item = viewModel.ShoppingItems.FirstOrDefault(si => si.Id == shoppingItemId);
            if (item != null)
            {
                await ShoppingService.UpdateQuantityAsync(shoppingItemId, item.Quantity - 1);
                await LoadShoppingItemsAsync();
                await LoadStoreAvailabilityAsync();
                await LoadPurchasedItemsStatusAsync();
            }
        }
        catch (Exception ex)
        {
            await ExceptionHandler.HandleExceptionAsync<Dashboard>(ex, nameof(DecrementQuantity));
        }
    }
    
    private async Task MarkAsPurchased(int shoppingItemId)
    {
        try
        {
            await ShoppingService.MarkAsPurchasedAsync(shoppingItemId);
            await LoadShoppingItemsAsync();
            await LoadStoreAvailabilityAsync();
            await LoadPurchasedItemsStatusAsync();
        }
        catch (Exception ex)
        {
            await ExceptionHandler.HandleExceptionAsync<Dashboard>(ex, nameof(MarkAsPurchased));
        }
    }
    
    private async Task RemoveItem(int shoppingItemId)
    {
        try
        {
            await ShoppingService.RemoveShoppingItemAsync(shoppingItemId);
            await LoadShoppingItemsAsync();
            await LoadStoreAvailabilityAsync();
            await LoadPurchasedItemsStatusAsync();
        }
        catch (Exception ex)
        {
            await ExceptionHandler.HandleExceptionAsync<Dashboard>(ex, nameof(RemoveItem));
        }
    }
    
    private async Task ClearPurchased()
    {
        try
        {
            await ShoppingService.ClearAllPurchasedAsync();
            await LoadShoppingItemsAsync();
            await LoadStoreAvailabilityAsync();
            await LoadPurchasedItemsStatusAsync();
        }
        catch (Exception ex)
        {
            await ExceptionHandler.HandleExceptionAsync<Dashboard>(ex, nameof(ClearPurchased));
        }
    }
}
