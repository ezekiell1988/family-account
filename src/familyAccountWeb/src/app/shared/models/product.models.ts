// ── Resumen de SKU dentro de Product ──────────────────────────────────────────
export interface ProductSKUSummaryDto {
  idProductSKU:    number;
  codeProductSKU:  string;
  nameProductSKU:  string;
  brandProductSKU: string | null;
  netContent:      string | null;
}

// ── Producto ──────────────────────────────────────────────────────────────────
export interface ProductDto {
  idProduct:   number;
  codeProduct: string;
  nameProduct: string;
  skus:        ProductSKUSummaryDto[];
}

// ── Centro de costo ───────────────────────────────────────────────────────────
export interface CostCenterDto {
  idCostCenter:   number;
  codeCostCenter: string;
  nameCostCenter: string;
  isActive:       boolean;
}

// ── Distribución contable de producto ─────────────────────────────────────────
export interface ProductAccountDto {
  idProductAccount:  number;
  idProduct:         number;
  codeProduct:       string;
  nameProduct:       string;
  idAccount:         number;
  codeAccount:       string;
  nameAccount:       string;
  idCostCenter:      number | null;
  nameCostCenter:    string | null;
  percentageAccount: number;
}

// ── Requests ──────────────────────────────────────────────────────────────────
export interface CreateProductAccountRequest {
  idProduct:         number;
  idAccount:         number;
  idCostCenter:      number | null;
  percentageAccount: number;
}

export interface UpdateProductAccountRequest {
  idAccount:         number;
  idCostCenter:      number | null;
  percentageAccount: number;
}

export interface CreateProductSKURequest {
  codeProductSKU:        string;
  nameProductSKU:        string;
  brandProductSKU?:      string | null;
  descriptionProductSKU?: string | null;
  netContent?:            string | null;
}

export interface CreateProductRequest {
  codeProduct: string;
  nameProduct: string;
}

/** Payload para crear un SKU+Producto+cuentas contables en una sola operación. */
export interface CreateProductWithAccountsRequest {
  skuCode:  string;
  skuName:  string;
  accounts: Array<{
    idAccount:         number;
    idCostCenter:      number | null;
    percentageAccount: number;
  }>;
}

export interface UpdateProductRequest {
  codeProduct: string;
  nameProduct: string;
}

export interface UpdateProductSKURequest {
  codeProductSKU:        string;
  nameProductSKU:        string;
  brandProductSKU:       string | null;
  descriptionProductSKU: string | null;
  netContent:            string | null;
}

// ── Categoría de producto ─────────────────────────────────────────────────────
export interface ProductCategoryDto {
  idProductCategory:   number;
  nameProductCategory: string;
}

export interface CreateProductCategoryRequest {
  nameProductCategory: string;
}

export interface UpdateProductCategoryRequest {
  nameProductCategory: string;
}
