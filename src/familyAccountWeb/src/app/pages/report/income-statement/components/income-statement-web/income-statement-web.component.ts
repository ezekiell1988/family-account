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
import { FinancialStatementFilter, IncomeStatementDto } from '../../../../../shared/models';

@Component({
  selector: 'app-income-statement-web',
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: true,
  imports: [CommonModule, FormsModule, PanelComponent],
  templateUrl: './income-statement-web.component.html',
})
export class IncomeStatementWebComponent {
  // ── Inputs ────────────────────────────────────────────────────────
  isLoading    = input(false);
  errorMessage = input('');
  report       = input<IncomeStatementDto | null>(null);
  filter       = input<FinancialStatementFilter>({});

  // ── Outputs ───────────────────────────────────────────────────────
  filterChange = output<FinancialStatementFilter>();
  refresh      = output<void>();
  clearError   = output<void>();

  // ── Filtro local (formulario) ─────────────────────────────────────
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

  // ── Totales calculados ────────────────────────────────────────────
  totalRevenues = computed(() => this.report()?.totalRevenues ?? 0);
  totalExpenses = computed(() => this.report()?.totalExpenses ?? 0);
  netIncome     = computed(() => this.report()?.netIncome ?? 0);

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

  netIncomeClass = computed(() =>
    this.netIncome() >= 0 ? 'text-success' : 'text-danger'
  );
}
