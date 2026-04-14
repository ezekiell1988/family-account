import {
  Component,
  ChangeDetectionStrategy,
  input,
  output,
  signal,
  computed,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { PanelComponent } from '../../../../../components';
import { FinancialStatementFilter, CashFlowStatementDto } from '../../../../../shared/models';

@Component({
  selector: 'app-cash-flow-web',
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: true,
  imports: [CommonModule, FormsModule, PanelComponent],
  templateUrl: './cash-flow-web.component.html',
})
export class CashFlowWebComponent {
  isLoading    = input(false);
  errorMessage = input('');
  report       = input<CashFlowStatementDto | null>(null);
  filter       = input<FinancialStatementFilter>({});

  filterChange = output<FinancialStatementFilter>();
  refresh      = output<void>();
  clearError   = output<void>();

  filterYear  = signal<number | null>(new Date().getFullYear());
  filterMonth = signal<number | null>(null);
  filterFrom  = signal<string>('');
  filterTo    = signal<string>('');

  months = [
    { value: 1, label: 'Enero' }, { value: 2, label: 'Febrero' },
    { value: 3, label: 'Marzo' }, { value: 4, label: 'Abril' },
    { value: 5, label: 'Mayo' }, { value: 6, label: 'Junio' },
    { value: 7, label: 'Julio' }, { value: 8, label: 'Agosto' },
    { value: 9, label: 'Septiembre' }, { value: 10, label: 'Octubre' },
    { value: 11, label: 'Noviembre' }, { value: 12, label: 'Diciembre' },
  ];

  netIncome           = computed(() => this.report()?.netIncome ?? 0);
  totalAssetChange    = computed(() => this.report()?.totalAssetChange ?? 0);
  totalLiabilityChange = computed(() => this.report()?.totalLiabilityChange ?? 0);
  totalEquityChange   = computed(() => this.report()?.totalEquityChange ?? 0);

  applyFilter(): void {
    const f: FinancialStatementFilter = {};
    if (this.filterFrom() && this.filterTo()) {
      f.dateFrom = this.filterFrom();
      f.dateTo   = this.filterTo();
    } else if (this.filterYear() != null && this.filterMonth() != null) {
      f.year  = this.filterYear()!;
      f.month = this.filterMonth()!;
    } else if (this.filterYear() != null) {
      f.year = this.filterYear()!;
    }
    this.filterChange.emit(f);
  }

  formatCurrency(value: number): string {
    return value.toLocaleString('es-CR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
  }

  changeClass(value: number): string {
    return value > 0 ? 'text-success' : value < 0 ? 'text-danger' : '';
  }
}
