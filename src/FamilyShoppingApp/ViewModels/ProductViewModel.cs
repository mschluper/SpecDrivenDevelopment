namespace FamilyShoppingApp.ViewModels;

public class ProductViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public HashSet<int> SelectedStoreIds { get; set; } = new();
}
