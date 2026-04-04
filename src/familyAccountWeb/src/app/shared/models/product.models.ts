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
  hasOptions:      boolean;
  isCombo:         boolean;
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
  hasOptions:      boolean;
  isCombo:         boolean;
}

export interface UpdateProductRequest {
  codeProduct:     string;
  nameProduct:     string;
  idProductType:   number;
  idUnit:          number;
  idProductParent: number | null;
  hasOptions:      boolean;
  isCombo:         boolean;
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

// ── Presentación / unidad de venta ────────────────────────────────────────────
export interface ProductUnitDto {
  idProductUnit:     number;
  idProduct:         number;
  idUnit:            number;
  codeUnit:          string;
  conversionFactor:  number;
  isBase:            boolean;
  usedForPurchase:   boolean;
  usedForSale:       boolean;
  codeBarcode:       string | null;
  namePresentation:  string | null;
  brandPresentation: string | null;
  salePrice:         number;
}

export interface CreateProductUnitRequest {
  idProduct:         number;
  idUnit:            number;
  conversionFactor:  number;
  isBase:            boolean;
  usedForPurchase:   boolean;
  usedForSale:       boolean;
  codeBarcode:       string | null;
  namePresentation:  string | null;
  brandPresentation: string | null;
  salePrice:         number;
}

export interface UpdateProductUnitRequest {
  conversionFactor:  number;
  isBase:            boolean;
  usedForPurchase:   boolean;
  usedForSale:       boolean;
  codeBarcode:       string | null;
  namePresentation:  string | null;
  brandPresentation: string | null;
  salePrice:         number;
}

// ── Grupo de opciones ─────────────────────────────────────────────────────────
export interface ProductOptionItemDto {
  idProductOptionItem: number;
  nameItem:            string;
  priceDelta:          number;
  isDefault:           boolean;
  sortOrder:           number;
}

export interface ProductOptionGroupDto {
  idProductOptionGroup: number;
  idProduct:            number;
  nameGroup:            string;
  isRequired:           boolean;
  minSelections:        number;
  maxSelections:        number;
  allowSplit:           boolean;
  sortOrder:            number;
  items:                ProductOptionItemDto[];
}

export interface ProductOptionItemRequest {
  nameItem:   string;
  priceDelta: number;
  isDefault:  boolean;
  sortOrder:  number;
}

export interface CreateProductOptionGroupRequest {
  idProduct:     number;
  nameGroup:     string;
  isRequired:    boolean;
  minSelections: number;
  maxSelections: number;
  allowSplit:    boolean;
  sortOrder:     number;
  items:         ProductOptionItemRequest[];
}

export interface UpdateProductOptionGroupRequest {
  nameGroup:     string;
  isRequired:    boolean;
  minSelections: number;
  maxSelections: number;
  allowSplit:    boolean;
  sortOrder:     number;
  items:         ProductOptionItemRequest[];
}

// ── Slots de combo ────────────────────────────────────────────────────────────
export interface ProductComboSlotProductDto {
  idProductComboSlotProduct: number;
  idProduct:                 number;
  nameProduct:               string;
  priceAdjustment:           number;
  sortOrder:                 number;
}

export interface ProductComboSlotDto {
  idProductComboSlot: number;
  idProductCombo:     number;
  nameSlot:           string;
  quantity:           number;
  isRequired:         boolean;
  sortOrder:          number;
  products:           ProductComboSlotProductDto[];
}

export interface ProductComboSlotProductRequest {
  idProduct:       number;
  priceAdjustment: number;
  sortOrder:       number;
}

export interface CreateProductComboSlotRequest {
  idProductCombo: number;
  nameSlot:       string;
  quantity:       number;
  isRequired:     boolean;
  sortOrder:      number;
  products:       ProductComboSlotProductRequest[];
}

export interface UpdateProductComboSlotRequest {
  nameSlot:   string;
  quantity:   number;
  isRequired: boolean;
  sortOrder:  number;
  products:   ProductComboSlotProductRequest[];
}
