namespace FamilyAccountApi.Domain.Entities;

/// <summary>
/// Regla de disponibilidad condicional: el item <see cref="IdRestrictedItem"/>
/// solo está disponible cuando al menos uno de sus habilitadores está seleccionado.
/// </summary>
public sealed class ProductOptionItemAvailability
{
    public int IdProductOptionItemAvailability { get; set; }
    /// <summary>Item que queda restringido hasta que un habilitador esté activo.</summary>
    public int IdRestrictedItem  { get; set; }
    /// <summary>Item que habilita al item restringido.</summary>
    public int IdEnablingItem    { get; set; }

    public ProductOptionItem IdRestrictedItemNavigation { get; set; } = null!;
    public ProductOptionItem IdEnablingItemNavigation   { get; set; } = null!;
}
