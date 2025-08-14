using Xunit;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using FamilyShoppingApp.Data;
using FamilyShoppingApp.Services;
using FamilyShoppingApp.Pages;
using FamilyShoppingApp.ViewModels;
using FamilyShoppingApp.Models;
using AngleSharp.Dom;
using Moq;
using FamilyShoppingApp.Tests.Helpers;

namespace FamilyShoppingApp.Tests.Pages;

public class StoresPageTests : TestContext
{
    private readonly ApplicationDbContext _context;
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

    public StoresPageTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _contextFactory = new TestDbContextFactory(options);
        
        Services.AddScoped<StoreService>(_ => new StoreService(_contextFactory));
        Services.AddScoped<ExceptionHandlerService>();
    }

    [Fact]
    public void StoresPage_RendersCorrectly_WhenNoStores()
    {
        // Act
        var component = RenderComponent<Stores>();

        // Assert
        Assert.Contains("Stores", component.Find("h1").InnerHtml);
        Assert.NotNull(component.Find("[data-testid='create-store-button']"));
        Assert.NotNull(component.Find("[data-testid='no-stores-message']"));
        Assert.Contains("No stores found. Click \"Add New Store\" to get started.", 
            component.Find("[data-testid='no-stores-message']").TextContent.Trim());
    }

    [Fact]
    public void CreateStoreButton_ShowsForm_WhenClicked()
    {
        // Arrange
        var component = RenderComponent<Stores>();

        // Act
        component.Find("[data-testid='create-store-button']").Click();

        // Assert
        Assert.NotNull(component.Find("[data-testid='store-form']"));
        Assert.NotNull(component.Find("[data-testid='store-name-input']"));
        Assert.NotNull(component.Find("[data-testid='store-notes-input']"));
        Assert.NotNull(component.Find("[data-testid='save-store-button']"));
        Assert.NotNull(component.Find("[data-testid='cancel-button']"));
    }

    [Fact]
    public void StoreForm_DisablesSubmit_WhenNameEmpty()
    {
        // Arrange
        var component = RenderComponent<Stores>();
        component.Find("[data-testid='create-store-button']").Click();

        // Assert
        var submitButton = component.Find("[data-testid='save-store-button']");
        Assert.True(submitButton.HasAttribute("disabled"));
    }

    [Fact]
    public void StoreForm_EnablesSubmit_WhenNameProvided()
    {
        // Arrange
        var component = RenderComponent<Stores>();
        component.Find("[data-testid='create-store-button']").Click();

        // Act
        var nameInput = component.Find("[data-testid='store-name-input']");
        nameInput.Change("Test Store");

        // Assert
        component.WaitForAssertion(() =>
        {
            var submitButton = component.Find("[data-testid='save-store-button']");
            Assert.False(submitButton.HasAttribute("disabled"));
        });
    }

    [Fact]
    public void StoreForm_ShowsValidationError_WhenNameEmptyAfterInput()
    {
        // Arrange
        var component = RenderComponent<Stores>();
        component.Find("[data-testid='create-store-button']").Click();

        // Act
        var nameInput = component.Find("[data-testid='store-name-input']");
        nameInput.Change("Test Store");
        nameInput.Change("");

        // Assert
        component.WaitForAssertion(() =>
        {
            var validationError = component.Find("[data-testid='name-validation-error']");
            Assert.Equal("Store name is required.", validationError.TextContent.Trim());
        });
    }

    [Fact]
    public async Task StoreForm_CreatesStore_WhenValidDataSubmitted()
    {
        // Arrange
        var component = RenderComponent<Stores>();
        component.Find("[data-testid='create-store-button']").Click();

        // Act
        var nameInput = component.Find("[data-testid='store-name-input']");
        var notesInput = component.Find("[data-testid='store-notes-input']");
        
        nameInput.Change("Test Store");
        notesInput.Change("Test Notes");
        
        component.Find("[data-testid='save-store-button']").Click();

        // Assert
        component.WaitForAssertion(() =>
        {
            var stores = _context.Stores.ToList();
            Assert.Single(stores);
            Assert.Equal("Test Store", stores[0].Name);
            Assert.Equal("Test Notes", stores[0].Notes);
        });
        
        // Form should be hidden after successful creation
        component.WaitForAssertion(() =>
        {
            Assert.Throws<ElementNotFoundException>(() => component.Find("[data-testid='store-form']"));
        });
    }

    [Fact]
    public async Task StoresPage_DisplaysStores_WhenStoresExist()
    {
        // Arrange
        var store1 = new Store { Name = "Store 1", Notes = "Notes 1" };
        var store2 = new Store { Name = "Store 2", Notes = "Notes 2" };
        
        _context.Stores.AddRange(store1, store2);
        await _context.SaveChangesAsync();

        // Act
        var component = RenderComponent<Stores>();

        // Assert
        component.WaitForAssertion(() =>
        {
            var table = component.Find("[data-testid='stores-table']");
            Assert.NotNull(table);
            
            var rows = component.FindAll("[data-testid='store-row']");
            Assert.Equal(2, rows.Count);
            
            var storeNames = component.FindAll("[data-testid='store-name']");
            Assert.Contains("Store 1", storeNames.Select(n => n.TextContent));
            Assert.Contains("Store 2", storeNames.Select(n => n.TextContent));
        });
    }

    [Fact]
    public async Task EditButton_LoadsStoreData_WhenClicked()
    {
        // Arrange
        var store = new Store { Name = "Test Store", Notes = "Test Notes" };
        _context.Stores.Add(store);
        await _context.SaveChangesAsync();

        var component = RenderComponent<Stores>();

        // Act
        component.WaitForAssertion(() =>
        {
            var editButton = component.Find("[data-testid='edit-store-button']");
            editButton.Click();
        });

        // Assert
        component.WaitForAssertion(() =>
        {
            var nameInput = component.Find("[data-testid='store-name-input']");
            var notesInput = component.Find("[data-testid='store-notes-input']");
            
            Assert.Equal("Test Store", nameInput.GetAttribute("value"));
            Assert.Equal("Test Notes", notesInput.GetAttribute("value") ?? notesInput.TextContent);
        });
    }

    [Fact]
    public async Task DeleteButton_RemovesStore_WhenClicked()
    {
        // Arrange
        var store = new Store { Name = "Store to Delete", Notes = "Will be deleted" };
        _context.Stores.Add(store);
        await _context.SaveChangesAsync();

        var component = RenderComponent<Stores>();

        // Act
        component.WaitForAssertion(() =>
        {
            var deleteButton = component.Find("[data-testid='delete-store-button']");
            deleteButton.Click();
        });

        // Assert
        component.WaitForAssertion(() =>
        {
            var stores = _context.Stores.ToList();
            Assert.Empty(stores);
        });
        
        component.WaitForAssertion(() =>
        {
            var noStoresMessage = component.Find("[data-testid='no-stores-message']");
            Assert.NotNull(noStoresMessage);
        });
    }

    [Fact]
    public void CancelButton_HidesForm_WhenClicked()
    {
        // Arrange
        var component = RenderComponent<Stores>();
        component.Find("[data-testid='create-store-button']").Click();

        // Act
        component.Find("[data-testid='cancel-button']").Click();

        // Assert
        Assert.Throws<ElementNotFoundException>(() => component.Find("[data-testid='store-form']"));
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _context.Dispose();
        }
        base.Dispose(disposing);
    }
}
