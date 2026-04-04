import {
  Component,
  ChangeDetectionStrategy,
  input,
  output,
  signal,
  computed,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { addIcons } from 'ionicons';
import {
  addOutline,
  pencilOutline,
  trashOutline,
  chevronDownOutline,
  chevronForwardOutline,
  pricetagsOutline,
  warningOutline,
  closeOutline,
  saveOutline,
  refreshOutline,
  layersOutline,
  pricetagOutline,
  settingsOutline,
  cubeOutline,
} from 'ionicons/icons';
import {
  IonContent,
  IonRefresher,
  IonRefresherContent,
  IonSpinner,
  IonText,
  IonList,
  IonItem,
  IonLabel,
  IonBadge,
  IonCard,
  IonCardHeader,
  IonCardTitle,
  IonCardContent,
  IonButton,
  IonIcon,
  IonInput,
  IonSelect,
  IonSelectOption,
  IonGrid,
  IonRow,
  IonCol,
  IonFab,
  IonFabButton,
  IonToggle,
  IonSegment,
  IonSegmentButton,
  IonNote,
} from '@ionic/angular/standalone';
import { HeaderComponent, FooterComponent } from '../../../../../components';
import {
  ProductDto,
  ProductTypeDto,
  UnitOfMeasureDto,
  ProductCategoryDto,
  ProductUnitDto,
  ProductOptionGroupDto,
  ProductComboSlotDto,
  ProductOptionItemRequest,
  ProductComboSlotProductRequest,
  CreateProductRequest,
  UpdateProductRequest,
  CreateProductUnitRequest,
  UpdateProductUnitRequest,
  CreateProductOptionGroupRequest,
  UpdateProductOptionGroupRequest,
  CreateProductComboSlotRequest,
  UpdateProductComboSlotRequest,
} from '../../../../../shared/models';

@Component({
  selector: 'app-products-mobile',
  host: { class: 'ion-page' },
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    HeaderComponent,
    FooterComponent,
    IonContent,
    IonRefresher,
    IonRefresherContent,
    IonSpinner,
    IonText,
    IonList,
    IonItem,
    IonLabel,
    IonBadge,
    IonCard,
    IonCardHeader,
    IonCardTitle,
    IonCardContent,
    IonButton,
    IonIcon,
    IonInput,
    IonSelect,
    IonSelectOption,
    IonGrid,
    IonRow,
    IonCol,
    IonFab,
    IonFabButton,
    IonToggle,
    IonSegment,
    IonSegmentButton,
    IonNote,
  ],
  templateUrl: './products-mobile.component.html',
})
export class ProductsMobileComponent {
  // ── Inputs ────────────────────────────────────────────────────────
  products          = input<ProductDto[]>([]);
  productTypes      = input<ProductTypeDto[]>([]);
  units             = input<UnitOfMeasureDto[]>([]);
  allUnits          = input<UnitOfMeasureDto[]>([]);
  allProducts       = input<ProductDto[]>([]);
  categories        = input<ProductCategoryDto[]>([]);
  expandedUnits        = input<ProductUnitDto[]>([]);
  expandedOptionGroups = input<ProductOptionGroupDto[]>([]);
  expandedComboSlots   = input<ProductComboSlotDto[]>([]);
  loadingDetail     = input(false);
  isLoading         = input(false);
  errorMessage      = input('');
  deletingProductId = input<number | null>(null);

  // ── Outputs ───────────────────────────────────────────────────────
  refresh       = output<void>();
  createProduct = output<CreateProductRequest>();
  editProduct   = output<UpdateProductRequest & { id: number }>();
  removeProduct = output<number>();
  clearError    = output<void>();
  rowExpanded   = output<ProductDto>();
  addCategory    = output<{ idProduct: number; idProductCategory: number }>();
  removeCategory = output<{ idProduct: number; idProductCategory: number }>();
  createUnit     = output<CreateProductUnitRequest>();
  editUnit       = output<UpdateProductUnitRequest & { id: number }>();
  removeUnit     = output<number>();
  createOptionGroup = output<CreateProductOptionGroupRequest>();
  editOptionGroup   = output<UpdateProductOptionGroupRequest & { id: number }>();
  removeOptionGroup = output<number>();
  createComboSlot   = output<CreateProductComboSlotRequest>();
  editComboSlot     = output<UpdateProductComboSlotRequest & { id: number }>();
  removeComboSlot   = output<number>();

