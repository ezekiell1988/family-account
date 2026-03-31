using FamilyAccountApi.Features.Contacts.Dtos;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace FamilyAccountApi.Features.Contacts;

public static class ContactsModule
{
    public static IServiceCollection AddContactsModule(this IServiceCollection services)
    {
        services.AddScoped<IContactService, ContactService>();
        return services;
    }

    public static IEndpointRouteBuilder MapContactsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/contacts")
            .WithTags("Contacts")
            .RequireAuthorization();

        group.MapGet("/data.json", GetByType)
            .WithName("GetContactsByType")
            .WithSummary("Obtener contactos filtrados por tipo (ej: PRO, CLI)");

        group.MapPost("/get-or-create", GetOrCreate)
            .WithName("GetOrCreateContact")
            .WithSummary("Busca un contacto por nombre y tipo; lo crea si no existe");

        return app;
    }

    private static async Task<Ok<IReadOnlyList<ContactResponse>>> GetByType(
        IContactService service,
        string type,
        CancellationToken ct)
    {
        var items = await service.GetByTypeAsync(type, ct);
        return TypedResults.Ok(items);
    }

    private static async Task<Results<Created<ContactResponse>, ValidationProblem>> GetOrCreate(
        GetOrCreateContactRequest request,
        IContactService service,
        CancellationToken ct)
    {
        var item = await service.GetOrCreateAsync(request.Name, request.ContactTypeCode, ct);
        return TypedResults.Created($"/api/v1/contacts/{item.IdContact}.json", item);
    }
}
