import {
  Component,
  OnInit,
  OnDestroy,
  inject,
  signal,
  ChangeDetectionStrategy,
} from '@angular/core';
import { finalize, forkJoin, switchMap } from 'rxjs';
import {
  AppSettings,
  PurchaseInvoiceService,
  PurchaseInvoiceTypeService,
  CurrencyService,
  BankAccountService,
  AccountingEntryService,
  AccountService,
  ProductService,
  ProductAccountService,
  UnitOfMeasureService,
  CostCenterService,
  ContactService,
  LoggerService,
} from '../../../service';
import { ResponsiveComponent } from '../../../shared';
import {
  CreatePurchaseInvoiceRequest,
  UpdatePurchaseInvoiceRequest,
  UpdateAccountingEntryRequest,
  CreateProductAccountRequest,
  UpdateProductAccountRequest,
} from '../../../shared/models';
import { PurchaseInvoicesWebComponent, PurchaseInvoicesMobileComponent } from './components';

@Component({
  selector: 'app-purchase-invoices',
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: true,
  imports: [PurchaseInvoicesWebComponent, PurchaseInvoicesMobileComponent],
  templateUrl: './purchase-invoices.html',
})
export class PurchaseInvoicesPage extends ResponsiveComponent implements OnInit, OnDestroy {
  private readonly svc        = inject(PurchaseInvoiceService);
  private readonly typeSvc    = inject(PurchaseInvoiceTypeService);
  private readonly currSvc    = inject(CurrencyService);
  private readonly bankSvc    = inject(BankAccountService);
  private readonly entrySvc   = inject(AccountingEntryService);
  private readonly accountSvc    = inject(AccountService);
  private readonly productSvc    = inject(ProductService);
  private readonly productAccSvc = inject(ProductAccountService);
  private readonly unitSvc       = inject(UnitOfMeasureService);
  private readonly costCenterSvc = inject(CostCenterService);
  private readonly contactSvc    = inject(ContactService);
  private readonly logger         = inject(LoggerService).getLogger('PurchaseInvoicesPage');

  // ── Estado del servicio ──────────────────────────────────────────────────
  lineAccError    = signal<string | null>(null);
  isLoading       = this.svc.isLoading;
  error         = this.svc.error;
  invoices      = this.svc.items;
  totalCount    = this.svc.totalCount;
  invoiceTypes  = this.typeSvc.items;
  currencies    = this.currSvc.currencies;
  fiscalPeriods = this.svc.fiscalPeriods;
  bankAccounts  = this.bankSvc.items;
  entries       = this.entrySvc.entries;
  accounts      = this.accountSvc.accounts;
  products      = this.productSvc.items;
  units         = this.unitSvc.items;
  productAccounts = this.productAccSvc.items;
  costCenters   = this.costCenterSvc.items;
  providers     = this.contactSvc.providers;

  // ── Estado local ──────────────────────────────────────────────────
  deletingId = signal<number | null>(null);

  constructor(public appSettings: AppSettings) {
    super();
  }

  override ngOnDestroy(): void {
    super.ngOnDestroy();
  }

  ngOnInit(): void {
    this.logger.info('🚀 Cargando Facturas de Compra');
    this.loadAll();
  }

  loadAll(): void {
    this.logger.info('📋 Cargando facturas y catálogos');

    forkJoin([
      this.typeSvc.loadActive(),
      this.currSvc.loadList(),
      this.svc.loadFiscalPeriods(),
      this.bankSvc.loadList(),
      this.entrySvc.loadList(),
      this.accountSvc.loadList(),
      this.productSvc.loadList(),
      this.productAccSvc.loadList(),
      this.unitSvc.loadList(),
      this.costCenterSvc.loadList(),
      this.contactSvc.loadProviders(),
    ]).pipe(
      finalize(() => this.logger.debug('Catálogos finalizados')),
    ).subscribe({
      error: (e) => this.logger.error('❌ Error al cargar catálogos:', e),
    });

    this.svc.loadList()
      .pipe(finalize(() => this.logger.debug('Facturas finalizadas')))
      .subscribe({
        next: () => this.logger.success('✅ Facturas cargadas'),
        error: (e) => this.logger.error('❌ Error al cargar facturas:', e),
      });
  }