  // ── Estado de UI ──────────────────────────────────────────────────
  expandedId      = signal<number | null>(null);
  activeDetailTab = signal<'units' | 'categories' | 'options' | 'combo'>('units');

  // ── Formulario crear/editar producto ─────────────────────────────
  showForm            = signal(false);
  editingId           = signal<number | null>(null);
  formCode            = signal('');
  formName            = signal('');
  formProductTypeId   = signal<number | null>(null);
  formUnitId          = signal<number | null>(null);
  formProductParentId = signal<number | null>(null);
  formHasOptions      = signal(false);
  formIsCombo         = signal(false);
  confirmDeleteId     = signal<number | null>(null);

  isEditing   = computed(() => this.editingId() !== null);
  isFormValid = computed(() =>
    this.formCode().trim().length > 0 &&
    this.formName().trim().length > 0 &&
    this.formProductTypeId() !== null &&
    this.formUnitId() !== null,
  );

  // ── Formulario de presentación ────────────────────────────────────
  showUnitForm        = signal(false);
  editingUnitId       = signal<number | null>(null);
  formUnitSelectId    = signal<number | null>(null);
  formUnitConvFactor  = signal<number>(1);
  formUnitIsBase      = signal(false);
  formUnitForPurch    = signal(true);
  formUnitForSale     = signal(true);
  formUnitSalePrice   = signal<number>(0);
  formUnitBarcode     = signal<string | null>(null);
  formUnitName        = signal<string | null>(null);
  formUnitBrand       = signal<string | null>(null);
  confirmDeleteUnitId = signal<number | null>(null);

  isUnitFormValid = computed(() =>
    this.formUnitSelectId() !== null && this.formUnitConvFactor() > 0,
  );

  // ── Formulario de grupo de opciones ──────────────────────────────
  showGroupForm        = signal(false);
  editingGroupId       = signal<number | null>(null);
  formGroupName        = signal('');
  formGroupRequired    = signal(true);
  formGroupMin         = signal(1);
  formGroupMax         = signal(1);
  formGroupAllowSplit  = signal(false);
  formGroupSortOrder   = signal(0);
  formGroupItems       = signal<ProductOptionItemRequest[]>([]);
  confirmDeleteGroupId = signal<number | null>(null);

  isGroupFormValid = computed(() =>
    this.formGroupName().trim().length > 0 &&
    this.formGroupItems().length > 0 &&
    this.formGroupMin() <= this.formGroupMax(),
  );

  // ── Formulario de slot de combo ───────────────────────────────────
  showSlotForm         = signal(false);
  editingSlotId        = signal<number | null>(null);
  formSlotName         = signal('');
  formSlotQuantity     = signal<number>(1);
  formSlotRequired     = signal(true);
  formSlotSortOrder    = signal(0);
  formSlotProducts     = signal<ProductComboSlotProductRequest[]>([]);
  confirmDeleteSlotId  = signal<number | null>(null);

  isSlotFormValid = computed(() =>
    this.formSlotName().trim().length > 0 &&
    this.formSlotProducts().length > 0,
  );

  // ── Categorías ────────────────────────────────────────────────────
  selectedCategoryId = signal<number | null>(null);

  private _pendingUnitProduct: number | null = null;
  private _pendingGroupProduct: number | null = null;
  private _pendingSlotCombo: number | null = null;

