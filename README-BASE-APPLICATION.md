# Family Shopping Application - Base Implementation

This is the Base Application implementation as defined in the roadmap, focusing solely on the Stores page functionality.

## Project Structure

```
FamilyShoppingApp.sln                    # Solution file
src/
├── FamilyShoppingApp/                   # Main Blazor Server application
│   ├── Models/                          # Data models (Store, Product, Recipe, etc.)
│   ├── Data/                           # DbContext and database configuration  
│   ├── Services/                       # Business logic services
│   ├── ViewModels/                     # Page view models
│   ├── Pages/                          # Blazor pages and components
│   ├── Shared/                         # Shared layout components
│   ├── Components/                     # Reusable components
│   └── wwwroot/                        # Static assets
tests/
└── FamilyShoppingApp.Tests/           # xUnit + bUnit tests
    ├── Services/                       # Service layer tests
    └── Pages/                         # Component/UI tests
```

## Implemented Features

### Base Application - Stores Page Only

✅ **Store Registration** (Test #11): Users can add new retail locations by providing store names and optional details
✅ **Store Modification** (Test #12): Existing store information can be updated  
✅ **Store Removal** (Test #13): Users can remove stores from their system
✅ **Form Validation**: Proper validation with visual feedback and disabled submit buttons
✅ **Error Handling**: Global error handling with toast notifications
✅ **Database Integration**: Entity Framework with SQL Server LocalDB
✅ **Comprehensive Testing**: Both service-layer and UI tests with bUnit

## Architecture Compliance

✅ **Blazor Server with SignalR**: Real-time updates capability
✅ **DbContextFactory Pattern**: Proper database context management 
✅ **Code-behind Separation**: `.razor.cs` files for C# logic
✅ **CSS Isolation**: Component-specific styling
✅ **ViewModel Pattern**: Page-specific view models
✅ **@onchange Data Binding**: Instead of @bind pattern
✅ **Entity Framework Migrations**: Database schema versioning

## Error Handling

✅ **ExceptionHandlerService**: Centralized exception handling
✅ **ErrorDisplay Component**: Fixed top-right toast notifications
✅ **Service Registration**: Proper DI configuration
✅ **Try-catch Wrapping**: All service calls wrapped with error handling

## UI State Management

✅ **Minimal State**: Using primitive types (int, string, bool)
✅ **Transformation**: Complex objects created only at service boundaries
✅ **Clean Separation**: UI state separate from domain models

## Testing

✅ **bUnit Integration**: Full component testing framework
✅ **In-Memory Database**: Isolated test data
✅ **WaitForAssertion**: Proper async test patterns
✅ **CRUD Coverage**: All create, read, update, delete operations tested
✅ **UI Interaction Tests**: Form validation and user interactions
✅ **CSS Selectors**: Using data-testid attributes for reliable tests

## Running the Application

### Prerequisites
- .NET 8.0 SDK
- SQL Server LocalDB (included with Visual Studio)

### Setup
```bash
# Navigate to solution directory
cd SpecDrivenDevelopment

# Restore packages
dotnet restore

# Apply database migrations
dotnet ef database update --project src/FamilyShoppingApp

# Run the application
dotnet run --project src/FamilyShoppingApp
```

### Testing
```bash
# Run all tests
dotnet test

# Run with detailed output
dotnet test --verbosity detailed
```

### Access
- Application: http://localhost:5000
- HTTPS: https://localhost:7000

## Database

- **Provider**: SQL Server LocalDB
- **Database**: FamilyShoppingAppDb_Dev (Development)
- **Connection String**: Configured in appsettings.json
- **Migrations**: Located in `Migrations/` folder

## Next Steps

This base application provides the foundation for incremental development:

- **Increment 1**: Add Products page
- **Increment 2**: Add Dashboard page  
- **Increment 3**: Add Recipes page

Each increment will build upon this solid foundation while maintaining all architectural guidelines and testing standards.
