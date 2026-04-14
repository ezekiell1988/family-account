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
import { BalanceSheetWebComponent } from './components/balance-sheet-web/balance-sheet-web.component';
import { BalanceSheetMobileComponent } from './components/balance-sheet-mobile/balance-sheet-mobile.component';

@Component({
  selector: 'app-balance-sheet',
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: true,
  imports: [BalanceSheetWebComponent, BalanceSheetMobileComponent],
  templateUrl: './balance-sheet.html',
})
export class BalanceSheetPage extends ResponsiveComponent implements OnInit, OnDestroy {
  private readonly svc    = inject(FinancialStatementService);
  private readonly logger = inject(LoggerService).getLogger('BalanceSheetPage');

  isLoading = this.svc.isLoading;
  error     = this.svc.error;
  report    = this.svc.balanceSheet;

  filter = signal<FinancialStatementFilter>({ year: new Date().getFullYear() });

  constructor(public appSettings: AppSettings) {
    super();
  }

  override ngOnDestroy(): void {
    super.ngOnDestroy();
  }

  ngOnInit(): void {
    this.logger.info('🚀 Cargando Estado de Situación Financiera');
    this.load();
  }

  load(): void {
    this.svc.loadBalanceSheet(this.filter()).subscribe({
      next: () => this.logger.success('✅ Balance General cargado'),
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
