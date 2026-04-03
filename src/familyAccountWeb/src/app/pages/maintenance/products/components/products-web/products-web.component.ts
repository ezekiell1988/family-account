import {
  Component,
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  ViewChild,
  inject,
  input,
  output,
  signal,
  computed,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import {
  NgxDatatableModule,
  ColumnMode,
  DatatableRowDetailDirective,
} from '@swimlane/ngx-datatable';
import { PanelComponent } from '../../../../../components';
import {
  ProductDto,
  ProductSKUSummaryDto,
  ProductSKUDto,
  ProductCategoryDto,
  CreateProductRequest,
  UpdateProductRequest,
} from '../../../../../shared/models';

@Component({
  selector: 'app-products-web',
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: true,
  imports: [CommonModule, FormsModule, NgxDatatableModule, PanelComponent],
  templateUrl: './products-web.component.html',
})
export class ProductsWebComponent {
  // ── Inputs ────────────────────────────────────────────────────────
  products     = input<ProductDto[]>([]);
  skus         = input<ProductSKUDto[]>([]);
  categories   = input<ProductCategoryDto[]>([]);
  isLoading    = input(false);
  deletingId   = input<number | null>(null);
  errorMessage = input('');

  // ── Outputs ───────────────────────────────────────────────────────
  refresh        = output<void>();
  create         = output<CreateProductRequest>();
  editSave       = output<UpdateProductRequest & { id: number }>();
  remove         = output<number>();
  addSku         = output<{ idProduct: number; idProductSKU: number }>();
  removeSku      = output<{ idProduct: number; idProductSKU: number }>();
  addCategory    = output<{ idProduct: number; idProductCategory: number }>();
  removeCategory = output<{ idProduct: number; idProductCategory: number }>();
  clearError     = output<void>();

  // ── Row detail ────────────────────────────────────────────────────
  @ViewChild(DatatableRowDetailDirective) rowDetail!: DatatableRowDetailDirective;
  private cdr = inject(ChangeDetectorRef);
  expandedId  = signal<number | null>(null);

  // ── Filtros ───────────────────────────────────────────────────────
  filterSearch = signal('');

  filteredProducts = computed(() => {
    const search = this.filterSearch().toLowerCase().trim();
    if (!search) return this.products();
    return this.products().filter(p =>
      p.codeProduct.toLowerCase().includes(search) ||
      p.nameProduct.toLowerCase().includes(search),
    );
  });

  // ── Formulario crear/editar producto ─────────────────────────────
  showForm        = signal(false);
  editingId       = signal<number | null>(null);
  formCode        = signal('');
  formName        = signal('');
  confirmDeleteId = signal<number | null>(null);

  isEditing   = computed(() => this.editingId() !== null);
  formTitle   = computed(() => this.isEditing() ? 'Editar Producto' : 'Nuevo Producto');
  isFormValid = computed(() =>
    this.formCode().trim().length > 0 && this.formName().trim().length > 0,
  );

  // ── Gestión de asociaciones en row-detail ─────────────────────────
  selectedSkuId      = signal<number | null>(null);
  selectedCategoryId = signal<number | null>(null);

  // ── SKUs disponibles que aún no están asociados al producto expandido ──────
  availableSkus = computed(() => {
    const product = this.products().find(p => p.idProduct === this.expandedId());
    if (!product) return this.skus();
    const linked = new Set(product.skus.map(s => s.idProductSKU));
    return this.skus().filter(s => !linked.has(s.idProductSKU));
  });

  // ── Categorías disponibles que aún no están asociadas ────────────
  availableCategories = computed(() => {
    const product = this.products().find(p => p.idProduct === this.expandedId());
    if (!product) return this.categories();
    // ProductDto no incluye categorías directamente — todas las categorías son disponibles
    // (la lista real de categorías del producto no está en el DTO de lista)
    return this.categories();
  });

  ColumnMode = ColumnMode;

  // ── Toggle row detail ─────────────────────────────────────────────
  toggleRowDetail(row: ProductDto): void {
    const current = this.expandedId();
    if (current === row.idProduct) {
      this.rowDetail.collapseAllRows();
      this.expandedId.set(null);
    } else {
      this.rowDetail.collapseAllRows();
      this.expandedId.set(row.idProduct);
      this.rowDetail.toggleExpandRow(row);
      this.selectedSkuId.set(null);
      this.selectedCategoryId.set(null);
    }
    this.cdr.detectChanges();
  }

  onDetailToggle(event: { value: boolean }): void {
    if (!event.value) this.expandedId.set(null);
  }

  // ── Formulario producto ───────────────────────────────────────────
  openCreate(): void {
    this.formCode.set('');
    this.formName.set('');
    this.editingId.set(null);
    this.showForm.set(true);
  }

  openEdit(row: ProductDto): void {
    this.formCode.set(row.codeProduct);
    this.formName.set(row.nameProduct);
    this.editingId.set(row.idProduct);
    this.showForm.set(true);
  }

  cancelForm(): void {
    this.showForm.set(false);
    this.editingId.set(null);
  }

  submitForm(): void {
    const id = this.editingId();
    const req = { codeProduct: this.formCode().trim(), nameProduct: this.formName().trim() };
    if (id !== null) {
      this.editSave.emit({ ...req, id });
    } else {
      this.create.emit(req);
    }
    this.cancelForm();
  }

  // ── Confirmación de borrado ───────────────────────────────────────
  askDelete(id: number): void    { this.confirmDeleteId.set(id); }
  cancelDelete(): void           { this.confirmDeleteId.set(null); }
  confirmDelete(): void {
    const id = this.confirmDeleteId();
    if (id !== null) { this.remove.emit(id); this.confirmDeleteId.set(null); }
  }

  // ── Asociaciones SKUs ─────────────────────────────────────────────
  handleAddSku(idProduct: number): void {
    const idSku = this.selectedSkuId();
    if (idSku !== null) {
      this.addSku.emit({ idProduct, idProductSKU: idSku });
      this.selectedSkuId.set(null);
    }
  }

  handleRemoveSku(idProduct: number, sku: ProductSKUSummaryDto): void {
    this.removeSku.emit({ idProduct, idProductSKU: sku.idProductSKU });
  }

  // ── Asociaciones Categorías ───────────────────────────────────────
  handleAddCategory(idProduct: number): void {
    const idCat = this.selectedCategoryId();
    if (idCat !== null) {
      this.addCategory.emit({ idProduct, idProductCategory: idCat });
      this.selectedCategoryId.set(null);
    }
  }

  handleRemoveCategory(idProduct: number, idCat: number): void {
    this.removeCategory.emit({ idProduct, idProductCategory: idCat });
  }

  // ── Helpers ───────────────────────────────────────────────────────
  getSkuName(id: number): string {
    return this.skus().find(s => s.idProductSKU === id)?.nameProductSKU ?? '';
  }

  getCategoryName(id: number): string {
    return this.categories().find(c => c.idProductCategory === id)?.nameProductCategory ?? '';
  }
}
