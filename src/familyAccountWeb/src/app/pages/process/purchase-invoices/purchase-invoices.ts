import {
  Component,
  OnInit,
  OnDestroy,
  inject,
  signal,
  ChangeDetectionStrategy,
} from '@angular/core';
import { finalize, forkJoin } from 'rxjs';
import {
  AppSettings,
  PurchaseInvoiceService,
  PurchaseInvoiceTypeService,
  CurrencyService,
  BankAccountService,
  LoggerService,
} from '../../../service';
import { ResponsiveComponent } from '../../../shared';
import {
  CreatePurchaseInvoiceRequest,
  UpdatePurchaseInvoiceRequest,
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
  private readonly logger     = inject(LoggerService).getLogger('PurchaseInvoicesPage');

  // ── Estado del servicio ───────────────────────────────────────
  isLoading     = this.svc.isLoading;
  error         = this.svc.error;
  invoices      = this.svc.items;
  totalCount    = this.svc.totalCount;
  invoiceTypes  = this.typeSvc.items;
  currencies    = this.currSvc.currencies;
  fiscalPeriods = this.svc.fiscalPeriods;
  bankAccounts  = this.bankSvc.items;

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
    this.svc.create(req)
      .pipe(finalize(() => {}))
      .subscribe({
        next: () => this.logger.success('✅ Factura creada'),
        error: (e) => this.logger.error('❌ Error al crear factura:', e),
      });
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
      .pipe(finalize(() => {}))
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
}
