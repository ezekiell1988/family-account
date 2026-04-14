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
import { CashFlowWebComponent } from './components/cash-flow-web/cash-flow-web.component';
import { CashFlowMobileComponent } from './components/cash-flow-mobile/cash-flow-mobile.component';

@Component({
  selector: 'app-cash-flow',
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: true,
  imports: [CashFlowWebComponent, CashFlowMobileComponent],
  templateUrl: './cash-flow.html',
})
export class CashFlowPage extends ResponsiveComponent implements OnInit, OnDestroy {
  private readonly svc    = inject(FinancialStatementService);
  private readonly logger = inject(LoggerService).getLogger('CashFlowPage');

  isLoading = this.svc.isLoading;
  error     = this.svc.error;
  report    = this.svc.cashFlow;

  filter = signal<FinancialStatementFilter>({ year: new Date().getFullYear() });

  constructor(public appSettings: AppSettings) {
    super();
  }

  override ngOnDestroy(): void {
    super.ngOnDestroy();
  }

  ngOnInit(): void {
    this.logger.info('🚀 Cargando Estado de Flujo de Efectivo');
    this.load();
  }

  load(): void {
    this.svc.loadCashFlow(this.filter()).subscribe({
      next: () => this.logger.success('✅ Flujo de Efectivo cargado'),
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
