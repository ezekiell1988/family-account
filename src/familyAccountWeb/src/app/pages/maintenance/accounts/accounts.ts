import {
  Component,
  OnInit,
  OnDestroy,
  inject,
  signal,
  ChangeDetectionStrategy,
} from '@angular/core';
import { finalize } from 'rxjs/operators';
import { AppSettings, AccountService, LoggerService } from '../../../service';
import { ResponsiveComponent } from '../../../shared';
import { AccountDto, CreateAccountRequest, UpdateAccountRequest } from '../../../shared/models';
import { AccountsWebComponent, AccountsMobileComponent } from './components';

@Component({
  selector: 'app-accounts',
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: true,
  imports: [AccountsWebComponent, AccountsMobileComponent],
  templateUrl: './accounts.html',
})
export class AccountsPage extends ResponsiveComponent implements OnInit, OnDestroy {
  private readonly svc    = inject(AccountService);
  private readonly logger = inject(LoggerService).getLogger('AccountsPage');

  // ── Estado del servicio (expuesto al template) ────────────────────
  isLoading  = this.svc.isLoading;
  error      = this.svc.error;
  accounts   = this.svc.accounts;
  totalCount = this.svc.totalCount;

  // ── Estado local ──────────────────────────────────────────────────
  deletingId = signal<number | null>(null);

  constructor(public appSettings: AppSettings) {
    super();
  }

  override ngOnDestroy(): void {
    super.ngOnDestroy();
  }

  ngOnInit(): void {
    this.logger.info('🚀 Cargando Cuentas Contables');
    this.load();
  }

  load(): void {
    this.logger.info('📋 Cargando lista de cuentas');
    this.svc.loadList()
      .pipe(finalize(() => this.logger.debug('Petición finalizada')))
      .subscribe({
        next: () => this.logger.success('✅ Cuentas cargadas'),
        error: (e) => this.logger.error('❌ Error al cargar cuentas:', e),
      });
  }

  onCreate(req: CreateAccountRequest): void {
    this.svc.create(req)
      .pipe(finalize(() => {}))
      .subscribe({
        next: () => this.logger.success('✅ Cuenta creada'),
        error: (e) => this.logger.error('❌ Error al crear cuenta:', e),
      });
  }

  onEditSave(req: UpdateAccountRequest & { id: number }): void {
    const { id, ...payload } = req;
    this.svc.update(id, payload)
      .pipe(finalize(() => {}))
      .subscribe({
        next: () => this.logger.success('✅ Cuenta actualizada'),
        error: (e) => this.logger.error('❌ Error al actualizar cuenta:', e),
      });
  }

  onDelete(id: number): void {
    this.deletingId.set(id);
    this.svc.delete(id)
      .pipe(finalize(() => this.deletingId.set(null)))
      .subscribe({
        next: () => this.logger.success('✅ Cuenta eliminada'),
        error: (e) => this.logger.error('❌ Error al eliminar cuenta:', e),
      });
  }

  clearError(): void {
    this.svc.clearError();
  }
}