  constructor() {
    addIcons({
      addOutline, pencilOutline, trashOutline,
      chevronDownOutline, chevronForwardOutline,
      pricetagsOutline,
      warningOutline, closeOutline, saveOutline, refreshOutline,
      layersOutline, pricetagOutline, settingsOutline, cubeOutline,
    });
  }

  toggleExpand(product: ProductDto): void {
    const next = this.expandedId() === product.idProduct ? null : product.idProduct;
    this.expandedId.set(next);
    if (next !== null) {
      this.activeDetailTab.set('units');
      this.selectedCategoryId.set(null);
      this.showUnitForm.set(false);
      this.showGroupForm.set(false);
      this.showSlotForm.set(false);
      this.rowExpanded.emit(product);
    }
  }

  async handleRefresh(event: CustomEvent): Promise<void> {
    this.refresh.emit();
    await new Promise(r => setTimeout(r, 800));
    (event.target as HTMLIonRefresherElement).complete();
  }

  // ── Formulario producto ───────────────────────────────────────────
  openCreate(): void {
    this.formCode.set('');
    this.formName.set('');
    this.formProductTypeId.set(null);
    this.formUnitId.set(null);
    this.formProductParentId.set(null);
    this.formHasOptions.set(false);
    this.formIsCombo.set(false);
    this.editingId.set(null);
    this.showForm.set(true);
  }

  openEdit(product: ProductDto): void {
    this.formCode.set(product.codeProduct);
    this.formName.set(product.nameProduct);
    this.formProductTypeId.set(product.idProductType);
    this.formUnitId.set(product.idUnit);
    this.formProductParentId.set(product.idProductParent);
    this.formHasOptions.set(product.hasOptions);
    this.formIsCombo.set(product.isCombo);
    this.editingId.set(product.idProduct);
    this.expandedId.set(null);
    this.showForm.set(true);
  }

  cancelForm(): void {
    this.showForm.set(false);
    this.editingId.set(null);
  }

  submitForm(): void {
    const id  = this.editingId();
    const req: CreateProductRequest = {
      codeProduct:     this.formCode().trim(),
      nameProduct:     this.formName().trim(),
      idProductType:   this.formProductTypeId()!,
      idUnit:          this.formUnitId()!,
      idProductParent: this.formProductParentId(),
      hasOptions:      this.formHasOptions(),
      isCombo:         this.formIsCombo(),
    };
    if (id !== null) {
      this.editProduct.emit({ ...req, id });
    } else {
      this.createProduct.emit(req);
    }
    this.cancelForm();
  }

  askDelete(id: number): void  { this.confirmDeleteId.set(id); }
  cancelDelete(): void         { this.confirmDeleteId.set(null); }
  confirmDelete(): void {
    const id = this.confirmDeleteId();
    if (id !== null) { this.removeProduct.emit(id); this.confirmDeleteId.set(null); }
  }

  // ── Formulario presentación ───────────────────────────────────────
  openCreateUnit(idProduct: number): void {
    this.editingUnitId.set(null);
    this.formUnitSelectId.set(null);
    this.formUnitConvFactor.set(1);
    this.formUnitIsBase.set(false);
    this.formUnitForPurch.set(true);
    this.formUnitForSale.set(true);
    this.formUnitSalePrice.set(0);
    this.formUnitBarcode.set(null);
    this.formUnitName.set(null);
    this.formUnitBrand.set(null);
    this._pendingUnitProduct = idProduct;
    this.showUnitForm.set(true);
  }

  openEditUnit(unit: ProductUnitDto): void {
    this.editingUnitId.set(unit.idProductUnit);
    this.formUnitSelectId.set(unit.idUnit);
    this.formUnitConvFactor.set(unit.conversionFactor);
    this.formUnitIsBase.set(unit.isBase);
    this.formUnitForPurch.set(unit.usedForPurchase);
    this.formUnitForSale.set(unit.usedForSale);
    this.formUnitSalePrice.set(unit.salePrice);
    this.formUnitBarcode.set(unit.codeBarcode);
    this.formUnitName.set(unit.namePresentation);
    this.formUnitBrand.set(unit.brandPresentation);
    this._pendingUnitProduct = unit.idProduct;
    this.showUnitForm.set(true);
  }

