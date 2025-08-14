# Copilot Instructions for Family Shopping Application

## Project Overview
This is a Blazor Server application using Entity Framework and SQL Server targeting .NET 8.0. The application manages family shopping across multiple stores.

[Architecture Guidelines](../Specifications/architecture-guidelines.md)

[UI State Management Patterns](../Specifications/ui-state-management-guidelines.md)

[Error Handling Pattern](../Specifications/error-handling-guidelines.md)

[Testing Guidelines](../Specifications/testing-guidelines.md)

## Entity Design
- Store: Represents retail locations
- Product: Represents items that can be purchased
- ShoppingItem: Represents products needed for shopping with quantities
- ProductStore: Many-to-many relationship between Products and Stores
- Ingredient: Represents a product used in a Recipe
- RecipeIngredient: Many-to-many relationship between Recipe and Ingredients

## Key Features
- Dashboard for managing shopping lists and store selection
- Products page for inventory management and in what stores they can be purchased
- Stores page for grocery store management
- Recipes page for management of recipes and what their ingredients are (i.e., the products they need)
- Real-time updates using SignalR
- Windows authentication support
- Form validation with visual feedback
- Centralized error handling with user-friendly error display

## Assumptions
- You are done when *all* instructions included above are met and there is a complete set of tests for each page, and all tests succeed. This implies that if there is even a single @code block in any file, you are not done.
