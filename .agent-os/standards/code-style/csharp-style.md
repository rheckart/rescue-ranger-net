## .NET 9 and C\# 13 Coding Style Guide

This style guide outlines best practices for coding in **.NET 9** and **C\# 13** environments. It provides conventions to ensure code is readable, maintainable, and consistent across teams.

***

### General Principles

- **Consistency:** Use consistent indentation, spacing, and casing.
- **Clarity:** Write clear, self-explanatory code and avoid excessive cleverness.
- **Simplicity:** Prefer straightforward solutions over complex constructs.

***

### File Structure

- **One class per file** (exception: tightly coupled private nested types).
- **Namespace declarations** should match folder structure.

```csharp
namespace MyCompany.Project.Feature;
```

- **File encoding:** UTF-8 with BOM.

***

### Naming Conventions

#### Casing

- **PascalCase** for classes, methods, properties.
- **camelCase** for parameters and private fields.
- **ALL_CAPS** for constants.


#### Examples

```csharp
public class UserProfile { ... }
private int userAge;
public void SaveChanges() { ... }
public const int MaxRetries = 5;
```


#### Acronyms

- Treat acronyms as words: `HttpRequest`, not `HTTPRequest`.

***

### Indentation and Spacing

- Use **4 spaces** for indentation (no tabs).
- Each block `{}`:
    - Opening brace on a new line for types, methods.
    - Closing brace on a new line.
- **Blank lines:** Use to separate logical sections.

```csharp
public void Method()
{
    if (condition)
    {
        // ...
    }
}
```


***

### Variable Declaration \& Initialization

- Prefer **explicit types** for clarity, except when the type is obvious:
    - Good: `var user = new User();`
    - Avoid: `var x = DoSomething();` when the type isn’t clear from context.
- Initialize variables where declared.

***

### LINQ \& Collections

- Use method syntax for LINQ:

```csharp
var results = users.Where(u => u.IsActive).ToList();
```

- Prefer `var` when the right-hand side makes the type explicit.

***

### Method, Property, and Class Design

- Use **expression-bodied members** for small methods or properties:

```csharp
public string FullName => $"{FirstName} {LastName}";
```

- Favor **readonly** and **init-only** properties when possible.
- Limit method and constructor parameters to a reasonable number (commonly ≤4).

***

### Nullability

- Use the **nullable reference types** feature (`string?`).
- Always handle potential nulls with appropriate checks or the null-coalescing operator `??`, or pattern matching (`is not null`).

***

### Modern C\# 13 Features

- **Primary constructors** for concise data/layer/poco classes.

```csharp
public class User(string name, int age)
{
    public string Name { get; } = name;
    public int Age { get; } = age;
}
```

- **Default lambda parameters** and **collection expressions** when appropriate.
- Embrace **pattern matching** and recent enhancements for clarity and brevity.
- Use **required members** for object initialization.

***

### Exception Handling

- Catch only specific exceptions, not `Exception` unless necessary.
- Prefer pattern matching with `catch` in C\# 13.
- Prefer using Nuget libraries such as Ardalis.Result to return results to callers versus throwing exceptions for them to catch.
- Log exceptions appropriately; avoid silent failure.

***

### Asynchronous Programming

- Use `async` and `await` for asynchronous operations.
- Name async methods with the `Async` suffix:

```csharp
public async Task SaveAsync() { ... }
```

- Avoid using `.Result` or `.Wait()` on tasks.

***

### Comments and Documentation

- Write **XML documentation comments** for public APIs.
- Use inline comments sparingly; let code self-document when possible.
- Update comments when code changes.

***

### Usings and Dependencies

- Use **`global using`** for common namespaces (in .NET 9).
- Organize usings: System namespaces first, then others, separated by blank lines.
- Remove unused namespaces regularly.

***

### Code Analysis and Formatting Tools

- Enforce style with **EditorConfig** and analyzers.
- Use `dotnet format` and style-checking tools as part of CI.

***

### Example: Putting it All Together

```csharp
namespace MyApp.Models;

public class Dog(string name, int age)
{
    public string Name { get; } = name;
    public int Age { get; } = age;

    public string Speak() => "Woof!";
}
```
