namespace FamilyShoppingApp.Models;

public class ShoppingItem
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public bool IsPurchased { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? PurchasedAt { get; set; }

    // Navigation properties
    public Product Product { get; set; } = null!;
}
