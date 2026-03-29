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
  BankMovementService,
  AccountingEntryService,
  AccountService,
  LoggerService,
} from '../../../service';
import { ResponsiveComponent } from '../../../shared';
import {
  CreateBankMovementRequest,
  UpdateBankMovementRequest,
  UpdateAccountingEntryRequest,
} from '../../../shared/models';
import { BankMovementsWebComponent, BankMovementsMobileComponent } from './components';

@Component({
  selector: 'app-bank-movements',
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: true,
  imports: [BankMovementsWebComponent, BankMovementsMobileComponent],
  templateUrl: './bank-movements.html',
})
export class BankMovementsPage extends ResponsiveComponent implements OnInit, OnDestroy {
  private readonly svc        = inject(BankMovementService);
  private readonly entrySvc   = inject(AccountingEntryService);
  private readonly accountSvc = inject(AccountService);
  private readonly logger     = inject(LoggerService).getLogger('BankMovementsPage');

  // ── Estado del servicio ───────────────────────────────────────
  isLoading     = this.svc.isLoading;
  error         = this.svc.error;
  movements     = this.svc.items;
  totalCount    = this.svc.totalCount;
  bankAccounts  = this.svc.bankAccounts;
  movementTypes = this.svc.movementTypes;
  fiscalPeriods = this.svc.fiscalPeriods;
  entries       = this.entrySvc.entries;
  accounts      = this.accountSvc.accounts;

  // ── Estado local ──────────────────────────────────────────────────
  deletingId = signal<number | null>(null);

  constructor(public appSettings: AppSettings) {
    super();
  }

  override ngOnDestroy(): void {
    super.ngOnDestroy();
  }

  ngOnInit(): void {
    this.logger.info('🚀 Cargando Movimientos Bancarios');
    this.loadAll();
  }

  loadAll(): void {
    this.logger.info('📋 Cargando movimientos y catálogos');

    forkJoin([
      this.svc.loadBankAccounts(),
      this.svc.loadMovementTypes(),
      this.svc.loadFiscalPeriods(),
      this.entrySvc.loadList(),
      this.accountSvc.loadList(),
    ]).pipe(
      finalize(() => this.logger.debug('Catálogos finalizados')),
    ).subscribe({
      error: (e) => this.logger.error('❌ Error al cargar catálogos:', e),
    });

    this.svc.loadList()
      .pipe(finalize(() => this.logger.debug('Movimientos finalizados')))
      .subscribe({
        next: () => this.logger.success('✅ Movimientos cargados'),
        error: (e) => this.logger.error('❌ Error al cargar movimientos:', e),
      });
  }

  onCreate(req: CreateBankMovementRequest): void {
    this.svc.create(req)
      .pipe(finalize(() => {}))
      .subscribe({
        next: () => this.logger.success('✅ Movimiento creado'),
        error: (e) => this.logger.error('❌ Error al crear movimiento:', e),
      });
  }

  onEditSave(req: UpdateBankMovementRequest & { id: number }): void {
    const { id, ...payload } = req;
    this.svc.update(id, payload)
      .pipe(finalize(() => {}))
      .subscribe({
        next: () => this.logger.success('✅ Movimiento actualizado'),
        error: (e) => this.logger.error('❌ Error al actualizar movimiento:', e),
      });
  }

  onDelete(id: number): void {
    this.deletingId.set(id);
    this.svc.delete(id)
      .pipe(finalize(() => this.deletingId.set(null)))
      .subscribe({
        next: () => this.logger.success('✅ Movimiento eliminado'),
        error: (e) => this.logger.error('❌ Error al eliminar movimiento:', e),
      });
  }

  onConfirm(id: number): void {
    this.svc.confirm(id)
      .pipe(finalize(() => {}))
      .subscribe({
        next: () => this.logger.success('✅ Movimiento confirmado — asiento generado'),
        error: (e) => this.logger.error('❌ Error al confirmar movimiento:', e),
      });
  }

  onCancel(id: number): void {
    this.svc.cancel(id)
      .pipe(finalize(() => {}))
      .subscribe({
        next: () => this.logger.success('✅ Movimiento anulado'),
        error: (e) => this.logger.error('❌ Error al anular movimiento:', e),
      });
  }

  clearError(): void {
    this.svc.clearError();
  }

  onEditEntrySave(req: UpdateAccountingEntryRequest & { id: number }): void {
    const { id, ...payload } = req;
    this.entrySvc.update(id, payload)
      .pipe(finalize(() => {}))
      .subscribe({
        next: () => this.logger.success('✅ Asiento actualizado desde Movimientos Bancarios'),
        error: (e) => this.logger.error('❌ Error al actualizar asiento:', e),
      });
  }
}
