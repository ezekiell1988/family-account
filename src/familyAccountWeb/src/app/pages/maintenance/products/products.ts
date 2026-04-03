import {
  Component,
  OnInit,
  OnDestroy,
  inject,
  signal,
  ChangeDetectionStrategy,
} from '@angular/core';
import { finalize } from 'rxjs/operators';
import {
  AppSettings,
  ProductService,
  ProductSKUService,
  ProductCategoryService,
  AccountService,
  CostCenterService,
  LoggerService,
} from '../../../service';
import { ResponsiveComponent } from '../../../shared';
import {
  CreateProductRequest,
  UpdateProductRequest,
  CreateProductSKURequest,
  UpdateProductSKURequest,
  CreateProductCategoryRequest,
  UpdateProductCategoryRequest,
} from '../../../shared/models';
import {
  ProductsWebComponent,
  ProductsSkusWebComponent,
  ProductsCategoriesWebComponent,
  ProductsMobileComponent,
} from './components';

@Component({
  selector: 'app-products',
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: true,
  imports: [
    ProductsWebComponent,
    ProductsSkusWebComponent,
    ProductsCategoriesWebComponent,
    ProductsMobileComponent,
  ],
  templateUrl: './products.html',
})
export class ProductsPage extends ResponsiveComponent implements OnInit, OnDestroy {
  private readonly productSvc  = inject(ProductService);
  private readonly skuSvc      = inject(ProductSKUService);
  private readonly categorySvc = inject(ProductCategoryService);
  private readonly accountSvc  = inject(AccountService);
  private readonly costCenterSvc = inject(CostCenterService);
  private readonly logger      = inject(LoggerService).getLogger('ProductsPage');

  // ── Estado expuesto ────────────────────────────────────────────────────────
  products         = this.productSvc.items;
  productsLoading  = this.productSvc.isLoading;
  productsError    = this.productSvc.error;

  skus             = this.skuSvc.items;
  skusLoading      = this.skuSvc.isLoading;
  skusError        = this.skuSvc.error;

  categories       = this.categorySvc.items;
  categoriesLoading = this.categorySvc.isLoading;
  categoriesError  = this.categorySvc.error;

  accounts         = this.accountSvc.accounts;
  costCenters      = this.costCenterSvc.items;

  // ── Estado local ──────────────────────────────────────────────────────────
  deletingProductId  = signal<number | null>(null);
  deletingSkuId      = signal<number | null>(null);
  deletingCategoryId = signal<number | null>(null);

  constructor(public appSettings: AppSettings) {
    super();
  }

  override ngOnDestroy(): void {
    super.ngOnDestroy();
  }

  ngOnInit(): void {
    this.logger.info('🚀 Cargando Productos');
    this.loadAll();
  }

  loadAll(): void {
    this.productSvc.loadList()
      .pipe(finalize(() => {}))
      .subscribe({
        next: () => this.logger.success('✅ Productos cargados'),
        error: (e) => this.logger.error('❌ Error al cargar productos:', e),
      });
    this.skuSvc.loadList().subscribe({
      next: () => this.logger.success('✅ SKUs cargados'),
      error: (e) => this.logger.error('❌ Error al cargar SKUs:', e),
    });
    this.categorySvc.loadList().subscribe({
      next: () => this.logger.success('✅ Categorías cargadas'),
      error: (e) => this.logger.error('❌ Error al cargar categorías:', e),
    });
    if (this.accounts().length === 0) {
      this.accountSvc.loadList().subscribe();
    }
    if (this.costCenters().length === 0) {
      this.costCenterSvc.loadList().subscribe();
    }
  }

  // ── Acciones de Productos ─────────────────────────────────────────────────
  createProduct(req: CreateProductRequest): void {
    this.productSvc.create(req).subscribe({
      next: () => this.logger.success('✅ Producto creado'),
      error: (e) => this.logger.error('❌ Error al crear producto:', e),
    });
  }

  updateProduct(payload: UpdateProductRequest & { id: number }): void {
    const { id, ...req } = payload;
    this.productSvc.update(id, req).subscribe({
      next: () => this.logger.success('✅ Producto actualizado'),
      error: (e) => this.logger.error('❌ Error al actualizar producto:', e),
    });
  }

  deleteProduct(id: number): void {
    this.deletingProductId.set(id);
    this.productSvc.delete(id).subscribe({
      next: () => this.deletingProductId.set(null),
      error: () => this.deletingProductId.set(null),
    });
  }

  addSkuToProduct(payload: { idProduct: number; idProductSKU: number }): void {
    this.productSvc.addSKU(payload.idProduct, payload.idProductSKU).subscribe({
      next: () => {
        this.logger.success('✅ SKU asociado');
        this.productSvc.loadList().subscribe();
      },
      error: (e) => this.logger.error('❌ Error al asociar SKU:', e),
    });
  }

  removeSkuFromProduct(payload: { idProduct: number; idProductSKU: number }): void {
    this.productSvc.removeSKU(payload.idProduct, payload.idProductSKU).subscribe({
      next: () => {
        this.logger.success('✅ SKU desasociado');
        this.productSvc.loadList().subscribe();
      },
      error: (e) => this.logger.error('❌ Error al desasociar SKU:', e),
    });
  }

  addCategoryToProduct(payload: { idProduct: number; idProductCategory: number }): void {
    this.categorySvc.addToProduct(payload.idProductCategory, payload.idProduct).subscribe({
      next: () => {
        this.logger.success('✅ Categoría asociada');
        this.productSvc.loadList().subscribe();
      },
      error: (e) => this.logger.error('❌ Error al asociar categoría:', e),
    });
  }

  removeCategoryFromProduct(payload: { idProduct: number; idProductCategory: number }): void {
    this.categorySvc.removeFromProduct(payload.idProductCategory, payload.idProduct).subscribe({
      next: () => {
        this.logger.success('✅ Categoría desasociada');
        this.productSvc.loadList().subscribe();
      },
      error: (e) => this.logger.error('❌ Error al desasociar categoría:', e),
    });
  }

  // ── Acciones de SKUs ──────────────────────────────────────────────────────
  createSku(req: CreateProductSKURequest): void {
    this.skuSvc.create(req).subscribe({
      next: () => this.logger.success('✅ SKU creado'),
      error: (e) => this.logger.error('❌ Error al crear SKU:', e),
    });
  }

  updateSku(payload: UpdateProductSKURequest & { id: number }): void {
    const { id, ...req } = payload;
    this.skuSvc.update(id, req).subscribe({
      next: () => this.logger.success('✅ SKU actualizado'),
      error: (e) => this.logger.error('❌ Error al actualizar SKU:', e),
    });
  }

  deleteSku(id: number): void {
    this.deletingSkuId.set(id);
    this.skuSvc.delete(id).subscribe({
      next: () => this.deletingSkuId.set(null),
      error: () => this.deletingSkuId.set(null),
    });
  }

  // ── Acciones de Categorías ────────────────────────────────────────────────
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

  // ── Métodos de soporte (template) ─────────────────────────────────────────
  clearProductError(): void  { this.productSvc.clearError(); }
  clearSkuError(): void      { this.skuSvc.clearError(); }
  clearCategoryError(): void { this.categorySvc.clearError(); }
  reloadSkus(): void         { this.skuSvc.loadList().subscribe(); }
  reloadCategories(): void   { this.categorySvc.loadList().subscribe(); }
}
