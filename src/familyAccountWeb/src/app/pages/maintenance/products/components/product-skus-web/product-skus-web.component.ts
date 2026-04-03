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
import { NgxDatatableModule, ColumnMode } from '@swimlane/ngx-datatable';
import { PanelComponent } from '../../../../../components';
import {
  ProductSKUDto,
  CreateProductSKURequest,
  UpdateProductSKURequest,
} from '../../../../../shared/models';

@Component({
  selector: 'app-products-skus-web',
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: true,
  imports: [CommonModule, FormsModule, NgxDatatableModule, PanelComponent],
  templateUrl: './product-skus-web.component.html',
})
export class ProductsSkusWebComponent {
  // ── Inputs ────────────────────────────────────────────────────────
  skus         = input<ProductSKUDto[]>([]);
  isLoading    = input(false);
  deletingId   = input<number | null>(null);
  errorMessage = input('');

  // ── Outputs ───────────────────────────────────────────────────────
  refresh    = output<void>();
  create     = output<CreateProductSKURequest>();
  editSave   = output<UpdateProductSKURequest & { id: number }>();
  remove     = output<number>();
  clearError = output<void>();

  // ── Filtros ───────────────────────────────────────────────────────
  filterSearch = signal('');

  filteredSkus = computed(() => {
    const search = this.filterSearch().toLowerCase().trim();
    if (!search) return this.skus();
    return this.skus().filter(s =>
      s.codeProductSKU.toLowerCase().includes(search) ||
      s.nameProductSKU.toLowerCase().includes(search) ||
      (s.brandProductSKU ?? '').toLowerCase().includes(search),
    );
  });

  // ── Formulario ────────────────────────────────────────────────────
  showForm        = signal(false);
  editingId       = signal<number | null>(null);
  formCode        = signal('');
  formName        = signal('');
  formBrand       = signal('');
  formDescription = signal('');
  formNetContent  = signal('');
  confirmDeleteId = signal<number | null>(null);

  isEditing   = computed(() => this.editingId() !== null);
  formTitle   = computed(() => this.isEditing() ? 'Editar SKU' : 'Nuevo SKU');
  isFormValid = computed(() =>
    this.formCode().trim().length > 0 && this.formName().trim().length > 0,
  );

  ColumnMode = ColumnMode;

  // ── Formulario ───────────────────────────────────────────────────
  openCreate(): void {
    this.formCode.set('');
    this.formName.set('');
    this.formBrand.set('');
    this.formDescription.set('');
    this.formNetContent.set('');
    this.editingId.set(null);
    this.showForm.set(true);
  }

  openEdit(row: ProductSKUDto): void {
    this.formCode.set(row.codeProductSKU);
    this.formName.set(row.nameProductSKU);
    this.formBrand.set(row.brandProductSKU ?? '');
    this.formDescription.set(row.descriptionProductSKU ?? '');
    this.formNetContent.set(row.netContent ?? '');
    this.editingId.set(row.idProductSKU);
    this.showForm.set(true);
  }

  cancelForm(): void {
    this.showForm.set(false);
    this.editingId.set(null);
  }

  submitForm(): void {
    const base = {
      codeProductSKU:        this.formCode().trim(),
      nameProductSKU:        this.formName().trim(),
      brandProductSKU:       this.formBrand().trim() || null,
      descriptionProductSKU: this.formDescription().trim() || null,
      netContent:            this.formNetContent().trim() || null,
    };
    const id = this.editingId();
    if (id !== null) {
      this.editSave.emit({ ...base, id });
    } else {
      this.create.emit(base);
    }
    this.cancelForm();
  }

  askDelete(id: number): void    { this.confirmDeleteId.set(id); }
  cancelDelete(): void           { this.confirmDeleteId.set(null); }
  confirmDelete(): void {
    const id = this.confirmDeleteId();
    if (id !== null) { this.remove.emit(id); this.confirmDeleteId.set(null); }
  }
}
