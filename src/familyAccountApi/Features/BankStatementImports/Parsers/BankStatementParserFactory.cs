namespace FamilyAccountApi.Features.BankStatementImports.Parsers;

/// <summary>
/// Selecciona el parser adecuado según el <c>CodeTemplate</c> de la plantilla bancaria.
/// Registrar como <c>singleton</c> en el contenedor de DI.
/// </summary>
public sealed class BankStatementParserFactory
{
    /// <summary>
    /// Devuelve el parser que corresponde al código de plantilla indicado.
    /// </summary>
    /// <exception cref="NotSupportedException">
    /// Si <paramref name="codeTemplate"/> no tiene un parser registrado.
    /// </exception>
    public IBankStatementParser GetParser(string codeTemplate) =>
        codeTemplate switch
        {
            "BCR-HTML-XLS-V1"  => new BcrXlsParser(),
            "BAC-TXT-V1"       => new BacTxtParser(),
            "BAC-TXT-CRC-V1"   => new BacTxtParser(),
            "BAC-TXT-USD-V1"   => new BacTxtParser(),
            "BAC-XLS-V1"       => new BacXlsParser(),
            "BNCR-CSV-V1"      => new BncrCsvParser(),
            _ => throw new NotSupportedException(
                $"No hay parser registrado para la plantilla '{codeTemplate}'. " +
                "Verifique el campo CodeTemplate en BankStatementTemplate.")
        };
}
