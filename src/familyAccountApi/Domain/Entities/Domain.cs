namespace FamilyAccountApi.Domain.Entities;

public sealed class CompanyDomain
{
    public int    IdDomain  { get; set; }
    public string DomainUrl { get; set; } = null!;
    public int    IdCompany { get; set; }

    // Navegaciones
    public Company Company { get; set; } = null!;
}
