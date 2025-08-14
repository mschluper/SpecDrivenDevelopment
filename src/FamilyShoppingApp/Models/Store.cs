namespace FamilyShoppingApp.Models;

public class Store
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Notes { get; set; }
    
    // Navigation properties
    public virtual ICollection<ProductStore> ProductStores { get; set; } = new List<ProductStore>();
}
