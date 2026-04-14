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
import { IncomeStatementWebComponent } from './components/income-statement-web/income-statement-web.component';
import { IncomeStatementMobileComponent } from './components/income-statement-mobile/income-statement-mobile.component';

@Component({
  selector: 'app-income-statement',
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: true,
  imports: [IncomeStatementWebComponent, IncomeStatementMobileComponent],
  templateUrl: './income-statement.html',
})
export class IncomeStatementPage extends ResponsiveComponent implements OnInit, OnDestroy {
  private readonly svc    = inject(FinancialStatementService);
  private readonly logger = inject(LoggerService).getLogger('IncomeStatementPage');

  // ── Estado del servicio ───────────────────────────────────────────
  isLoading = this.svc.isLoading;
  error     = this.svc.error;
  report    = this.svc.incomeStatement;

  // ── Filtro activo ─────────────────────────────────────────────────
  filter = signal<FinancialStatementFilter>({ year: new Date().getFullYear() });

  constructor(public appSettings: AppSettings) {
    super();
  }

  override ngOnDestroy(): void {
    super.ngOnDestroy();
  }

  ngOnInit(): void {
    this.logger.info('🚀 Cargando Estado de Resultado');
    this.load();
  }

  load(): void {
    this.svc.loadIncomeStatement(this.filter()).subscribe({
      next: () => this.logger.success('✅ Estado de Resultado cargado'),
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
