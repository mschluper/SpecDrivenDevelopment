namespace FamilyShoppingApp.ViewModels;

public class ShoppingItemViewModel
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductNotes { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public bool IsPurchased { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? PurchasedAt { get; set; }
    public HashSet<int> AvailableStoreIds { get; set; } = new();
}

public class StoreAvailabilityViewModel
{
    public int StoreId { get; set; }
    public string StoreName { get; set; } = string.Empty;
    public int AvailableItemsCount { get; set; }
    public int TotalItemsCount { get; set; }
    public decimal CoveragePercentage => TotalItemsCount > 0 
        ? Math.Round((decimal)AvailableItemsCount / TotalItemsCount * 100, 1) 
        : 0;
}

public class DashboardViewModel
{
    public List<ProductViewModel> AvailableProducts { get; set; } = new();
    public List<ShoppingItemViewModel> ShoppingItems { get; set; } = new();
    public List<StoreAvailabilityViewModel> StoreAvailability { get; set; } = new();
    public int SelectedProductId { get; set; }
    public int SelectedQuantity { get; set; } = 1;
    public bool HasPurchasedItems { get; set; }
}
