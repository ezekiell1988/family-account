// ── BankStatementTemplate ────────────────────────────────────────────────────
export interface BankStatementTemplateDto {
  idBankStatementTemplate: number;
  codeTemplate: string;
  nameTemplate: string;
  bankName: string;
  isActive: boolean;
}

// ── BankStatementImport ──────────────────────────────────────────────────────
export interface BankStatementImportDto {
  idBankStatementImport: number;
  idBankAccount: number;
  bankAccountCode: string;
  bankAccountName: string;
  idBankStatementTemplate: number;
  templateCode: string;
  templateName: string;
  fileName: string;
  importDate: string;
  importedBy: number;
  importedByName: string;
  status: string;
  totalTransactions: number;
  processedTransactions: number;
  errorMessage: string | null;
}

// ── BankStatementTransaction ─────────────────────────────────────────────────
export interface BankStatementTransactionDto {
  idBankStatementTransaction: number;
  idBankStatementImport: number;
  accountingDate: string;
  transactionDate: string;
  transactionTime: string | null;
  documentNumber: string | null;
  description: string;
  debitAmount: number | null;
  creditAmount: number | null;
  balance: number | null;
  isReconciled: boolean;
  idBankMovementType: number | null;
  bankMovementTypeName: string | null;
  movementSign: string | null;
  idAccountCounterpart: number | null;
  accountCounterpartName: string | null;
  idAccountingEntry: number | null;
}

// ── Classify request ─────────────────────────────────────────────────────────
export interface ClassifyTransactionRequest {
  idBankMovementType: number;
  idAccountCounterpart?: number | null;
}
