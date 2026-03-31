using FamilyAccountApi.Features.Contacts.Dtos;

namespace FamilyAccountApi.Features.Contacts;

public interface IContactService
{
    /// <summary>Retorna todos los contactos del tipo indicado (ej: "PRO").</summary>
    Task<IReadOnlyList<ContactResponse>> GetByTypeAsync(string contactTypeCode, CancellationToken ct = default);

    /// <summary>
    /// Busca un contacto cuyo Name coincida (insensible a mayúsculas) con el tipo indicado.
    /// Si no existe lo crea y lo vincula al tipo automáticamente.
    /// </summary>
    Task<ContactResponse> GetOrCreateAsync(string name, string contactTypeCode, CancellationToken ct = default);
}
