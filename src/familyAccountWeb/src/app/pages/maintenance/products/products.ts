import {
  Component,
  OnInit,
  OnDestroy,
  inject,
  signal,
  ChangeDetectionStrategy,
} from '@angular/core';
import { forkJoin } from 'rxjs';
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
  ProductUnitService,
  ProductOptionGroupService,
  ProductComboSlotService,
} from '../../../service';
import { ResponsiveComponent } from '../../../shared';
import {
  ProductDto,
  CreateProductRequest,
  UpdateProductRequest,
  ProductUnitDto,
  ProductOptionGroupDto,
  ProductComboSlotDto,
  CreateProductUnitRequest,
  UpdateProductUnitRequest,
  CreateProductOptionGroupRequest,
  UpdateProductOptionGroupRequest,
  CreateProductComboSlotRequest,
  UpdateProductComboSlotRequest,
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
  private readonly productUnitSvc        = inject(ProductUnitService);
  private readonly productOptionGroupSvc = inject(ProductOptionGroupService);
  private readonly productComboSlotSvc   = inject(ProductComboSlotService);

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

  // ── Estado de detalle expandido ───────────────────────────────────────────
  expandedProductUnits        = signal<ProductUnitDto[]>([]);
  expandedProductOptionGroups = signal<ProductOptionGroupDto[]>([]);
  expandedProductComboSlots   = signal<ProductComboSlotDto[]>([]);
  loadingDetail               = signal(false);

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

  // ── Carga lazy de detalle ─────────────────────────────────────────────────
  loadDetailFor(product: ProductDto): void {
    this.loadingDetail.set(true);
    this.expandedProductUnits.set([]);
    this.expandedProductOptionGroups.set([]);
    this.expandedProductComboSlots.set([]);

    const requests: ReturnType<typeof forkJoin> extends never ? never : any[] = [
      this.productUnitSvc.getByProduct(product.idProduct),
    ];
    if (product.hasOptions) {
      requests.push(this.productOptionGroupSvc.getByProduct(product.idProduct));
    }
    if (product.isCombo) {
      requests.push(this.productComboSlotSvc.getByCombo(product.idProduct));
    }

    forkJoin(requests)
      .pipe(finalize(() => this.loadingDetail.set(false)))
      .subscribe({
        next: (results: any[]) => {
          this.expandedProductUnits.set(results[0] ?? []);
          if (product.hasOptions) this.expandedProductOptionGroups.set(results[1] ?? []);
          if (product.isCombo)    this.expandedProductComboSlots.set(results[product.hasOptions ? 2 : 1] ?? []);
        },
        error: (e) => this.logger.error('❌ Error al cargar detalle:', e),
      });
  }

  // ── CRUD presentaciones ───────────────────────────────────────────────────
  createProductUnit(req: CreateProductUnitRequest): void {
    this.productUnitSvc.create(req).subscribe({
      next: (unit) => {
        this.expandedProductUnits.update(ls => [...ls, unit]);
        this.logger.success('✅ Presentación creada');
      },
      error: (e) => this.logger.error('❌ Error al crear presentación:', e),
    });
  }

  updateProductUnit(payload: UpdateProductUnitRequest & { id: number }): void {
    const { id, ...req } = payload;
    this.productUnitSvc.update(id, req).subscribe({
      next: (unit) => {
        this.expandedProductUnits.update(ls => ls.map(u => u.idProductUnit === id ? unit : u));
        this.logger.success('✅ Presentación actualizada');
      },
      error: (e) => this.logger.error('❌ Error al actualizar presentación:', e),
    });
  }

  deleteProductUnit(id: number): void {
    this.productUnitSvc.delete(id).subscribe({
      next: () => this.expandedProductUnits.update(ls => ls.filter(u => u.idProductUnit !== id)),
      error: (e) => this.logger.error('❌ Error al eliminar presentación:', e),
    });
  }

  // ── CRUD grupos de opciones ───────────────────────────────────────────────
  createOptionGroup(req: CreateProductOptionGroupRequest): void {
    this.productOptionGroupSvc.create(req).subscribe({
      next: (group) => {
        this.expandedProductOptionGroups.update(ls => [...ls, group]);
        this.logger.success('✅ Grupo de opciones creado');
      },
      error: (e) => this.logger.error('❌ Error al crear grupo de opciones:', e),
    });
  }

  updateOptionGroup(payload: UpdateProductOptionGroupRequest & { id: number }): void {
    const { id, ...req } = payload;
    this.productOptionGroupSvc.update(id, req).subscribe({
      next: (group) => {
        this.expandedProductOptionGroups.update(ls => ls.map(g => g.idProductOptionGroup === id ? group : g));
        this.logger.success('✅ Grupo de opciones actualizado');
      },
      error: (e) => this.logger.error('❌ Error al actualizar grupo de opciones:', e),
    });
  }

  deleteOptionGroup(id: number): void {
    this.productOptionGroupSvc.delete(id).subscribe({
      next: () => this.expandedProductOptionGroups.update(ls => ls.filter(g => g.idProductOptionGroup !== id)),
      error: (e) => this.logger.error('❌ Error al eliminar grupo de opciones:', e),
    });
  }

  // ── CRUD slots de combo ───────────────────────────────────────────────────
  createComboSlot(req: CreateProductComboSlotRequest): void {
    this.productComboSlotSvc.create(req).subscribe({
      next: (slot) => {
        this.expandedProductComboSlots.update(ls => [...ls, slot]);
        this.logger.success('✅ Slot de combo creado');
      },
      error: (e) => this.logger.error('❌ Error al crear slot de combo:', e),
    });
  }

  updateComboSlot(payload: UpdateProductComboSlotRequest & { id: number }): void {
    const { id, ...req } = payload;
    this.productComboSlotSvc.update(id, req).subscribe({
      next: (slot) => {
        this.expandedProductComboSlots.update(ls => ls.map(s => s.idProductComboSlot === id ? slot : s));
        this.logger.success('✅ Slot de combo actualizado');
      },
      error: (e) => this.logger.error('❌ Error al actualizar slot de combo:', e),
    });
  }

  deleteComboSlot(id: number): void {
    this.productComboSlotSvc.delete(id).subscribe({
      next: () => this.expandedProductComboSlots.update(ls => ls.filter(s => s.idProductComboSlot !== id)),
      error: (e) => this.logger.error('❌ Error al eliminar slot de combo:', e),
    });
  }

  // ── Métodos de soporte (template) ─────────────────────────────────────────
  clearProductError(): void { this.productSvc.clearError(); }
}
