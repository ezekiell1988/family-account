// ── Documento de soporte ─────────────────────────────────────────────────────
export interface BankMovementDocumentDto {
  idBankMovementDocument: number;
  idAccountingEntry:      number | null;
  typeDocument:           string;
  numberDocument:         string | null;
  dateDocument:           string;
  amountDocument:         number;
  descriptionDocument:    string | null;
}

export interface BankMovementDocumentRequest {
  typeDocument:        string;
  numberDocument:      string | null;
  dateDocument:        string;
  amountDocument:      number;
  descriptionDocument: string | null;
  idAccountingEntry:   number | null;
}

// ── Movimiento bancario ───────────────────────────────────────────────────────
export interface BankMovementDto {
  idBankMovement:      number;
  idBankAccount:       number;
  codeBankAccount:     string;
  accountNumber:       string;
  idBankMovementType:  number;
  codeBankMovementType: string;
  nameBankMovementType: string;
  movementSign:        string;
  idFiscalPeriod:      number;
  nameFiscalPeriod:    string;
  numberMovement:      string;
  /** ISO date string (YYYY-MM-DD) */
  dateMovement:        string;
  descriptionMovement: string;
  amount:              number;
  statusMovement:      string;
  referenceMovement:   string | null;
  exchangeRateValue:   number;
  createdAt:           string;
  documents:           BankMovementDocumentDto[];
}

// ── Requests ──────────────────────────────────────────────────────────────────
export interface CreateBankMovementRequest {
  idBankAccount:       number;
  idBankMovementType:  number;
  idFiscalPeriod:      number;
  numberMovement:      string;
  dateMovement:        string;
  descriptionMovement: string;
  amount:              number;
  statusMovement:      string;
  referenceMovement:   string | null;
  exchangeRateValue:   number;
  documents:           BankMovementDocumentRequest[];
}

export interface UpdateBankMovementRequest {
  idBankAccount:       number;
  idBankMovementType:  number;
  idFiscalPeriod:      number;
  numberMovement:      string;
  dateMovement:        string;
  descriptionMovement: string;
  amount:              number;
  statusMovement:      string;
  referenceMovement:   string | null;
  exchangeRateValue:   number;
  documents:           BankMovementDocumentRequest[];
}
