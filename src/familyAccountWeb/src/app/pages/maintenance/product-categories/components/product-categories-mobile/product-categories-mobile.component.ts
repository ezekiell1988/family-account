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
import { TranslatePipe } from '@ngx-translate/core';
import { addIcons } from 'ionicons';
import {
  addOutline,
  pencilOutline,
  trashOutline,
  chevronDownOutline,
  chevronForwardOutline,
  pricetagOutline,
  warningOutline,
  closeOutline,
  saveOutline,
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
  ProductCategoryDto,
  CreateProductCategoryRequest,
  UpdateProductCategoryRequest,
} from '../../../../../shared/models';

@Component({
  selector: 'app-product-categories-mobile',
  host: { class: 'ion-page' },
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    TranslatePipe,
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
    IonButton,
    IonIcon,
    IonInput,
    IonGrid,
    IonRow,
    IonCol,
    IonFab,
    IonFabButton,
  ],
  templateUrl: './product-categories-mobile.component.html',
})
export class ProductCategoriesMobileComponent {
  // ── Inputs ────────────────────────────────────────────────────────
  categories   = input<ProductCategoryDto[]>([]);
  isLoading    = input(false);
  deletingId   = input<number | null>(null);
  errorMessage = input('');

  // ── Outputs ───────────────────────────────────────────────────────
  refresh    = output<void>();
  create     = output<CreateProductCategoryRequest>();
  editSave   = output<UpdateProductCategoryRequest & { id: number }>();
  remove     = output<number>();
  clearError = output<void>();

  // ── Estado de UI ──────────────────────────────────────────────────
  expandedId      = signal<number | null>(null);
  showForm        = signal(false);
  editingId       = signal<number | null>(null);
  formName        = signal('');
  confirmDeleteId = signal<number | null>(null);

  isEditing   = computed(() => this.editingId() !== null);
  isFormValid = computed(() => this.formName().trim().length > 0);

  constructor() {
    addIcons({
      addOutline,
      pencilOutline,
      trashOutline,
      chevronDownOutline,
      chevronForwardOutline,
      pricetagOutline,
      warningOutline,
      closeOutline,
      saveOutline,
    });
  }

  toggleExpand(id: number): void {
    this.expandedId.update((v) => (v === id ? null : id));
  }

  async handleRefresh(event: CustomEvent): Promise<void> {
    this.refresh.emit();
    await new Promise((r) => setTimeout(r, 800));
    (event.target as HTMLIonRefresherElement).complete();
  }

  openCreate(): void {
    this.formName.set('');
    this.editingId.set(null);
    this.showForm.set(true);
  }

  openEdit(category: ProductCategoryDto): void {
    this.formName.set(category.nameProductCategory);
    this.editingId.set(category.idProductCategory);
    this.expandedId.set(null);
    this.showForm.set(true);
  }

  cancelForm(): void {
    this.showForm.set(false);
    this.editingId.set(null);
  }

  submitForm(): void {
    const id  = this.editingId();
    const req: CreateProductCategoryRequest = {
      nameProductCategory: this.formName().trim(),
    };
    if (id !== null) {
      this.editSave.emit({ ...req, id });
    } else {
      this.create.emit(req);
    }
    this.cancelForm();
  }

  askDelete(id: number): void  { this.confirmDeleteId.set(id); }
  cancelDelete(): void         { this.confirmDeleteId.set(null); }
  confirmDelete(): void {
    const id = this.confirmDeleteId();
    if (id !== null) { this.remove.emit(id); this.confirmDeleteId.set(null); }
  }
}
