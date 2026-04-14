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
import { FinancialStatementFilter, BalanceSheetDto } from '../../../../../shared/models';

@Component({
  selector: 'app-balance-sheet-web',
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: true,
  imports: [CommonModule, FormsModule, PanelComponent],
  templateUrl: './balance-sheet-web.component.html',
})
export class BalanceSheetWebComponent {
  isLoading    = input(false);
  errorMessage = input('');
  report       = input<BalanceSheetDto | null>(null);
  filter       = input<FinancialStatementFilter>({});

  filterChange = output<FinancialStatementFilter>();
  refresh      = output<void>();
  clearError   = output<void>();

  filterYear  = signal<number | null>(new Date().getFullYear());
  filterMonth = signal<number | null>(null);
  filterTo    = signal<string>('');

  months = [
    { value: 1, label: 'Enero' }, { value: 2, label: 'Febrero' },
    { value: 3, label: 'Marzo' }, { value: 4, label: 'Abril' },
    { value: 5, label: 'Mayo' }, { value: 6, label: 'Junio' },
    { value: 7, label: 'Julio' }, { value: 8, label: 'Agosto' },
    { value: 9, label: 'Septiembre' }, { value: 10, label: 'Octubre' },
    { value: 11, label: 'Noviembre' }, { value: 12, label: 'Diciembre' },
  ];

  totalAssets              = computed(() => this.report()?.totalAssets ?? 0);
  totalLiabilities         = computed(() => this.report()?.totalLiabilities ?? 0);
  totalCapital             = computed(() => this.report()?.totalCapital ?? 0);
  totalLiabilitiesCapital  = computed(() => this.report()?.totalLiabilitiesAndCapital ?? 0);

  applyFilter(): void {
    const f: FinancialStatementFilter = {};
    if (this.filterTo()) {
      f.dateTo = this.filterTo();
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
}
