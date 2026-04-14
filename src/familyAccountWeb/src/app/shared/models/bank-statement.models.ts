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
  idCostCenter: number | null;
  costCenterName: string | null;
  idAccountingEntry: number | null;
}

// ── Classify request ─────────────────────────────────────────────────────────
export interface ClassifyTransactionRequest {
  idBankMovementType: number;
  idAccountCounterpart?: number | null;
  idCostCenter?: number | null;
}

// ── Bulk classify ─────────────────────────────────────────────────────────────
export interface BulkClassifyItem {
  idBankStatementTransaction: number;
  idBankMovementType: number;
  idAccountCounterpart?: number | null;
  idCostCenter?: number | null;
  learnKeyword?: boolean;
  keywordText?: string | null;
}

export interface BulkClassifyRequest {
  items: BulkClassifyItem[];
}

export interface BulkClassifyResult {
  classified: number;
  keywordsAdded: number;
}
