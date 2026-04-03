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
  ProductCategoryDto,
  CreateProductCategoryRequest,
  UpdateProductCategoryRequest,
} from '../../../../../shared/models';

@Component({
  selector: 'app-products-categories-web',
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: true,
  imports: [CommonModule, FormsModule, NgxDatatableModule, PanelComponent],
  templateUrl: './product-categories-web.component.html',
})
export class ProductsCategoriesWebComponent {
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

  // ── Filtros ───────────────────────────────────────────────────────
  filterSearch = signal('');

  filteredCategories = computed(() => {
    const search = this.filterSearch().toLowerCase().trim();
    if (!search) return this.categories();
    return this.categories().filter(c =>
      c.nameProductCategory.toLowerCase().includes(search),
    );
  });

  // ── Formulario ────────────────────────────────────────────────────
  showForm        = signal(false);
  editingId       = signal<number | null>(null);
  formName        = signal('');
  confirmDeleteId = signal<number | null>(null);

  isEditing   = computed(() => this.editingId() !== null);
  formTitle   = computed(() => this.isEditing() ? 'Editar Categoría' : 'Nueva Categoría');
  isFormValid = computed(() => this.formName().trim().length > 0);

  ColumnMode = ColumnMode;

  openCreate(): void {
    this.formName.set('');
    this.editingId.set(null);
    this.showForm.set(true);
  }

  openEdit(row: ProductCategoryDto): void {
    this.formName.set(row.nameProductCategory);
    this.editingId.set(row.idProductCategory);
    this.showForm.set(true);
  }

  cancelForm(): void {
    this.showForm.set(false);
    this.editingId.set(null);
  }

  submitForm(): void {
    const req = { nameProductCategory: this.formName().trim() };
    const id  = this.editingId();
    if (id !== null) {
      this.editSave.emit({ ...req, id });
    } else {
      this.create.emit(req);
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
