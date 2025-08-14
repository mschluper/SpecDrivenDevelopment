namespace FamilyShoppingApp.Models;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Notes { get; set; }
    
    // Navigation properties
    public virtual ICollection<ProductStore> ProductStores { get; set; } = new List<ProductStore>();
    public virtual ICollection<RecipeProduct> RecipeProducts { get; set; } = new List<RecipeProduct>();
}
