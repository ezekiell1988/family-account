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
  ProductTypeDto,
  UnitOfMeasureDto,
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
  productTypes = input<ProductTypeDto[]>([]);
  units        = input<UnitOfMeasureDto[]>([]);
  categories   = input<ProductCategoryDto[]>([]);
  isLoading    = input(false);
  deletingId   = input<number | null>(null);
  errorMessage = input('');

  // ── Outputs ───────────────────────────────────────────────────────
  refresh        = output<void>();
  create         = output<CreateProductRequest>();
  editSave       = output<UpdateProductRequest & { id: number }>();
  remove         = output<number>();
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
  showForm           = signal(false);
  editingId          = signal<number | null>(null);
  formCode           = signal('');
  formName           = signal('');
  formProductTypeId  = signal<number | null>(null);
  formUnitId         = signal<number | null>(null);
  formProductParentId = signal<number | null>(null);
  confirmDeleteId    = signal<number | null>(null);

  isEditing   = computed(() => this.editingId() !== null);
  formTitle   = computed(() => this.isEditing() ? 'Editar Producto' : 'Nuevo Producto');
  isFormValid = computed(() =>
    this.formCode().trim().length > 0 &&
    this.formName().trim().length > 0 &&
    this.formProductTypeId() !== null &&
    this.formUnitId() !== null,
  );

  // ── Gestión de asociaciones en row-detail ─────────────────────────
  selectedCategoryId = signal<number | null>(null);

  // ── Categorías disponibles que aún no están asociadas ────────────
  availableCategories = computed(() => this.categories());

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
    this.formProductTypeId.set(null);
    this.formUnitId.set(null);
    this.formProductParentId.set(null);
    this.editingId.set(null);
    this.showForm.set(true);
  }

  openEdit(row: ProductDto): void {
    this.formCode.set(row.codeProduct);
    this.formName.set(row.nameProduct);
    this.formProductTypeId.set(row.idProductType);
    this.formUnitId.set(row.idUnit);
    this.formProductParentId.set(row.idProductParent);
    this.editingId.set(row.idProduct);
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
    };
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
}

