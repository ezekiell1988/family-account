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
  BankAccountService,
  BankService,
  AccountService,
  CurrencyService,
  LoggerService,
} from '../../../service';
import { ResponsiveComponent } from '../../../shared';
import { CreateBankAccountRequest, UpdateBankAccountRequest } from '../../../shared/models';
import { BankAccountsWebComponent, BankAccountsMobileComponent } from './components';

@Component({
  selector: 'app-bank-accounts',
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: true,
  imports: [BankAccountsWebComponent, BankAccountsMobileComponent],
  templateUrl: './bank-accounts.html',
})
export class BankAccountsPage extends ResponsiveComponent implements OnInit, OnDestroy {
  private readonly svc         = inject(BankAccountService);
  private readonly bankSvc     = inject(BankService);
  private readonly accountSvc  = inject(AccountService);
  private readonly currencySvc = inject(CurrencyService);
  private readonly logger      = inject(LoggerService).getLogger('BankAccountsPage');

  isLoading    = this.svc.isLoading;
  error        = this.svc.error;
  bankAccounts = this.svc.items;
  totalCount   = this.svc.totalCount;

  banks      = this.bankSvc.banks;
  accounts   = this.accountSvc.accounts;
  currencies = this.currencySvc.currencies;

  deletingId = signal<number | null>(null);

  constructor(public appSettings: AppSettings) {
    super();
  }

  override ngOnDestroy(): void {
    super.ngOnDestroy();
  }

  ngOnInit(): void {
    this.logger.info('🚀 Cargando Cuentas Bancarias');
    this.load();
    if (this.banks().length === 0) {
      this.bankSvc.loadList().subscribe();
    }
    if (this.accounts().length === 0) {
      this.accountSvc.loadList().subscribe();
    }
    this.currencySvc.loadList().subscribe();
  }

  load(): void {
    this.svc.loadList()
      .pipe(finalize(() => this.logger.debug('Petición finalizada')))
      .subscribe({
        next: () => this.logger.success('✅ Cuentas bancarias cargadas'),
        error: (e) => this.logger.error('❌ Error al cargar cuentas bancarias:', e),
      });
  }

  onCreate(req: CreateBankAccountRequest): void {
    this.svc.create(req)
      .subscribe({
        next: () => this.logger.success('✅ Cuenta bancaria creada'),
        error: (e) => this.logger.error('❌ Error al crear cuenta bancaria:', e),
      });
  }

  onEditSave(req: UpdateBankAccountRequest & { id: number }): void {
    const { id, ...payload } = req;
    this.svc.update(id, payload)
      .subscribe({
        next: () => this.logger.success('✅ Cuenta bancaria actualizada'),
        error: (e) => this.logger.error('❌ Error al actualizar cuenta bancaria:', e),
      });
  }

  onDelete(id: number): void {
    this.deletingId.set(id);
    this.svc.delete(id)
      .pipe(finalize(() => this.deletingId.set(null)))
      .subscribe({
        next: () => this.logger.success('✅ Cuenta bancaria eliminada'),
        error: (e) => this.logger.error('❌ Error al eliminar cuenta bancaria:', e),
      });
  }

  clearError(): void {
    this.svc.clearError();
  }
}
