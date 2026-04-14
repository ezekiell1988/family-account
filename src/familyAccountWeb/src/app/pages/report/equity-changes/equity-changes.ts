import {
  Component,
  OnInit,
  OnDestroy,
  inject,
  signal,
  ChangeDetectionStrategy,
} from '@angular/core';
import { AppSettings, FinancialStatementService, LoggerService } from '../../../service';
import { ResponsiveComponent } from '../../../shared';
import { FinancialStatementFilter } from '../../../shared/models';
import { EquityChangesWebComponent } from './components/equity-changes-web/equity-changes-web.component';
import { EquityChangesMobileComponent } from './components/equity-changes-mobile/equity-changes-mobile.component';

@Component({
  selector: 'app-equity-changes',
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: true,
  imports: [EquityChangesWebComponent, EquityChangesMobileComponent],
  templateUrl: './equity-changes.html',
})
export class EquityChangesPage extends ResponsiveComponent implements OnInit, OnDestroy {
  private readonly svc    = inject(FinancialStatementService);
  private readonly logger = inject(LoggerService).getLogger('EquityChangesPage');

  isLoading = this.svc.isLoading;
  error     = this.svc.error;
  report    = this.svc.equityStatement;

  filter = signal<FinancialStatementFilter>({ year: new Date().getFullYear() });

  constructor(public appSettings: AppSettings) {
    super();
  }

  override ngOnDestroy(): void {
    super.ngOnDestroy();
  }

  ngOnInit(): void {
    this.logger.info('🚀 Cargando Estado de Cambios en el Patrimonio');
    this.load();
  }

  load(): void {
    this.svc.loadEquityChanges(this.filter()).subscribe({
      next: () => this.logger.success('✅ Cambios en el Patrimonio cargados'),
      error: (e) => this.logger.error('❌ Error:', e),
    });
  }

  onFilterChange(f: FinancialStatementFilter): void {
    this.filter.set(f);
    this.load();
  }

  clearError(): void {
    this.svc.clearError();
  }
}
