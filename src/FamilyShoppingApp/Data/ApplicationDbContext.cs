using Microsoft.EntityFrameworkCore;
using FamilyShoppingApp.Models;

namespace FamilyShoppingApp.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }
    
    public DbSet<Store> Stores { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<Recipe> Recipes { get; set; }
    public DbSet<ProductStore> ProductStores { get; set; }
    public DbSet<RecipeProduct> RecipeProducts { get; set; }
    public DbSet<ShoppingItem> ShoppingItems { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Configure ProductStore many-to-many relationship
        modelBuilder.Entity<ProductStore>()
            .HasKey(ps => new { ps.ProductId, ps.StoreId });
            
        modelBuilder.Entity<ProductStore>()
            .HasOne(ps => ps.Product)
            .WithMany(p => p.ProductStores)
            .HasForeignKey(ps => ps.ProductId);
            
        modelBuilder.Entity<ProductStore>()
            .HasOne(ps => ps.Store)
            .WithMany(s => s.ProductStores)
            .HasForeignKey(ps => ps.StoreId);
            
        // Configure RecipeProduct many-to-many relationship
        modelBuilder.Entity<RecipeProduct>()
            .HasKey(rp => new { rp.RecipeId, rp.ProductId });
            
        modelBuilder.Entity<RecipeProduct>()
            .HasOne(rp => rp.Recipe)
            .WithMany(r => r.RecipeProducts)
            .HasForeignKey(rp => rp.RecipeId);
            
        modelBuilder.Entity<RecipeProduct>()
            .HasOne(rp => rp.Product)
            .WithMany(p => p.RecipeProducts)
            .HasForeignKey(rp => rp.ProductId);
            
        // Configure ShoppingItem relationship
        modelBuilder.Entity<ShoppingItem>()
            .HasOne(si => si.Product)
            .WithMany()
            .HasForeignKey(si => si.ProductId);
    }
}
