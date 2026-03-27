import {
  Component,
  OnInit,
  OnDestroy,
  inject,
  signal,
  ChangeDetectionStrategy,
} from '@angular/core';
import { finalize, forkJoin } from 'rxjs';
import { AppSettings, AccountingEntryService, AccountService, LoggerService } from '../../../service';
import { ResponsiveComponent } from '../../../shared';
import { CreateAccountingEntryRequest, UpdateAccountingEntryRequest } from '../../../shared/models';
import { AccountingEntriesWebComponent, AccountingEntriesMobileComponent } from './components';

@Component({
  selector: 'app-accounting-entries',
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: true,
  imports: [AccountingEntriesWebComponent, AccountingEntriesMobileComponent],
  templateUrl: './accounting-entries.html',
})
export class AccountingEntriesPage extends ResponsiveComponent implements OnInit, OnDestroy {
  private readonly svc         = inject(AccountingEntryService);
  private readonly accountSvc  = inject(AccountService);
  private readonly logger      = inject(LoggerService).getLogger('AccountingEntriesPage');

  // ── Estado del servicio ───────────────────────────────────────────
  isLoading     = this.svc.isLoading;
  error         = this.svc.error;
  entries       = this.svc.entries;
  totalCount    = this.svc.totalCount;
  fiscalPeriods = this.svc.fiscalPeriods;
  currencies    = this.svc.currencies;
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
    this.logger.info('🚀 Cargando Asientos Contables');
    this.loadAll();
  }

  loadAll(): void {
    this.logger.info('📋 Cargando asientos y catálogos');

    // Cargar catálogos en paralelo (sin afectar el isLoading del servicio)
    forkJoin([
      this.svc.loadFiscalPeriods(),
      this.svc.loadCurrencies(),
      this.accountSvc.loadList(),
    ]).pipe(
      finalize(() => this.logger.debug('Catálogos finalizados')),
    ).subscribe({
      error: (e) => this.logger.error('❌ Error al cargar catálogos:', e),
    });

    // Cargar asientos (maneja isLoading)
    this.svc.loadList()
      .pipe(finalize(() => this.logger.debug('Asientos finalizados')))
      .subscribe({
        next: () => this.logger.success('✅ Asientos cargados'),
        error: (e) => this.logger.error('❌ Error al cargar asientos:', e),
      });
  }

  onCreate(req: CreateAccountingEntryRequest): void {
    this.svc.create(req)
      .pipe(finalize(() => {}))
      .subscribe({
        next: () => this.logger.success('✅ Asiento creado'),
        error: (e) => this.logger.error('❌ Error al crear asiento:', e),
      });
  }

  onEditSave(req: UpdateAccountingEntryRequest & { id: number }): void {
    const { id, ...payload } = req;
    this.svc.update(id, payload)
      .pipe(finalize(() => {}))
      .subscribe({
        next: () => this.logger.success('✅ Asiento actualizado'),
        error: (e) => this.logger.error('❌ Error al actualizar asiento:', e),
      });
  }

  onDelete(id: number): void {
    this.deletingId.set(id);
    this.svc.delete(id)
      .pipe(finalize(() => this.deletingId.set(null)))
      .subscribe({
        next: () => this.logger.success('✅ Asiento eliminado'),
        error: (e) => this.logger.error('❌ Error al eliminar asiento:', e),
      });
  }

  clearError(): void {
    this.svc.clearError();
  }
}
