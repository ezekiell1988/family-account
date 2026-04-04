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
  ProductTypeService,
  UnitOfMeasureService,
  ProductCategoryService,
  AccountService,
  CostCenterService,
  LoggerService,
} from '../../../service';
import { ResponsiveComponent } from '../../../shared';
import {
  CreateProductRequest,
  UpdateProductRequest,
} from '../../../shared/models';
import {
  ProductsWebComponent,
  ProductsMobileComponent,
} from './components';

@Component({
  selector: 'app-products',
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: true,
  imports: [
    ProductsWebComponent,
    ProductsMobileComponent,
  ],
  templateUrl: './products.html',
})
export class ProductsPage extends ResponsiveComponent implements OnInit, OnDestroy {
  private readonly productSvc    = inject(ProductService);
  private readonly productTypeSvc = inject(ProductTypeService);
  private readonly unitSvc       = inject(UnitOfMeasureService);
  private readonly categorySvc   = inject(ProductCategoryService);
  private readonly accountSvc    = inject(AccountService);
  private readonly costCenterSvc = inject(CostCenterService);
  private readonly logger        = inject(LoggerService).getLogger('ProductsPage');

  // ── Estado expuesto ────────────────────────────────────────────────────────
  products        = this.productSvc.items;
  productsLoading = this.productSvc.isLoading;
  productsError   = this.productSvc.error;

  productTypes        = this.productTypeSvc.items;
  productTypesLoading = this.productTypeSvc.isLoading;

  units        = this.unitSvc.items;
  unitsLoading = this.unitSvc.isLoading;

  categories = this.categorySvc.items;

  accounts    = this.accountSvc.accounts;
  costCenters = this.costCenterSvc.items;

  // ── Estado local ──────────────────────────────────────────────────────────
  deletingProductId = signal<number | null>(null);

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
    if (this.productTypes().length === 0) {
      this.productTypeSvc.loadList().subscribe({
        next: () => this.logger.success('✅ Tipos de producto cargados'),
        error: (e) => this.logger.error('❌ Error al cargar tipos de producto:', e),
      });
    }
    if (this.units().length === 0) {
      this.unitSvc.loadList().subscribe({
        next: () => this.logger.success('✅ Unidades de medida cargadas'),
        error: (e) => this.logger.error('❌ Error al cargar unidades:', e),
      });
    }
    if (this.categories().length === 0) {
      this.categorySvc.loadList().subscribe({
        next: () => this.logger.success('✅ Categorías cargadas'),
        error: (e) => this.logger.error('❌ Error al cargar categorías:', e),
      });
    }
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

  // ── Métodos de soporte (template) ─────────────────────────────────────────
  clearProductError(): void { this.productSvc.clearError(); }
}