  cancelUnitForm(): void { this.showUnitForm.set(false); this.editingUnitId.set(null); }

  submitUnitForm(): void {
    const id = this.editingUnitId();
    const req: CreateProductUnitRequest = {
      idProduct:         this._pendingUnitProduct!,
      idUnit:            this.formUnitSelectId()!,
      conversionFactor:  this.formUnitConvFactor(),
      isBase:            this.formUnitIsBase(),
      usedForPurchase:   this.formUnitForPurch(),
      usedForSale:       this.formUnitForSale(),
      salePrice:         this.formUnitSalePrice(),
      codeBarcode:       this.formUnitBarcode() || null,
      namePresentation:  this.formUnitName() || null,
      brandPresentation: this.formUnitBrand() || null,
    };
    if (id !== null) {
      const { idProduct: _p, idUnit: _u, ...updateReq } = req;
      this.editUnit.emit({ ...updateReq, id });
    } else {
      this.createUnit.emit(req);
    }
    this.cancelUnitForm();
  }

  askDeleteUnit(id: number): void    { this.confirmDeleteUnitId.set(id); }
  cancelDeleteUnit(): void           { this.confirmDeleteUnitId.set(null); }
  confirmDeleteUnit(): void {
    const id = this.confirmDeleteUnitId();
    if (id !== null) { this.removeUnit.emit(id); this.confirmDeleteUnitId.set(null); }
  }

  // ── Formulario grupo de opciones ──────────────────────────────────
  openCreateGroup(idProduct: number): void {
    this.editingGroupId.set(null);
    this.formGroupName.set('');
    this.formGroupRequired.set(true);
    this.formGroupMin.set(1);
    this.formGroupMax.set(1);
    this.formGroupAllowSplit.set(false);
    this.formGroupSortOrder.set(0);
    this.formGroupItems.set([{ nameItem: '', priceDelta: 0, isDefault: false, sortOrder: 0 }]);
    this._pendingGroupProduct = idProduct;
    this.showGroupForm.set(true);
  }

  openEditGroup(group: ProductOptionGroupDto): void {
    this.editingGroupId.set(group.idProductOptionGroup);
    this.formGroupName.set(group.nameGroup);
    this.formGroupRequired.set(group.isRequired);
    this.formGroupMin.set(group.minSelections);
    this.formGroupMax.set(group.maxSelections);
    this.formGroupAllowSplit.set(group.allowSplit);
    this.formGroupSortOrder.set(group.sortOrder);
    this.formGroupItems.set(group.items.map(i => ({
      nameItem: i.nameItem, priceDelta: i.priceDelta, isDefault: i.isDefault, sortOrder: i.sortOrder,
    })));
    this._pendingGroupProduct = group.idProduct;
    this.showGroupForm.set(true);
  }

  cancelGroupForm(): void { this.showGroupForm.set(false); this.editingGroupId.set(null); }

  submitGroupForm(): void {
    const id = this.editingGroupId();
    const base = {
      nameGroup:     this.formGroupName().trim(),
      isRequired:    this.formGroupRequired(),
      minSelections: this.formGroupRequired() ? this.formGroupMin() : 0,
      maxSelections: this.formGroupMax(),
      allowSplit:    this.formGroupAllowSplit(),
      sortOrder:     this.formGroupSortOrder(),
      items:         this.formGroupItems(),
    };
    if (id !== null) {
      this.editOptionGroup.emit({ ...base, id });
    } else {
      this.createOptionGroup.emit({ ...base, idProduct: this._pendingGroupProduct! });
    }
    this.cancelGroupForm();
  }

  addGroupItem(): void {
    this.formGroupItems.update(ls => [...ls, { nameItem: '', priceDelta: 0, isDefault: false, sortOrder: ls.length }]);
  }