  onCreate(req: CreatePurchaseInvoiceRequest): void {
    const nameExists = this.contactSvc.providers()
      .some(p => p.name.toLowerCase() === req.providerName.trim().toLowerCase());

    const doCreate = () =>
      this.svc.create(req)
        .pipe(finalize(() => {}))
        .subscribe({
          next: () => this.logger.success('✅ Factura creada'),
          error: (e) => this.logger.error('❌ Error al crear factura:', e),
        });

    if (!nameExists && req.providerName.trim()) {
      this.contactSvc.getOrCreate(req.providerName.trim()).subscribe({
        next: () => doCreate(),
        error: () => doCreate(), // igual crear la factura aunque falle el contacto
      });
    } else {
      doCreate();
    }
  }

  onEditSave(req: UpdatePurchaseInvoiceRequest & { id: number }): void {
    const { id, ...payload } = req;
    this.svc.update(id, payload)
      .pipe(finalize(() => {}))
      .subscribe({
        next: () => this.logger.success('✅ Factura actualizada'),
        error: (e) => this.logger.error('❌ Error al actualizar factura:', e),
      });
  }

  onDelete(id: number): void {
    this.deletingId.set(id);
    this.svc.delete(id)
      .pipe(finalize(() => this.deletingId.set(null)))
      .subscribe({
        next: () => this.logger.success('✅ Factura eliminada'),
        error: (e) => this.logger.error('❌ Error al eliminar factura:', e),
      });
  }

  onConfirm(id: number): void {
    this.svc.confirm(id)
      .pipe(
        switchMap(() => this.entrySvc.loadList()),
        finalize(() => {}),
      )
      .subscribe({
        next: () => this.logger.success('✅ Factura confirmada'),
        error: (e) => this.logger.error('❌ Error al confirmar factura:', e),
      });
  }

  onCancel(id: number): void {
    this.svc.cancel(id)
      .pipe(finalize(() => {}))
      .subscribe({
        next: () => this.logger.success('✅ Factura anulada'),
        error: (e) => this.logger.error('❌ Error al anular factura:', e),
      });
  }

  clearError(): void {
    this.svc.clearError();
  }

  private extractApiError(e: unknown): string {
    if (e && typeof e === 'object') {
      const err = e as Record<string, unknown>;
      // ProblemDetails / ValidationProblem devuelven title o errors
      const errorBody = err['error'] as Record<string, unknown> | undefined;
      if (errorBody) {
        if (typeof errorBody['detail'] === 'string') return errorBody['detail'];
        if (typeof errorBody['title'] === 'string')  return errorBody['title'];
        if (errorBody['errors']) {
          const flat = Object.values(errorBody['errors'] as Record<string, string[]>).flat();
          if (flat.length) return flat.join(' ');
        }
      }
      if (typeof err['message'] === 'string') return err['message'];
    }
    return 'Error inesperado al guardar. Intente de nuevo.';
  }

  onCreateProductAccount(req: CreateProductAccountRequest): void {
    this.productAccSvc.create(req)
      .pipe(finalize(() => {}))
      .subscribe({
        next: () => { this.lineAccError.set(null); this.logger.success('✅ Distribución contable creada'); },
        error: (e) => { const msg = this.extractApiError(e); this.lineAccError.set(msg); this.logger.error('❌ Error al crear distribución:', e); },
      });
  }

  onUpdateProductAccount(req: UpdateProductAccountRequest & { id: number }): void {
    const { id, ...payload } = req;
    this.productAccSvc.update(id, payload)
      .pipe(finalize(() => {}))
      .subscribe({
        next: () => { this.lineAccError.set(null); this.logger.success('✅ Distribución contable actualizada'); },
        error: (e) => { const msg = this.extractApiError(e); this.lineAccError.set(msg); this.logger.error('❌ Error al actualizar distribución:', e); },
      });
  }

  onDeleteProductAccount(id: number): void {
    this.productAccSvc.delete(id)
      .pipe(finalize(() => {}))
      .subscribe({
        next: () => { this.lineAccError.set(null); this.logger.success('✅ Distribución contable eliminada'); },
        error: (e) => { const msg = this.extractApiError(e); this.lineAccError.set(msg); this.logger.error('❌ Error al eliminar distribución:', e); },
      });
  }

  onEditEntrySave(req: UpdateAccountingEntryRequest & { id: number }): void {
    const { id, ...payload } = req;
    this.entrySvc.update(id, payload)
      .pipe(finalize(() => {}))
      .subscribe({
        next: () => this.logger.success('✅ Asiento actualizado desde Facturas de Compra'),
        error: (e) => this.logger.error('❌ Error al actualizar asiento:', e),
      });
  }

}

