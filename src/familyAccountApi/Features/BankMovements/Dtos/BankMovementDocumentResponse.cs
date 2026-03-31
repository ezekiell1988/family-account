namespace FamilyAccountApi.Features.BankMovements.Dtos;

public sealed record BankMovementDocumentResponse(
    int      IdBankMovementDocument,
    int?     IdPurchaseInvoice,
    string?  NumberPurchaseInvoice,
    string   TypeDocument,
    string?  NumberDocument,
    DateOnly DateDocument,
    decimal  AmountDocument,
    string?  DescriptionDocument);
