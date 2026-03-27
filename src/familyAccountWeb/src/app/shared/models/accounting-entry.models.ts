// ── Lookups ──────────────────────────────────────────────────────────────────
export interface FiscalPeriodLookup {
  idFiscalPeriod: number;
  namePeriod: string;
  statusPeriod: string;
}

export interface CurrencyLookup {
  idCurrency: number;
  codeCurrency: string;
  nameCurrency: string;
}

// ── Línea de asiento ─────────────────────────────────────────────────────────
export interface AccountingEntryLineDto {
  idAccountingEntryLine: number;
  idAccount: number;
  codeAccount: string;
  nameAccount: string;
  debitAmount: number;
  creditAmount: number;
  descriptionLine: string | null;
}

// ── Cabecera de asiento ───────────────────────────────────────────────────────
export interface AccountingEntryDto {
  idAccountingEntry: number;
  idFiscalPeriod: number;
  nameFiscalPeriod: string;
  idCurrency: number;
  codeCurrency: string;
  nameCurrency: string;
  numberEntry: string;
  /** ISO date string (YYYY-MM-DD) */
  dateEntry: string;
  descriptionEntry: string;
  statusEntry: string;
  referenceEntry: string | null;
  exchangeRateValue: number;
  createdAt: string;
  lines: AccountingEntryLineDto[];
}

// ── Requests ──────────────────────────────────────────────────────────────────
export interface AccountingEntryLineRequest {
  idAccount: number;
  debitAmount: number;
  creditAmount: number;
  descriptionLine: string | null;
}

export interface CreateAccountingEntryRequest {
  idFiscalPeriod: number;
  idCurrency: number;
  numberEntry: string;
  /** ISO date string (YYYY-MM-DD) */
  dateEntry: string;
  descriptionEntry: string;
  statusEntry: string;
  referenceEntry: string | null;
  exchangeRateValue: number;
  lines: AccountingEntryLineRequest[];
}

export interface UpdateAccountingEntryRequest {
  idFiscalPeriod: number;
  idCurrency: number;
  numberEntry: string;
  /** ISO date string (YYYY-MM-DD) */
  dateEntry: string;
  descriptionEntry: string;
  statusEntry: string;
  referenceEntry: string | null;
  exchangeRateValue: number;
  lines: AccountingEntryLineRequest[];
}
