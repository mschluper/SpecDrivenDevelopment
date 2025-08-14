namespace FamilyShoppingApp.Models;

public class ProductStore
{
    public int ProductId { get; set; }
    public int StoreId { get; set; }
    
    // Navigation properties
    public virtual Product Product { get; set; } = null!;
    public virtual Store Store { get; set; } = null!;
}
