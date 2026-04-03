namespace FamilyAccountApi.Domain.Entities;

public sealed class Company
{
    public int    IdCompany   { get; set; }
    public string CodeCompany { get; set; } = null!;
    public string NameCompany { get; set; } = null!;

    // Navegaciones
    public ICollection<CompanyDomain>    Domains    { get; set; } = [];
    public ICollection<CompanyWhatsapp>  Whatsapps  { get; set; } = [];
}
