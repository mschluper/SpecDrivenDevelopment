## Testing Guidelines
- Use bUnit for Blazor component testing
- Use WaitForAssertion instead of Task.Delay to ensure state has changed before an assertion check
- Use in-memory database for unit test isolation
- Write comprehensive tests for all CRUD operations
- Verify database state changes explicitly in tests
- Test UI interactions and form validation
- Prefer CSS selectors with data-testid over searching for text content in tests