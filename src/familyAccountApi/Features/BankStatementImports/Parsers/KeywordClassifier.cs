using System.Globalization;
using System.Text;
using System.Text.Json;

namespace FamilyAccountApi.Features.BankStatementImports.Parsers;

/// <summary>
/// Regla de clasificación asociada a una plantilla de extracto.
/// Se almacena como JSON en BankStatementTemplate.KeywordRules.
/// </summary>
public sealed record KeywordRule
{
    /// <summary>Palabras clave (insensibles a mayúsculas/acentos) que deben aparecer en la descripción.</summary>
    public IReadOnlyList<string> Keywords { get; init; } = [];
    /// <summary>ID del BankMovementType que se asignará si se cumple la regla.</summary>
    public int IdBankMovementType { get; init; }
    /// <summary>Cuenta contable contrapartida que sobreescribe la del tipo (opcional).</summary>
    public int? IdAccountCounterpart { get; init; }
    /// <summary>
    /// Modo de coincidencia: "Any" (basta con una palabra) o "All" (deben estar todas).
    /// Por defecto "Any".
    /// </summary>
    public string MatchMode { get; init; } = "Any";
}

/// <summary>
/// Resultado de la clasificación automática de una transacción.
/// </summary>
public sealed record ClassificationResult(
    int  IdBankMovementType,
    int? IdAccountCounterpart);

/// <summary>
/// Clasifica transacciones según palabras clave definidas en la plantilla de extracto.
/// </summary>
public static class KeywordClassifier
{
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Intenta clasificar la descripción usando las reglas de la plantilla.
    /// Retorna null si ninguna regla coincide.
    /// </summary>
    public static ClassificationResult? Classify(string description, string? keywordRulesJson)
    {
        if (string.IsNullOrWhiteSpace(keywordRulesJson)) return null;

        List<KeywordRule>? rules;
        try
        {
            rules = JsonSerializer.Deserialize<List<KeywordRule>>(keywordRulesJson, JsonOpts);
        }
        catch (JsonException)
        {
            return null;
        }

        if (rules is null || rules.Count == 0) return null;

        var normalizedDescription = Normalize(description);

        foreach (var rule in rules)
        {
            if (rule.Keywords.Count == 0) continue;

            var normalizedKeywords = rule.Keywords.Select(Normalize).ToList();

            bool matches = rule.MatchMode.Equals("All", StringComparison.OrdinalIgnoreCase)
                ? normalizedKeywords.All(k => normalizedDescription.Contains(k, StringComparison.Ordinal))
                : normalizedKeywords.Any(k => normalizedDescription.Contains(k, StringComparison.Ordinal));

            if (matches)
                return new ClassificationResult(rule.IdBankMovementType, rule.IdAccountCounterpart);
        }

        return null;
    }

    /// <summary>Normaliza texto a mayúsculas sin acentos para comparación.</summary>
    private static string Normalize(string input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;

        var normalized = input.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalized.Length);

        foreach (var c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }

        return sb.ToString().Normalize(NormalizationForm.FormC).ToUpperInvariant();
    }
}
