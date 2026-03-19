---
name: scalar-dotnet
description: Use when implementing Scalar API documentation in .NET 10 applications; when setting up OpenAPI/Swagger documentation; when configuring JWT authentication in API docs; when users ask about API documentation, Scalar, OpenAPI transformers, or replacing Swagger UI
---

# Scalar API Documentation for .NET 10

Scalar is Microsoft's recommended modern alternative to Swagger UI for .NET 10. Provides superior performance, better authentication UX, and multi-language code examples.

**Official docs:** [Scalar](https://scalar.com/products/api-references) | [ASP.NET Core Integration](https://scalar.com/products/api-references/integrations/aspnetcore/integration) | [Microsoft OpenAPI](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi/using-openapi-documents)

## Why Scalar over Swagger UI?

- **Native in .NET 10**: Microsoft recommends Scalar as primary option
- **Modern UI**: Clean, contemporary design
- **Better performance**: Faster than traditional Swagger UI
- **Superior auth UX**: Intuitive JWT, OAuth2, API Keys configuration
- **Code generation**: Examples in cURL, JavaScript, Python, C#, etc.
- **Native support**: No need for Swashbuckle

---

## Quick Setup

### 1. Install Packages

```xml
<PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="10.0.2" />
<PackageReference Include="Microsoft.OpenApi" Version="2.0.0" />
<PackageReference Include="Scalar.AspNetCore" Version="2.12.32" />
```

### 2. Basic Configuration (Program.cs)

```csharp
var builder = WebApplication.CreateBuilder(args);

// Add native .NET 10 OpenAPI
builder.Services.AddOpenApi();

var app = builder.Build();

// Development only
if (app.Environment.IsDevelopment())
{
    // Expose OpenAPI document
    app.MapOpenApi();
    
    // Add Scalar UI
    app.MapScalarApiReference();
}

app.Run();
```

**Access:**
- Scalar UI: `https://localhost:7191/scalar/v1`
- OpenAPI JSON: `https://localhost:7191/openapi/v1.json`

---

## JWT Authentication Setup

En .NET 10 los tipos de OpenAPI están en el namespace `Microsoft.OpenApi` (no `Microsoft.OpenApi.Models`).

### Opción A — Bearer en TODOS los endpoints (incluyendo anónimos)

Usar un `IOpenApiDocumentTransformer` que añade el esquema y lo aplica a todas las operaciones:

```csharp
// BearerSecuritySchemeTransformer.cs
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

internal sealed class BearerSecuritySchemeTransformer(
    IAuthenticationSchemeProvider authenticationSchemeProvider)
    : IOpenApiDocumentTransformer
{
    public async Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        var authSchemes = await authenticationSchemeProvider.GetAllSchemesAsync();
        if (!authSchemes.Any(s => s.Name == "Bearer")) return;

        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes = new Dictionary<string, IOpenApiSecurityScheme>
        {
            ["Bearer"] = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                In = ParameterLocation.Header,
                BearerFormat = "JWT",
                Description = "Enter JWT token obtained from login endpoint"
            }
        };

        // Aplica a TODAS las operaciones
        foreach (var pathItem in document.Paths.Values)
        {
            foreach (var operation in pathItem.Operations.Values)
            {
                operation.Security ??= [];
                operation.Security.Add(new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference("Bearer", document)] = []
                });
            }
        }
    }
}
```

```csharp
// Program.cs — solo registra el document transformer
builder.Services.AddOpenApi("v1", options =>
{
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
});
```

---

### Opción B — Bearer SOLO en endpoints que requieren autorización ✅ Recomendado

Un único `IOpenApiDocumentTransformer` que hace ambas cosas: registrar el esquema **y** asignar
el requisito por operación. Los document transformers corren **después** de todos los operation
transformers (incluyendo los internos de ASP.NET Core), garantizando que el `{}` vacío que el
framework auto-inyecta en endpoints con `RequireAuthorization()` quede sobreescrito.

> **Fuente:** [Microsoft ASP.NET Core OpenAPI docs – transformer execution order](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi/using-openapi-documents#transformer-execution-order)

```csharp
// BearerSecuritySchemeTransformer.cs
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

internal sealed class BearerSecuritySchemeTransformer(
    IAuthenticationSchemeProvider authenticationSchemeProvider)
    : IOpenApiDocumentTransformer
{
    public async Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        var authSchemes = await authenticationSchemeProvider.GetAllSchemesAsync();
        if (!authSchemes.Any(s => s.Name == "Bearer")) return;

        // 1. Registrar esquema en components/securitySchemes
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes = new Dictionary<string, IOpenApiSecurityScheme>
        {
            ["Bearer"] = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                In = ParameterLocation.Header,
                BearerFormat = "JWT",
                Description = "Enter JWT token obtained from login endpoint"
            }
        };

        // 2. Construir lookup de endpoints anónimos
        //    context.DescriptionGroups es IReadOnlyList<ApiDescriptionGroup>;
        //    cada grupo tiene .Items (IReadOnlyList<ApiDescription>).
        var anonymousKeys = context.DescriptionGroups
            .SelectMany(g => g.Items)
            .Where(d => d.ActionDescriptor.EndpointMetadata
                .OfType<IAllowAnonymous>().Any())
            .Select(d => (
                Path: "/" + (d.RelativePath ?? string.Empty).TrimStart('/'),
                Method: d.HttpMethod?.ToUpperInvariant() ?? string.Empty
            ))
            .ToHashSet();

        // 3. ASIGNAR (no .Add()) para sobrescribir el {} vacío que ASP.NET Core inyecta.
        //    CRÍTICO: pasar `document` como hostDocument en OpenApiSecuritySchemeReference.
        //    Sin él, Target == null y el serializer omite la entrada → genera [{}] (Optional).
        foreach (var (path, pathItem) in document.Paths)
        {
            foreach (var (opType, operation) in pathItem.Operations)
            {
                var isAnonymous = anonymousKeys.Contains(
                    (path, opType.ToString().ToUpperInvariant()));

                operation.Security = isAnonymous
                    ? null
                    : [new OpenApiSecurityRequirement
                      {
                          [new OpenApiSecuritySchemeReference("Bearer", document)] = []
                      }];
            }
        }
    }
}
```

```csharp
// Program.cs — solo el document transformer (no hace falta operation transformer)
builder.Services.AddOpenApi("v1", options =>
{
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
});
```

> **Notas críticas:**
> - En .NET 10 los tipos son `OpenApiSecurityRequirement` y `OpenApiSecuritySchemeReference`,
>   ambos en el namespace `Microsoft.OpenApi` (no `Microsoft.OpenApi.Models`).
> - `IOpenApiSecurityScheme` es la interfaz; `OpenApiSecurityScheme` la implementación concreta.
> - `context.DescriptionGroups` (NO `ApiDescriptionGroups`) da acceso a los `ApiDescription`.
> - `opType.ToString().ToUpperInvariant()` sobre `OperationType` devuelve `"GET"`, `"POST"`, etc.,
>   que coincide con `d.HttpMethod` del `ApiDescription`.

---

## Customization

### Theme and Title

```csharp
app.MapScalarApiReference(options =>
{
    options
        .WithTitle("My API")
        .WithTheme(ScalarTheme.Purple)
        .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient)
        .ShowSidebar(true);
});
```

### Pre-fill Authentication (Development Only)

```csharp
// ⚠️ DEVELOPMENT ONLY - Never use real tokens in production
if (app.Environment.IsDevelopment())
{
    app.MapScalarApiReference(options => options
        .AddPreferredSecuritySchemes("Bearer")
        .AddHttpAuthentication("Bearer", auth =>
        {
            auth.Token = "your_dev_token_here";
        }));
}
```

---

## Using Authentication in Scalar

### Step 1: Get JWT Token

Call your login endpoint:

```bash
POST /api/v1/Auth/login
Content-Type: application/json

{
  "emailLogin": "user@example.com",
  "password": "Password123!"
}
```

Response:
```json
{
  "success": true,
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresAt": "2026-02-08T12:00:00Z"
}
```

### Step 2: Configure in Scalar

1. In Scalar UI, find the authentication button or "Bearer Token" field
2. Paste the JWT token (WITHOUT "Bearer" prefix)
3. Scalar automatically adds `Authorization: Bearer {token}` header

### Step 3: Test Protected Endpoints

Now `[Authorize]` endpoints work:
```csharp
app.MapGet("/api/v1/protected", [Authorize] () => "Secret data")
   .WithName("GetProtectedData");
```

---

## API Versioning

Support multiple API versions:

```csharp
builder.Services.AddOpenApi("v1");
builder.Services.AddOpenApi("v2");

app.MapOpenApi("/openapi/{documentName}.json");
app.MapScalarApiReference(options =>
{
    options.WithTitle("My API v1");
});
```

Access:
- v1: `https://localhost:7191/scalar/v1`
- v2: `https://localhost:7191/scalar/v2`

---

## Common Patterns

### Add Metadata to Endpoints

```csharp
app.MapGet("/api/users/{id}", GetUserById)
   .WithName("GetUser")
   .WithTags("Users")
   .WithSummary("Get user by ID")
   .WithDescription("Returns detailed user information")
   .Produces<UserDto>(StatusCodes.Status200OK)
   .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

static async Task<Results<Ok<UserDto>, NotFound<ProblemDetails>>> GetUserById(
    int id, 
    IUserService userService)
{
    var user = await userService.GetByIdAsync(id);
    return user is not null 
        ? TypedResults.Ok(user) 
        : TypedResults.NotFound(new ProblemDetails 
        { 
            Title = "User not found", 
            Status = 404 
        });
}
```

### Document Request/Response Models

Use attributes for better OpenAPI documentation:

```csharp
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

public record LoginRequest
{
    [Required, EmailAddress]
    [Description("User email address")]
    public required string EmailLogin { get; init; }

    [Required, MinLength(8)]
    [Description("User password (min 8 characters)")]
    public required string Password { get; init; }
}
```

### Exclude Endpoints from OpenAPI

```csharp
app.MapGet("/internal/health", () => "OK")
   .ExcludeFromDescription(); // Won't appear in Scalar
```

---

## Troubleshooting

### Authentication button not visible

1. Verify `BearerSecuritySchemeTransformer` is registered
2. Check JWT authentication service is configured
3. Inspect `/openapi/v1.json` for `components.securitySchemes`

### Token not accepted

1. Ensure token hasn't expired
2. Don't include "Bearer" prefix when pasting in Scalar
3. Verify JWT configuration in `appsettings.json`

### Scalar doesn't load

1. Check `ASPNETCORE_ENVIRONMENT=Development`
2. Confirm `Scalar.AspNetCore` package is installed
3. Ensure `app.MapScalarApiReference()` is called AFTER `app.MapOpenApi()`

### Scalar muestra "Authentication Optional" en vez de "Required"

**Causa raíz investigada (marzo 2026):**

El serializer de `OpenApiSecurityRequirement` ([fuente en microsoft/OpenAPI.NET](https://github.com/microsoft/OpenAPI.NET/blob/main/src/Microsoft.OpenApi/Models/OpenApiSecurityRequirement.cs))
contiene internamente:

```csharp
foreach (var pair in this.Where(static p => p.Key?.Target is not null))
```

Si no se pasa `document` como `hostDocument` al constructor de `OpenApiSecuritySchemeReference`,
`Target` es `null` y la entrada se omite. El resultado serializado es `[{}]` en lugar de
`[{"Bearer":[]}]`. Por especificación OpenAPI, `[{}]` significa "auth opcional" (acceso anónimo
soportado), mientras que `[{"Bearer":[]}]` significa "auth requerida".

> **Fuente spec:** [Scalar issue #8046](https://github.com/scalar/scalar/issues/8046) —
> confirmado por el equipo de Scalar: `[{},{"Bearer":[]}]` = Optional, `[{"Bearer":[]}]` = Required.

**Síntomas:**
- Scalar muestra el candado con etiqueta "Authentication Optional"
- El JSON en `/openapi/v1.json` tiene `"security": [{}]` en endpoints protegidos

**Solución:** Siempre pasar `document` al construir el `OpenApiSecuritySchemeReference`:

```csharp
// ❌ MAL — Target es null, se serializa como {}
[new OpenApiSecuritySchemeReference("Bearer")] = []

// ✅ BIEN — Target resuelve al esquema en components, se serializa como {"Bearer":[]}
[new OpenApiSecuritySchemeReference("Bearer", document)] = []
```

**Por qué no funciona el operation transformer para este caso:**
Los operation transformers corren ANTES que los document transformers. El document transformer
que registra el esquema en `components.securitySchemes` corre DESPUÉS. Por eso, si el
`OpenApiSecuritySchemeReference` se construye en un operation transformer, `Target` siempre
será null en ese momento (el esquema aún no existe en el documento).

**Diagnóstico rápido:**

```powershell
# Verificar qué valor real tiene security en el JSON generado
$json = Invoke-RestMethod http://localhost:5132/openapi/v1.json
$json.paths.PSObject.Properties | ForEach-Object {
    $path = $_.Name
    $_.Value.PSObject.Properties | ForEach-Object {
        $sec = $_.Value.security
        $secStr = if($null -ne $sec){ $sec | ConvertTo-Json -Compress } else { "(null)" }
        "{0} {1} => security: {2}" -f $_.Name.ToUpper(), $path, $secStr
    }
}
# Resultado esperado para endpoints protegidos: {"Bearer":[]}
# Resultado con bug:                             {}
```

---

## Best Practices

✅ **DO:**
- Use `IOpenApiDocumentTransformer` for security schemes
- Add metadata with `.WithName()`, `.WithTags()`, `.WithSummary()`
- Use `TypedResults<T>` for type-safe responses
- Document all public endpoints
- Use versioning for breaking changes

❌ **DON'T:**
- Hard-code authentication tokens in production
- Expose Scalar in production (development only)
- Forget to add `.Produces<T>()` for response types
- Use `[ApiExplorerSettings(IgnoreApi = true)]` without reason

---

## See Also

- [minimal-apis.md](../dotnet-10-csharp-14/minimal-apis.md) - Minimal API patterns
- [security.md](../dotnet-10-csharp-14/security.md) - JWT authentication setup
- Official Scalar docs: https://scalar.com
- Microsoft OpenAPI docs: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi

---

---

## Orden de ejecución de transformers

> **Fuente:** [Microsoft ASP.NET Core OpenAPI docs](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi/using-openapi-documents#transformer-execution-order)

```
Schema transformers → Operation transformers → Document transformers
```

Regla práctica:
- Usa **document transformer** cuando necesites acceso a `document.Components` (esquemas, securitySchemes) porque
  es el único punto donde el documento está completo.
- Los **operation transformers** NO deben construir `OpenApiSecuritySchemeReference` con hostDocument,
  porque `components.securitySchemes` aún no existe en ese momento.

---

**Last updated:** March 19, 2026  
**Scalar version:** 2.13.11  
**.NET version:** 10.0.104
