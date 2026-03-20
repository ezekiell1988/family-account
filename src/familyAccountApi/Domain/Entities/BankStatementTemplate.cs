namespace FamilyAccountApi.Domain.Entities;

public sealed class BankStatementTemplate
{
    public int IdBankStatementTemplate { get; set; }
    public string CodeTemplate { get; set; } = null!;
    public string NameTemplate { get; set; } = null!;
    public string BankName { get; set; } = null!;
    public string ColumnMappings { get; set; } = null!;
    /// <summary>
    /// Reglas de palabras clave en formato JSON para auto-clasificar transacciones.
    /// Cada regla define palabras clave que, si aparecen en la descripción, asignan
    /// automáticamente un IdBankMovementType y opcionalmente un IdAccountCounterpart.
    /// Ejemplo: [{"keywords":["SALARIO","ITQS"],"idBankMovementType":1,"idAccountCounterpart":44}]
    /// </summary>
    public string? KeywordRules { get; set; }
    public string? DateFormat { get; set; }
    public string? TimeFormat { get; set; }
    public bool IsActive { get; set; }
    public string? Notes { get; set; }

    // Relaciones
    public ICollection<BankStatementImport> BankStatementImports { get; set; } = [];
}
