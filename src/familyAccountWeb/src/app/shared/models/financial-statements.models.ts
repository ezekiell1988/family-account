// ── Filtro ────────────────────────────────────────────────────────────────────
export interface FinancialStatementFilter {
  idFiscalPeriod?: number | null;
  dateFrom?: string | null;   // yyyy-MM-dd
  dateTo?: string | null;     // yyyy-MM-dd
  year?: number | null;
  month?: number | null;
}

// ── Estado de Resultado ───────────────────────────────────────────────────────
export interface IncomeStatementLineDto {
  idAccount: number;
  codeAccount: string;
  nameAccount: string;
  levelAccount: number;
  debitTotal: number;
  creditTotal: number;
  balance: number;
}

export interface IncomeStatementDto {
  dateFrom: string;
  dateTo: string;
  revenues: IncomeStatementLineDto[];
  expenses: IncomeStatementLineDto[];
  totalRevenues: number;
  totalExpenses: number;
  netIncome: number;
}

// ── Estado de Situación Financiera ────────────────────────────────────────────
export interface BalanceSheetLineDto {
  idAccount: number;
  codeAccount: string;
  nameAccount: string;
  levelAccount: number;
  debitTotal: number;
  creditTotal: number;
  balance: number;
}

export interface BalanceSheetDto {
  dateTo: string;
  assets: BalanceSheetLineDto[];
  liabilities: BalanceSheetLineDto[];
  capital: BalanceSheetLineDto[];
  totalAssets: number;
  totalLiabilities: number;
  totalCapital: number;
  totalLiabilitiesAndCapital: number;
}

// ── Estado de Flujo de Efectivo ───────────────────────────────────────────────
export interface CashFlowLineDto {
  idAccount: number;
  codeAccount: string;
  nameAccount: string;
  levelAccount: number;
  openingBalance: number;
  periodDebits: number;
  periodCredits: number;
  closingBalance: number;
  change: number;
}

export interface CashFlowStatementDto {
  dateFrom: string;
  dateTo: string;
  netIncome: number;
  assetMovements: CashFlowLineDto[];
  liabilityMovements: CashFlowLineDto[];
  equityMovements: CashFlowLineDto[];
  totalAssetChange: number;
  totalLiabilityChange: number;
  totalEquityChange: number;
}

// ── Estado de Cambios en el Patrimonio ────────────────────────────────────────
export interface EquityMovementDto {
  idAccount: number;
  codeAccount: string;
  nameAccount: string;
  levelAccount: number;
  openingBalance: number;
  creditsInPeriod: number;
  debitsInPeriod: number;
  closingBalance: number;
}

export interface EquityStatementDto {
  dateFrom: string;
  dateTo: string;
  netIncome: number;
  movements: EquityMovementDto[];
  totalOpeningEquity: number;
  totalContributions: number;
  totalWithdrawals: number;
  totalClosingEquity: number;
  totalEquityIncludingNetIncome: number;
}
