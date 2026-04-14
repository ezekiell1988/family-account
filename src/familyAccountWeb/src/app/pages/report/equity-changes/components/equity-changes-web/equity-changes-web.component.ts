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
import { FinancialStatementFilter, EquityStatementDto } from '../../../../../shared/models';

@Component({
  selector: 'app-equity-changes-web',
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: true,
  imports: [CommonModule, FormsModule, PanelComponent],
  templateUrl: './equity-changes-web.component.html',
})
export class EquityChangesWebComponent {
  isLoading    = input(false);
  errorMessage = input('');
  report       = input<EquityStatementDto | null>(null);
  filter       = input<FinancialStatementFilter>({});

  filterChange = output<FinancialStatementFilter>();
  refresh      = output<void>();
  clearError   = output<void>();

  filterYear  = signal<number | null>(new Date().getFullYear());
  filterMonth = signal<number | null>(null);

  months = [
    { value: 1, label: 'Enero' }, { value: 2, label: 'Febrero' },
    { value: 3, label: 'Marzo' }, { value: 4, label: 'Abril' },
    { value: 5, label: 'Mayo' }, { value: 6, label: 'Junio' },
    { value: 7, label: 'Julio' }, { value: 8, label: 'Agosto' },
    { value: 9, label: 'Septiembre' }, { value: 10, label: 'Octubre' },
    { value: 11, label: 'Noviembre' }, { value: 12, label: 'Diciembre' },
  ];

  totalOpeningEquity         = computed(() => this.report()?.totalOpeningEquity ?? 0);
  totalContributions         = computed(() => this.report()?.totalContributions ?? 0);
  totalWithdrawals           = computed(() => this.report()?.totalWithdrawals ?? 0);
  netIncome                  = computed(() => this.report()?.netIncome ?? 0);
  totalClosingEquity         = computed(() => this.report()?.totalClosingEquity ?? 0);
  totalEquityIncludingNetIncome = computed(() => this.report()?.totalEquityIncludingNetIncome ?? 0);

  closingClass = computed(() =>
    this.totalEquityIncludingNetIncome() >= 0 ? 'text-success' : 'text-danger'
  );

  formatCurrency(value: number): string {
    return new Intl.NumberFormat('es-CR', { style: 'currency', currency: 'CRC', minimumFractionDigits: 2 }).format(value);
  }

  applyFilter(): void {
    const f: FinancialStatementFilter = {};
    if (this.filterYear() != null && this.filterMonth() != null) {
      f.year  = this.filterYear()!;
      f.month = this.filterMonth()!;
    } else if (this.filterYear() != null) {
      f.year = this.filterYear()!;
    }
    this.filterChange.emit(f);
  }
}
