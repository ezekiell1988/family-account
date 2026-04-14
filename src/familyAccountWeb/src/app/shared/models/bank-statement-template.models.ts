// ── KeywordRule ─────────────────────────────────────────────────────────────
export interface KeywordRule {
  keywords:              string[];
  idBankMovementType:    number;
  idAccountCounterpart?: number | null;
  idCostCenter?:         number | null;
  matchMode:             'Any';
  regex?:                boolean;
}

// ── BankStatementTemplate models extendidos ─────────────────────────────
export interface BankStatementTemplateDto {
  idBankStatementTemplate: number;
  codeTemplate: string;
  nameTemplate: string;
  bankName: string;
  columnMappings: string;
  keywordRules?: string | null;
  dateFormat?: string | null;
  timeFormat?: string | null;
  isActive: boolean;
  notes?: string | null;
}

export interface CreateBankStatementTemplateRequest {
  codeTemplate: string;
  nameTemplate: string;
  bankName: string;
  columnMappings: string;
  keywordRules?: string | null;
  dateFormat?: string | null;
  timeFormat?: string | null;
  isActive: boolean;
  notes?: string | null;
}

export interface UpdateBankStatementTemplateRequest {
  codeTemplate: string;
  nameTemplate: string;
  bankName: string;
  columnMappings: string;
  keywordRules?: string | null;
  dateFormat?: string | null;
  timeFormat?: string | null;
  isActive: boolean;
  notes?: string | null;
}
