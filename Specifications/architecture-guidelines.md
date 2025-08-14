## Architecture Guidelines
- Use Blazor Server with SignalR for real-time updates
- Implement DbContextFactory pattern for Blazor Server compatibility
- Use HTTP Context Accessor for Windows identity extraction
- Separate code-behind files (.razor.cs) for C# logic
- Use CSS isolation (.razor.css) for component-specific styling
- Use a ViewModel for each page
- Use @onchange instead of @bind for data binding
- Implement Entity Framework migrations for database schema changes

- Remove any code that came with the Blazor template: Counter, FetchData, WeatherForecast, SurveyPrompt
