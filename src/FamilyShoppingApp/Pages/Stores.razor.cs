using Microsoft.AspNetCore.Components;
using FamilyShoppingApp.Services;
using FamilyShoppingApp.ViewModels;

namespace FamilyShoppingApp.Pages;

public partial class Stores : ComponentBase
{
    private List<StoreViewModel> stores = new();
    private StoreViewModel currentStore = new();
    private bool showCreateEditForm = false;
    private int? editingStoreId = null;
    
    private bool IsNameValid => !string.IsNullOrWhiteSpace(currentStore.Name);
    private bool IsFormValid => IsNameValid;
    private bool hasNameBeenTouched = false;
    
    protected override async Task OnInitializedAsync()
    {
        await LoadStoresAsync();
    }
    
    private async Task LoadStoresAsync()
    {
        try
        {
            stores = await StoreService.GetAllStoresAsync();
        }
        catch (Exception ex)
        {
            await ExceptionHandler.HandleExceptionAsync<Stores>(ex, nameof(LoadStoresAsync));
        }
    }
    
    private void ShowCreateForm()
    {
        currentStore = new StoreViewModel();
        editingStoreId = null;
        hasNameBeenTouched = false;
        showCreateEditForm = true;
    }
    
    private async Task EditStore(int storeId)
    {
        try
        {
            var store = await StoreService.GetStoreByIdAsync(storeId);
            if (store != null)
            {
                currentStore = store;
                editingStoreId = storeId;
                showCreateEditForm = true;
            }
        }
        catch (Exception ex)
        {
            await ExceptionHandler.HandleExceptionAsync<Stores>(ex, nameof(EditStore));
        }
    }
    
    private async Task HandleSubmit()
    {
        if (!IsFormValid) return;
        
        try
        {
            if (editingStoreId.HasValue)
            {
                await StoreService.UpdateStoreAsync(currentStore);
            }
            else
            {
                await StoreService.CreateStoreAsync(currentStore);
            }
            
            await LoadStoresAsync();
            CancelEdit();
        }
        catch (Exception ex)
        {
            await ExceptionHandler.HandleExceptionAsync<Stores>(ex, nameof(HandleSubmit));
        }
    }
    
    private async Task DeleteStore(int storeId)
    {
        try
        {
            await StoreService.DeleteStoreAsync(storeId);
            await LoadStoresAsync();
        }
        catch (Exception ex)
        {
            await ExceptionHandler.HandleExceptionAsync<Stores>(ex, nameof(DeleteStore));
        }
    }
    
    private void CancelEdit()
    {
        showCreateEditForm = false;
        currentStore = new StoreViewModel();
        editingStoreId = null;
        hasNameBeenTouched = false;
    }
    
    private void OnNameChanged(ChangeEventArgs e)
    {
        currentStore.Name = e.Value?.ToString() ?? string.Empty;
        hasNameBeenTouched = true;
        StateHasChanged();
    }
    
    private void OnNotesChanged(ChangeEventArgs e)
    {
        currentStore.Notes = e.Value?.ToString();
        StateHasChanged();
    }
}
