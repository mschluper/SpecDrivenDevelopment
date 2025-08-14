namespace FamilyShoppingApp.Models;

public class Recipe
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Servings { get; set; }
    
    // Navigation properties
    public virtual ICollection<RecipeProduct> RecipeProducts { get; set; } = new List<RecipeProduct>();
}
