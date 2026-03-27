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
  BankService,
  BankAccountService,
  AccountService,
  CurrencyService,
  LoggerService,
} from '../../../service';
import { ResponsiveComponent } from '../../../shared';
import { CreateBankRequest, UpdateBankRequest, CreateBankAccountRequest, UpdateBankAccountRequest } from '../../../shared/models';
import { BanksWebComponent, BanksMobileComponent, BankAccountsWebComponent } from './components';

@Component({
  selector: 'app-banks',
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: true,
  imports: [BanksWebComponent, BanksMobileComponent, BankAccountsWebComponent],
  templateUrl: './banks.html',
})
export class BanksPage extends ResponsiveComponent implements OnInit, OnDestroy {
  private readonly svc         = inject(BankService);
  private readonly bankAccountSvc = inject(BankAccountService);
  private readonly accountSvc  = inject(AccountService);
  private readonly currencySvc = inject(CurrencyService);
  private readonly logger      = inject(LoggerService).getLogger('BanksPage');

  isLoading  = this.svc.isLoading;
  error      = this.svc.error;
  banks      = this.svc.banks;
  totalCount = this.svc.totalCount;

  bankAccounts             = this.bankAccountSvc.items;
  bankAccountsTotalCount   = this.bankAccountSvc.totalCount;
  bankAccountsIsLoading    = this.bankAccountSvc.isLoading;
  bankAccountsError        = this.bankAccountSvc.error;
  accounts                 = this.accountSvc.accounts;
  currencies               = this.currencySvc.currencies;

  deletingId            = signal<number | null>(null);
  deletingBankAccountId = signal<number | null>(null);
  selectedBankId        = signal<number | null>(null);

  constructor(public appSettings: AppSettings) {
    super();
  }

  override ngOnDestroy(): void {
    super.ngOnDestroy();
  }

  ngOnInit(): void {
    this.logger.info('🚀 Cargando Bancos');
    this.load();
    if (this.accounts().length === 0) this.accountSvc.loadList().subscribe();
    this.currencySvc.loadList().subscribe();
  }

  load(): void {
    this.logger.info('📋 Cargando lista de bancos');
    this.svc.loadList()
      .pipe(finalize(() => this.logger.debug('Petición finalizada')))
      .subscribe({
        next: () => this.logger.success('✅ Bancos cargados'),
        error: (e) => this.logger.error('❌ Error al cargar bancos:', e),
      });
    this.loadBankAccounts();
  }

  loadBankAccounts(): void {
    this.bankAccountSvc.loadList().subscribe({
      next: () => this.logger.success('✅ Cuentas bancarias cargadas'),
      error: (e) => this.logger.error('❌ Error al cargar cuentas bancarias:', e),
    });
  }

  onCreate(req: CreateBankRequest): void {
    this.svc.create(req)
      .pipe(finalize(() => {}))
      .subscribe({
        next: () => this.logger.success('✅ Banco creado'),
        error: (e) => this.logger.error('❌ Error al crear banco:', e),
      });
  }

  onEditSave(req: UpdateBankRequest & { id: number }): void {
    const { id, ...payload } = req;
    this.svc.update(id, payload)
      .pipe(finalize(() => {}))
      .subscribe({
        next: () => this.logger.success('✅ Banco actualizado'),
        error: (e) => this.logger.error('❌ Error al actualizar banco:', e),
      });
  }

  onDelete(id: number): void {
    this.deletingId.set(id);
    this.svc.delete(id)
      .pipe(finalize(() => this.deletingId.set(null)))
      .subscribe({
        next: () => this.logger.success('✅ Banco eliminado'),
        error: (e) => this.logger.error('❌ Error al eliminar banco:', e),
      });
  }

  clearError(): void {
    this.svc.clearError();
  }

  onSelectBank(bankId: number | null): void {
    this.selectedBankId.set(bankId);
  }

  onBankAccountCreate(req: CreateBankAccountRequest): void {
    this.bankAccountSvc.create(req).subscribe({
      next: () => this.logger.success('✅ Cuenta bancaria creada'),
      error: (e) => this.logger.error('❌ Error al crear cuenta bancaria:', e),
    });
  }

  onBankAccountEditSave(req: UpdateBankAccountRequest & { id: number }): void {
    const { id, ...payload } = req;
    this.bankAccountSvc.update(id, payload).subscribe({
      next: () => this.logger.success('✅ Cuenta bancaria actualizada'),
      error: (e) => this.logger.error('❌ Error al actualizar cuenta bancaria:', e),
    });
  }

  onBankAccountDelete(id: number): void {
    this.deletingBankAccountId.set(id);
    this.bankAccountSvc.delete(id)
      .pipe(finalize(() => this.deletingBankAccountId.set(null)))
      .subscribe({
        next: () => this.logger.success('✅ Cuenta bancaria eliminada'),
        error: (e) => this.logger.error('❌ Error al eliminar cuenta bancaria:', e),
      });
  }

  clearBankAccountError(): void {
    this.bankAccountSvc.clearError();
  }
}
