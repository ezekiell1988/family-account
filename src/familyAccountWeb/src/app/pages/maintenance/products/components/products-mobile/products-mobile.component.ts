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
} from '@ionic/angular/standalone';
import { HeaderComponent, FooterComponent } from '../../../../../components';
import {
  ProductDto,
  ProductTypeDto,
  UnitOfMeasureDto,
  ProductUnitDto,
  ProductOptionGroupDto,
  ProductComboSlotDto,
  CreateProductRequest,
  UpdateProductRequest,
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
  ],
  templateUrl: './products-mobile.component.html',
})
export class ProductsMobileComponent {
  // ── Inputs ────────────────────────────────────────────────────────
  products          = input<ProductDto[]>([]);
  productTypes      = input<ProductTypeDto[]>([]);
  units             = input<UnitOfMeasureDto[]>([]);
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

  // ── Estado de UI ──────────────────────────────────────────────────
  expandedId         = signal<number | null>(null);
  showForm           = signal(false);
  editingId          = signal<number | null>(null);
  formCode           = signal('');
  formName           = signal('');
  formProductTypeId  = signal<number | null>(null);
  formUnitId         = signal<number | null>(null);
  formProductParentId = signal<number | null>(null);
  formHasOptions     = signal(false);
  formIsCombo        = signal(false);
  confirmDeleteId    = signal<number | null>(null);

  isEditing   = computed(() => this.editingId() !== null);
  isFormValid = computed(() =>
    this.formCode().trim().length > 0 &&
    this.formName().trim().length > 0 &&
    this.formProductTypeId() !== null &&
    this.formUnitId() !== null,
  );

  constructor() {
    addIcons({
      addOutline, pencilOutline, trashOutline,
      chevronDownOutline, chevronForwardOutline,
      pricetagsOutline,
      warningOutline, closeOutline, saveOutline, refreshOutline,
    });
  }

  toggleExpand(product: ProductDto): void {
    const next = this.expandedId() === product.idProduct ? null : product.idProduct;
    this.expandedId.set(next);
    if (next !== null) {
      this.rowExpanded.emit(product);
    }
  }

  async handleRefresh(event: CustomEvent): Promise<void> {
    this.refresh.emit();
    await new Promise(r => setTimeout(r, 800));
    (event.target as HTMLIonRefresherElement).complete();
  }

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
}
