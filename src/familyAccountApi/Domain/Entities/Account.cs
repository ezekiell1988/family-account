namespace FamilyAccountApi.Domain.Entities;

/// <summary>
/// Cuenta contable. Soporta jerarquía auto-referenciada (idAccountParent → account).
/// typeAccount: Activo | Pasivo | Capital | Ingreso | Gasto | Control
/// </summary>
public sealed class Account
{
    public int    IdAccount       { get; set; }
    public string CodeAccount     { get; set; } = null!;  // ej: "1", "1.1", "1.1.01"
    public string NameAccount     { get; set; } = null!;
    public string TypeAccount     { get; set; } = null!;  // check constraint
    public int    LevelAccount    { get; set; }
    public int?   IdAccountParent { get; set; }           // null = cuenta raíz
    public bool   AllowsMovements { get; set; }           // permite asientos directos
    public bool   IsActive        { get; set; } = true;

    // Navegaciones (auto-referencia)
    public Account?             Parent   { get; set; }
    public ICollection<Account> Children { get; set; } = [];
    public ICollection<AccountingEntryLine> AccountingEntryLines { get; set; } = [];
}
