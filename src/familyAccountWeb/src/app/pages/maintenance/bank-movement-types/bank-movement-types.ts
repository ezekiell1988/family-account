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
  BankMovementTypeService,
  AccountService,
  LoggerService,
} from '../../../service';
import { ResponsiveComponent } from '../../../shared';
import { CreateBankMovementTypeRequest, UpdateBankMovementTypeRequest } from '../../../shared/models';
import { BankMovementTypesWebComponent, BankMovementTypesMobileComponent } from './components';

@Component({
  selector: 'app-bank-movement-types',
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: true,
  imports: [BankMovementTypesWebComponent, BankMovementTypesMobileComponent],
  templateUrl: './bank-movement-types.html',
})
export class BankMovementTypesPage extends ResponsiveComponent implements OnInit, OnDestroy {
  private readonly svc        = inject(BankMovementTypeService);
  private readonly accountSvc = inject(AccountService);
  private readonly logger     = inject(LoggerService).getLogger('BankMovementTypesPage');

  isLoading  = this.svc.isLoading;
  error      = this.svc.error;
  items      = this.svc.items;
  totalCount = this.svc.totalCount;

  accounts = this.accountSvc.accounts;

  deletingId = signal<number | null>(null);

  constructor(public appSettings: AppSettings) {
    super();
  }

  override ngOnDestroy(): void {
    super.ngOnDestroy();
  }

  ngOnInit(): void {
    this.logger.info('🚀 Cargando Tipos de Movimiento Bancario');
    this.load();
    if (this.accounts().length === 0) this.accountSvc.loadList().subscribe();
  }

  load(): void {
    this.logger.info('📋 Cargando lista de tipos de movimiento bancario');
    this.svc.loadList()
      .pipe(finalize(() => this.logger.debug('Petición finalizada')))
      .subscribe({
        next: () => this.logger.success('✅ Tipos de movimiento bancario cargados'),
        error: (e) => this.logger.error('❌ Error al cargar tipos de movimiento bancario:', e),
      });
  }

  onCreate(req: CreateBankMovementTypeRequest): void {
    this.svc.create(req)
      .subscribe({
        next: () => this.logger.success('✅ Tipo de movimiento creado'),
        error: (e) => this.logger.error('❌ Error al crear:', e),
      });
  }

  onEditSave(req: UpdateBankMovementTypeRequest & { id: number }): void {
    const { id, ...payload } = req;
    this.svc.update(id, payload)
      .subscribe({
        next: () => this.logger.success('✅ Tipo de movimiento actualizado'),
        error: (e) => this.logger.error('❌ Error al actualizar:', e),
      });
  }

  onDelete(id: number): void {
    this.deletingId.set(id);
    this.svc.delete(id)
      .pipe(finalize(() => this.deletingId.set(null)))
      .subscribe({
        next: () => this.logger.success('✅ Tipo de movimiento eliminado'),
        error: (e) => this.logger.error('❌ Error al eliminar:', e),
      });
  }

  clearError(): void {
    this.svc.clearError();
  }
}
