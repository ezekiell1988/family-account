import {
  Component,
  OnInit,
  OnDestroy,
  inject,
  signal,
  ChangeDetectionStrategy,
} from '@angular/core';
import { finalize } from 'rxjs/operators';
import { AppSettings, BankService, LoggerService } from '../../../service';
import { ResponsiveComponent } from '../../../shared';
import { CreateBankRequest, UpdateBankRequest } from '../../../shared/models';
import { BanksWebComponent, BanksMobileComponent } from './components';

@Component({
  selector: 'app-banks',
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: true,
  imports: [BanksWebComponent, BanksMobileComponent],
  templateUrl: './banks.html',
})
export class BanksPage extends ResponsiveComponent implements OnInit, OnDestroy {
  private readonly svc    = inject(BankService);
  private readonly logger = inject(LoggerService).getLogger('BanksPage');

  isLoading  = this.svc.isLoading;
  error      = this.svc.error;
  banks      = this.svc.banks;
  totalCount = this.svc.totalCount;

  deletingId = signal<number | null>(null);

  constructor(public appSettings: AppSettings) {
    super();
  }

  override ngOnDestroy(): void {
    super.ngOnDestroy();
  }

  ngOnInit(): void {
    this.logger.info('🚀 Cargando Bancos');
    this.load();
  }

  load(): void {
    this.logger.info('📋 Cargando lista de bancos');
    this.svc.loadList()
      .pipe(finalize(() => this.logger.debug('Petición finalizada')))
      .subscribe({
        next: () => this.logger.success('✅ Bancos cargados'),
        error: (e) => this.logger.error('❌ Error al cargar bancos:', e),
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
}
