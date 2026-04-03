namespace FamilyAccountApi.Domain.Entities;

public sealed class CompanyWhatsapp
{
    public int    IdCompanyWhatsapp { get; set; }
    public int    IdCompany         { get; set; }

    /// <summary>Número de teléfono con código de país, ej: +50688888888</summary>
    public string PhoneNumber       { get; set; } = null!;

    /// <summary>Phone Number ID otorgado por Meta Business Manager</summary>
    public string PhoneNumberId     { get; set; } = null!;

    /// <summary>WhatsApp Business Account ID (WABA ID)</summary>
    public string WabaId            { get; set; } = null!;

    /// <summary>Access Token de la app de Meta (permanente o temporal)</summary>
    public string AccessToken       { get; set; } = null!;

    /// <summary>Token que se configura en el webhook de Meta para verificación</summary>
    public string WebhookVerifyToken { get; set; } = null!;

    /// <summary>Versión de la API de WhatsApp Cloud, ej: v24.0</summary>
    public string ApiVersion         { get; set; } = null!;

    public bool   IsActive           { get; set; } = true;

    // Navegaciones
    public Company Company { get; set; } = null!;
}
