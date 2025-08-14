namespace FamilyShoppingApp.Models;

public class RecipeProduct
{
    public int RecipeId { get; set; }
    public int ProductId { get; set; }
    
    // Navigation properties
    public virtual Recipe Recipe { get; set; } = null!;
    public virtual Product Product { get; set; } = null!;
}
