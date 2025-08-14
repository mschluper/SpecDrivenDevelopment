## Error Handling Pattern
- All pages must inject ExceptionHandlerService and wrap service calls in try-catch blocks
- Use `await ExceptionHandler.HandleExceptionAsync<ComponentType>(ex, "MethodName")` pattern
- ErrorDisplay component is included in MainLayout and shows errors as dismissible toast notifications
- ErrorDisplay is positioned fixed top-right with animations and responsive design (on top of all else, even modals)
- Service registration required: `builder.Services.AddScoped<ExceptionHandlerService>();`
