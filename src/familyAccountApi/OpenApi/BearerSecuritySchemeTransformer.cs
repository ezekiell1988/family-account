using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace FamilyAccountApi.OpenApi;

/// <summary>
/// Registra el esquema Bearer en OpenAPI y aplica el requisito de seguridad
/// solo en los endpoints que NO tienen AllowAnonymous.
/// Corre como DocumentTransformer (último en la cadena), después de todos los
/// OperationTransformers internos de ASP.NET Core, garantizando que el
/// {} vacío auto-inyectado quede sobreescrito.
/// </summary>
internal sealed class BearerSecuritySchemeTransformer(
    IAuthenticationSchemeProvider authenticationSchemeProvider) : IOpenApiDocumentTransformer
{
    public async Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        var authSchemes = await authenticationSchemeProvider.GetAllSchemesAsync();
        if (!authSchemes.Any(s => s.Name == "Bearer")) return;

        // 1. Registrar el esquema en components/securitySchemes
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes = new Dictionary<string, IOpenApiSecurityScheme>
        {
            ["Bearer"] = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                In = ParameterLocation.Header,
                BearerFormat = "JWT",
                Description = "Ingresa el JWT obtenido en el endpoint de login"
            }
        };

        // 2. Construir lookup de endpoints anónimos desde DescriptionGroups
        var anonymousKeys = context.DescriptionGroups
            .SelectMany(g => g.Items)
            .Where(d => d.ActionDescriptor.EndpointMetadata
                .OfType<IAllowAnonymous>()
                .Any())
            .Select(d => (
                Path: "/" + (d.RelativePath ?? string.Empty).TrimStart('/'),
                Method: d.HttpMethod?.ToUpperInvariant() ?? string.Empty
            ))
            .ToHashSet();

        // 3. Aplicar (o limpiar) seguridad en cada operación
        //    Usamos asignación directa para sobrescribir el {} vacío que
        //    ASP.NET Core inyecta en endpoints con RequireAuthorization().
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
