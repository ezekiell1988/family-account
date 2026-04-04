// ── Tipo de producto ──────────────────────────────────────────────────────────
export interface ProductTypeDto {
  idProductType:          number;
  nameProductType:        string;
  descriptionProductType: string | null;
}

// ── Unidad de medida ──────────────────────────────────────────────────────────
export interface UnitOfMeasureDto {
  idUnit:   number;
  codeUnit: string;
  nameUnit: string;
  typeUnit: string;
}

// ── SKU de Producto ───────────────────────────────────────────────────────────
export interface ProductSKUSummaryDto {
  idProductSKU:   number;
  codeProductSKU: string;
  nameProductSKU: string;
}

export interface ProductSKUDto {
  idProductSKU:          number;
  codeProductSKU:        string;
  nameProductSKU:        string;
  brandProductSKU:       string | null;
  descriptionProductSKU: string | null;
  netContent:            string | null;
}

export interface CreateProductSKURequest {
  codeProductSKU:        string;
  nameProductSKU:        string;
  brandProductSKU:       string | null;
  descriptionProductSKU: string | null;
  netContent:            string | null;
}

export interface UpdateProductSKURequest {
  id:                    number;
  codeProductSKU:        string;
  nameProductSKU:        string;
  brandProductSKU:       string | null;
  descriptionProductSKU: string | null;
  netContent:            string | null;
}

// ── Producto ──────────────────────────────────────────────────────────────────
export interface ProductDto {
  idProduct:       number;
  codeProduct:     string;
  nameProduct:     string;
  idProductType:   number;
  nameProductType: string;
  idUnit:          number;
  codeUnit:        string;
  idProductParent: number | null;
  averageCost:     number;
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

export interface CreateProductRequest {
  codeProduct:     string;
  nameProduct:     string;
  idProductType:   number;
  idUnit:          number;
  idProductParent: number | null;
}

export interface UpdateProductRequest {
  codeProduct:     string;
  nameProduct:     string;
  idProductType:   number;
  idUnit:          number;
  idProductParent: number | null;
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
