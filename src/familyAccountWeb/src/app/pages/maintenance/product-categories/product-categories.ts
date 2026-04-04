import {
  Component,
  OnInit,
  OnDestroy,
  inject,
  signal,
  ChangeDetectionStrategy,
} from '@angular/core';
import {
  AppSettings,
  ProductCategoryService,
  LoggerService,
} from '../../../service';
import { ResponsiveComponent } from '../../../shared';
import {
  CreateProductCategoryRequest,
  UpdateProductCategoryRequest,
} from '../../../shared/models';
import {
  ProductCategoriesWebComponent,
  ProductCategoriesMobileComponent,
} from './components';

@Component({
  selector: 'app-product-categories',
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: true,
  imports: [ProductCategoriesWebComponent, ProductCategoriesMobileComponent],
  templateUrl: './product-categories.html',
})
export class ProductCategoriesPage
  extends ResponsiveComponent
  implements OnInit, OnDestroy
{
  private readonly categorySvc = inject(ProductCategoryService);
  private readonly logger = inject(LoggerService).getLogger(
    'ProductCategoriesPage'
  );

  // ── Estado expuesto ───────────────────────────────────────────────
  categories = this.categorySvc.items;
  categoriesLoading = this.categorySvc.isLoading;
  categoriesError = this.categorySvc.error;

  // ── Estado local ──────────────────────────────────────────────────
  deletingCategoryId = signal<number | null>(null);

  constructor(public appSettings: AppSettings) {
    super();
  }

  override ngOnDestroy(): void {
    super.ngOnDestroy();
  }

  ngOnInit(): void {
    this.logger.info('🚀 Cargando Categorías de Productos');
    this.load();
  }

  load(): void {
    this.categorySvc.loadList().subscribe({
      next: () => this.logger.success('✅ Categorías cargadas'),
      error: (e) => this.logger.error('❌ Error al cargar categorías:', e),
    });
  }

  createCategory(req: CreateProductCategoryRequest): void {
    this.categorySvc.create(req).subscribe({
      next: () => this.logger.success('✅ Categoría creada'),
      error: (e) => this.logger.error('❌ Error al crear categoría:', e),
    });
  }

  updateCategory(payload: UpdateProductCategoryRequest & { id: number }): void {
    const { id, ...req } = payload;
    this.categorySvc.update(id, req).subscribe({
      next: () => this.logger.success('✅ Categoría actualizada'),
      error: (e) => this.logger.error('❌ Error al actualizar categoría:', e),
    });
  }

  deleteCategory(id: number): void {
    this.deletingCategoryId.set(id);
    this.categorySvc.delete(id).subscribe({
      next: () => this.deletingCategoryId.set(null),
      error: () => this.deletingCategoryId.set(null),
    });
  }

  clearCategoryError(): void {
    this.categorySvc.clearError();
  }
}
