// ── Tipo de factura de compra ─────────────────────────────────────────────────
export interface PurchaseInvoiceTypeDto {
  idPurchaseInvoiceType:       number;
  codePurchaseInvoiceType:     string;
  namePurchaseInvoiceType:     string;
  counterpartFromBankMovement: boolean;
  isActive:                    boolean;
}

// ── Línea de factura ──────────────────────────────────────────────────────────
export interface PurchaseInvoiceLineDto {
  idPurchaseInvoiceLine: number;
  idPurchaseInvoice:     number;
  idProduct:             number | null;
  nameProduct:           string | null;
  idUnit:                number | null;
  codeUnit:              string | null;
  descriptionLine:       string;
  quantity:              number;
  quantityBase:          number;
  unitPrice:             number;
  taxPercent:            number;
  totalLineAmount:       number;
  lotNumber:             string | null;
  expirationDate:        string | null;
}

// ── Factura de compra ─────────────────────────────────────────────────────────
export interface PurchaseInvoiceDto {
  idPurchaseInvoice:           number;
  idFiscalPeriod:              number;
  nameFiscalPeriod:            string;
  idCurrency:                  number;
  codeCurrency:                string;
  nameCurrency:                string;
  idPurchaseInvoiceType:       number;
  codePurchaseInvoiceType:     string;
  namePurchaseInvoiceType:     string;
  counterpartFromBankMovement: boolean;
  idBankAccount:               number | null;
  codeBankAccount:             string | null;
  numberInvoice:               string;
  providerName:                string;
  dateInvoice:                 string;
  subTotalAmount:              number;
  taxAmount:                   number;
  totalAmount:                 number;
  statusInvoice:               string;
  descriptionInvoice:          string | null;
  exchangeRateValue:           number;
  createdAt:                   string;
  idAccountingEntry:           number | null;
  lines:                       PurchaseInvoiceLineDto[];
}

// ── Requests ──────────────────────────────────────────────────────────────────
export interface PurchaseInvoiceLineRequest {
  idProduct:       number | null;
  idUnit:          number | null;
  lotNumber:       string | null;
  expirationDate:  string | null;
  descriptionLine: string;
  quantity:        number;
  unitPrice:       number;
  taxPercent:      number;
  totalLineAmount: number;
}

export interface CreatePurchaseInvoiceRequest {
  idFiscalPeriod:        number;
  idCurrency:            number;
  idPurchaseInvoiceType: number;
  idBankAccount:         number | null;
  numberInvoice:         string;
  providerName:          string;
  dateInvoice:           string;
  subTotalAmount:        number;
  taxAmount:             number;
  totalAmount:           number;
  descriptionInvoice:    string | null;
  exchangeRateValue:     number;
  lines:                 PurchaseInvoiceLineRequest[];
}

export interface UpdatePurchaseInvoiceRequest {
  idFiscalPeriod:        number;
  idCurrency:            number;
  idPurchaseInvoiceType: number;
  idBankAccount:         number | null;
  numberInvoice:         string;
  providerName:          string;
  dateInvoice:           string;
  subTotalAmount:        number;
  taxAmount:             number;
  totalAmount:           number;
  descriptionInvoice:    string | null;
  exchangeRateValue:     number;
  lines:                 PurchaseInvoiceLineRequest[];
}
