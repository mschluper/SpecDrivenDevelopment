namespace FamilyShoppingApp.ViewModels;

public class RecipeViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Servings { get; set; }
    public HashSet<int> SelectedProductIds { get; set; } = new();
}