  removeGroupItem(index: number): void {
    this.formGroupItems.update(ls => ls.filter((_, i) => i !== index));
  }

  updateGroupItem(index: number, field: keyof ProductOptionItemRequest, value: unknown): void {
    this.formGroupItems.update(ls => ls.map((item, i) => i === index ? { ...item, [field]: value } : item));
  }

  askDeleteGroup(id: number): void    { this.confirmDeleteGroupId.set(id); }
  cancelDeleteGroup(): void           { this.confirmDeleteGroupId.set(null); }
  confirmDeleteGroup(): void {
    const id = this.confirmDeleteGroupId();
    if (id !== null) { this.removeOptionGroup.emit(id); this.confirmDeleteGroupId.set(null); }
  }

  // ── Formulario slot de combo ──────────────────────────────────────
  openCreateSlot(idProductCombo: number): void {
    this.editingSlotId.set(null);
    this.formSlotName.set('');
    this.formSlotQuantity.set(1);
    this.formSlotRequired.set(true);
    this.formSlotSortOrder.set(0);
    this.formSlotProducts.set([{ idProduct: 0, priceAdjustment: 0, sortOrder: 0 }]);
    this._pendingSlotCombo = idProductCombo;
    this.showSlotForm.set(true);
  }

  openEditSlot(slot: ProductComboSlotDto): void {
    this.editingSlotId.set(slot.idProductComboSlot);
    this.formSlotName.set(slot.nameSlot);
    this.formSlotQuantity.set(Number(slot.quantity));
    this.formSlotRequired.set(slot.isRequired);
    this.formSlotSortOrder.set(slot.sortOrder);
    this.formSlotProducts.set(slot.products.map(p => ({
      idProduct: p.idProduct, priceAdjustment: p.priceAdjustment, sortOrder: p.sortOrder,
    })));
    this._pendingSlotCombo = slot.idProductCombo;
    this.showSlotForm.set(true);
  }

  cancelSlotForm(): void { this.showSlotForm.set(false); this.editingSlotId.set(null); }

  submitSlotForm(): void {
    const id = this.editingSlotId();
    const base = {
      nameSlot:   this.formSlotName().trim(),
      quantity:   this.formSlotQuantity(),
      isRequired: this.formSlotRequired(),
      sortOrder:  this.formSlotSortOrder(),
      products:   this.formSlotProducts(),
    };
    if (id !== null) {
      this.editComboSlot.emit({ ...base, id });
    } else {
      this.createComboSlot.emit({ ...base, idProductCombo: this._pendingSlotCombo! });
    }
    this.cancelSlotForm();
  }

  addSlotProduct(): void {
    this.formSlotProducts.update(ls => [...ls, { idProduct: 0, priceAdjustment: 0, sortOrder: ls.length }]);
  }

  removeSlotProduct(index: number): void {
    this.formSlotProducts.update(ls => ls.filter((_, i) => i !== index));
  }

  updateSlotProduct(index: number, field: keyof ProductComboSlotProductRequest, value: unknown): void {
    this.formSlotProducts.update(ls => ls.map((p, i) => i === index ? { ...p, [field]: value } : p));
  }

  askDeleteSlot(id: number): void    { this.confirmDeleteSlotId.set(id); }
  cancelDeleteSlot(): void           { this.confirmDeleteSlotId.set(null); }
  confirmDeleteSlot(): void {
    const id = this.confirmDeleteSlotId();
    if (id !== null) { this.removeComboSlot.emit(id); this.confirmDeleteSlotId.set(null); }
  }

  // ── Categorías ────────────────────────────────────────────────────
  handleAddCategory(idProduct: number): void {
    const idCat = this.selectedCategoryId();
    if (idCat !== null) {
      this.addCategory.emit({ idProduct, idProductCategory: idCat });
      this.selectedCategoryId.set(null);
    }
  }
}
