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
  barcodeOutline,
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
  IonCard,
  IonCardHeader,
  IonCardTitle,
  IonCardContent,
  IonBadge,
  IonButton,
  IonIcon,
  IonInput,
  IonGrid,
  IonRow,
  IonCol,
  IonFab,
  IonFabButton,
} from '@ionic/angular/standalone';
import { HeaderComponent, FooterComponent } from '../../../../../components';
import {
  ProductDto,
  ProductSKUDto,
  ProductCategoryDto,
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
    IonCard,
    IonCardHeader,
    IonCardTitle,
    IonCardContent,
    IonBadge,
    IonButton,
    IonIcon,
    IonInput,
    IonGrid,
    IonRow,
    IonCol,
    IonFab,
    IonFabButton,
  ],
  templateUrl: './products-mobile.component.html',
})
export class ProductsMobileComponent {
  // ── Inputs ────────────────────────────────────────────────────────
  products          = input<ProductDto[]>([]);
  skus              = input<ProductSKUDto[]>([]);
  categories        = input<ProductCategoryDto[]>([]);
  isLoading         = input(false);
  errorMessage      = input('');
  deletingProductId = input<number | null>(null);

  // ── Outputs ───────────────────────────────────────────────────────
  refresh       = output<void>();
  createProduct = output<CreateProductRequest>();
  editProduct   = output<UpdateProductRequest & { id: number }>();
  removeProduct = output<number>();
  clearError    = output<void>();

  // ── Estado de UI ──────────────────────────────────────────────────
  expandedId      = signal<number | null>(null);
  showForm        = signal(false);
  editingId       = signal<number | null>(null);
  formCode        = signal('');
  formName        = signal('');
  confirmDeleteId = signal<number | null>(null);

  isEditing   = computed(() => this.editingId() !== null);
  isFormValid = computed(() =>
    this.formCode().trim().length > 0 && this.formName().trim().length > 0,
  );

  constructor() {
    addIcons({
      addOutline, pencilOutline, trashOutline,
      chevronDownOutline, chevronForwardOutline,
      barcodeOutline, pricetagsOutline,
      warningOutline, closeOutline, saveOutline, refreshOutline,
    });
  }

  toggleExpand(id: number): void {
    this.expandedId.update(v => v === id ? null : id);
  }

  async handleRefresh(event: CustomEvent): Promise<void> {
    this.refresh.emit();
    await new Promise(r => setTimeout(r, 800));
    (event.target as HTMLIonRefresherElement).complete();
  }

  openCreate(): void {
    this.formCode.set('');
    this.formName.set('');
    this.editingId.set(null);
    this.showForm.set(true);
  }

  openEdit(product: ProductDto): void {
    this.formCode.set(product.codeProduct);
    this.formName.set(product.nameProduct);
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
    const req = { codeProduct: this.formCode().trim(), nameProduct: this.formName().trim() };
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

  getSkuSummary(product: ProductDto): string {
    if (product.skus.length === 0) return 'Sin SKUs';
    return product.skus.slice(0, 2).map(s => s.codeProductSKU).join(', ')
      + (product.skus.length > 2 ? ` +${product.skus.length - 2}` : '');
  }
}
