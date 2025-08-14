using Microsoft.AspNetCore.Components;

namespace FamilyShoppingApp.Services;

public class ExceptionHandlerService
{
    public event Action<string>? OnError;
    
    public async Task HandleExceptionAsync<T>(Exception exception, string methodName) where T : ComponentBase
    {
        var componentName = typeof(T).Name;
        var errorMessage = $"Error in {componentName}.{methodName}: {exception.Message}";
        
        // Log the exception (in a real app, you'd use ILogger)
        Console.WriteLine($"[ERROR] {errorMessage}");
        Console.WriteLine($"[STACK TRACE] {exception.StackTrace}");
        
        // Notify the ErrorDisplay component
        OnError?.Invoke(errorMessage);
        
        await Task.CompletedTask;
    }
}
