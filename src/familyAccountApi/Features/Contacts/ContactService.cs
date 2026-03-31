using FamilyAccountApi.Domain.Entities;
using FamilyAccountApi.Features.Contacts.Dtos;
using FamilyAccountApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FamilyAccountApi.Features.Contacts;

public sealed class ContactService(AppDbContext db) : IContactService
{
    public async Task<IReadOnlyList<ContactResponse>> GetByTypeAsync(
        string contactTypeCode, CancellationToken ct = default)
    {
        return await db.ContactContactType
            .AsNoTracking()
            .Where(cct => cct.ContactType.CodeContactType == contactTypeCode)
            .OrderBy(cct => cct.Contact.Name)
            .Select(cct => new ContactResponse(
                cct.Contact.IdContact,
                cct.Contact.CodeContact,
                cct.Contact.Name))
            .ToListAsync(ct);
    }

    public async Task<ContactResponse> GetOrCreateAsync(
        string name, string contactTypeCode, CancellationToken ct = default)
    {
        // Buscar contacto existente con ese nombre y tipo
        var existing = await db.ContactContactType
            .AsNoTracking()
            .Where(cct =>
                cct.ContactType.CodeContactType == contactTypeCode &&
                cct.Contact.Name.ToLower() == name.ToLower())
            .Select(cct => new ContactResponse(
                cct.Contact.IdContact,
                cct.Contact.CodeContact,
                cct.Contact.Name))
            .FirstOrDefaultAsync(ct);

        if (existing is not null)
            return existing;

        // Obtener el IdContactType
        var contactType = await db.ContactType
            .AsNoTracking()
            .FirstOrDefaultAsync(ct2 => ct2.CodeContactType == contactTypeCode, ct)
            ?? throw new InvalidOperationException($"ContactType '{contactTypeCode}' no encontrado.");

        // Generar un CodeContact único desde el nombre
        var baseCode = new string(
            name.ToUpperInvariant()
                .Replace(" ", "_")
                .Where(c => char.IsLetterOrDigit(c) || c == '_')
                .ToArray());

        if (baseCode.Length > 48) baseCode = baseCode[..48];
        if (baseCode.Length == 0) baseCode = "CNT";

        var code = baseCode;
        var suffix = 1;
        while (await db.Contact.AnyAsync(c => c.CodeContact == code, ct))
        {
            code = $"{baseCode}_{suffix++}";
            if (code.Length > 50) code = code[..50];
        }

        var contact = new Contact { CodeContact = code, Name = name };
        db.Contact.Add(contact);
        await db.SaveChangesAsync(ct);

        db.ContactContactType.Add(new ContactContactType
        {
            IdContact     = contact.IdContact,
            IdContactType = contactType.IdContactType,
        });
        await db.SaveChangesAsync(ct);

        return new ContactResponse(contact.IdContact, contact.CodeContact, contact.Name);
    }
}
