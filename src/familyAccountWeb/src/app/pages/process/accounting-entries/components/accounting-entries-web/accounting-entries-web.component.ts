import {
  Component,
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  ViewChild,
  inject,
  input,
  output,
  signal,
  computed,
} from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import {
  NgxDatatableModule,
  ColumnMode,
  DatatableRowDetailDirective,
} from '@swimlane/ngx-datatable';
import { PanelComponent } from '../../../../../components';
import {
  AccountingEntryDto,
  AccountingEntryLineDto,
  AccountingEntryLineRequest,
  CreateAccountingEntryRequest,
  UpdateAccountingEntryRequest,
  FiscalPeriodLookup,
  CurrencyLookup,
  AccountDto,
} from '../../../../../shared/models';

interface FormLine {
  idAccount: number;
  debitAmount: number;
  creditAmount: number;
  descriptionLine: string;
}

const STATUS_OPTIONS = ['Borrador', 'Publicado', 'Anulado'] as const;

@Component({
  selector: 'app-accounting-entries-web',
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: true,
  imports: [CommonModule, FormsModule, NgxDatatableModule, PanelComponent, DatePipe],
  templateUrl: './accounting-entries-web.component.html',
})
export class AccountingEntriesWebComponent {
  // ── Inputs ────────────────────────────────────────────────────────
  entries       = input<AccountingEntryDto[]>([]);
  totalCount    = input(0);
  isLoading     = input(false);
  deletingId    = input<number | null>(null);
  errorMessage  = input('');
  fiscalPeriods = input<FiscalPeriodLookup[]>([]);
  currencies    = input<CurrencyLookup[]>([]);
  accounts      = input<AccountDto[]>([]);

  // ── Outputs ───────────────────────────────────────────────────────
  refresh    = output<void>();
  create     = output<CreateAccountingEntryRequest>();
  editSave   = output<UpdateAccountingEntryRequest & { id: number }>();
  remove     = output<number>();
  clearError = output<void>();

  // ── Row detail ────────────────────────────────────────────────────
  @ViewChild(DatatableRowDetailDirective) rowDetail!: DatatableRowDetailDirective;
  private cdr = inject(ChangeDetectorRef);
  expandedId  = signal<number | null>(null);

  // ── Filtros ───────────────────────────────────────────────────────
  filterStatus = signal('');
  filterSearch = signal('');

  filteredItems = computed(() => {
    let items = this.entries();
    const status = this.filterStatus();
    const search = this.filterSearch().toLowerCase().trim();
    if (status) items = items.filter(e => e.statusEntry === status);
    if (search) items = items.filter(e =>
      e.numberEntry.toLowerCase().includes(search) ||
      e.descriptionEntry.toLowerCase().includes(search) ||
      e.nameFiscalPeriod.toLowerCase().includes(search),
    );
    return items;
  });

  // ── Estado del formulario (cabecera) ──────────────────────────────
  showForm        = signal(false);
  editingId       = signal<number | null>(null);
  formFiscalPeriod = signal(0);
  formCurrency    = signal(0);
  formNumber      = signal('');
  formDate        = signal('');
  formDescription = signal('');
  formStatus      = signal<string>('Borrador');
  formReference   = signal('');
  formExchangeRate = signal(1);

  // ── Líneas del formulario ─────────────────────────────────────────
  formLines = signal<FormLine[]>([]);

  confirmDeleteId = signal<number | null>(null);

  isEditing  = computed(() => this.editingId() !== null);
  formTitle  = computed(() => this.isEditing() ? 'Editar Asiento Contable' : 'Nuevo Asiento Contable');

  totalDebit  = computed(() => this.formLines().reduce((s, l) => s + (l.debitAmount || 0), 0));
  totalCredit = computed(() => this.formLines().reduce((s, l) => s + (l.creditAmount || 0), 0));
  isBalanced  = computed(() =>
    this.formLines().length >= 2 &&
    this.totalDebit() > 0 &&
    Math.abs(this.totalDebit() - this.totalCredit()) < 0.001,
  );

  isFormValid = computed(() =>
    this.formFiscalPeriod() > 0 &&
    this.formCurrency() > 0 &&
    this.formNumber().trim().length > 0 &&
    this.formDate().length > 0 &&
    this.formDescription().trim().length > 0 &&
    this.formExchangeRate() > 0 &&
    this.isBalanced(),
  );

  // ── Metadata ──────────────────────────────────────────────────────
  ColumnMode    = ColumnMode;
  statusOptions = STATUS_OPTIONS;

  // ── Cuentas con movimientos para las líneas ───────────────────────
  movementAccounts = computed(() => this.accounts().filter(a => a.allowsMovements && a.isActive));

  // ── Helpers de display ────────────────────────────────────────────
  getStatusBadgeClass(status: string): string {
    const map: Record<string, string> = {
      Borrador:  'badge bg-warning text-dark',
      Publicado: 'badge bg-success',
      Anulado:   'badge bg-secondary',
    };
    return map[status] ?? 'badge bg-light text-dark';
  }

  formatAmount(value: number): string {
    if (!value) return '—';
    return value.toLocaleString('es-CR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
  }

  // ── Row detail ────────────────────────────────────────────────────
  toggleExpand(row: AccountingEntryDto): void {
    this.rowDetail.toggleExpandRow(row);
    const id = row.idAccountingEntry;
    this.expandedId.update(k => (k === id ? null : id));
    this.cdr.markForCheck();
  }

  // ── Gestión de líneas ─────────────────────────────────────────────
  addLine(): void {
    this.formLines.update(ls => [
      ...ls,
      { idAccount: 0, debitAmount: 0, creditAmount: 0, descriptionLine: '' },
    ]);
  }

  removeLine(index: number): void {
    this.formLines.update(ls => ls.filter((_, i) => i !== index));
  }

  updateLineAccount(index: number, value: number): void {
    this.formLines.update(ls => ls.map((l, i) => i === index ? { ...l, idAccount: value } : l));
  }

  updateLineDebit(index: number, value: number): void {
    this.formLines.update(ls => ls.map((l, i) =>
      i === index ? { ...l, debitAmount: value || 0, creditAmount: value ? 0 : l.creditAmount } : l,
    ));
  }

  updateLineCredit(index: number, value: number): void {
    this.formLines.update(ls => ls.map((l, i) =>
      i === index ? { ...l, creditAmount: value || 0, debitAmount: value ? 0 : l.debitAmount } : l,
    ));
  }

  updateLineDescription(index: number, value: string): void {
    this.formLines.update(ls => ls.map((l, i) => i === index ? { ...l, descriptionLine: value } : l));
  }

  // ── Formulario: abrir / cerrar ────────────────────────────────────
  private getTodayIso(): string {
    return new Date().toISOString().slice(0, 10);
  }

  openCreate(): void {
    this.editingId.set(null);
    this.formFiscalPeriod.set(0);
    this.formCurrency.set(0);
    this.formNumber.set('');
    this.formDate.set(this.getTodayIso());
    this.formDescription.set('');
    this.formStatus.set('Borrador');
    this.formReference.set('');
    this.formExchangeRate.set(1);
    this.formLines.set([
      { idAccount: 0, debitAmount: 0, creditAmount: 0, descriptionLine: '' },
      { idAccount: 0, debitAmount: 0, creditAmount: 0, descriptionLine: '' },
    ]);
    this.showForm.set(true);
  }

  openEdit(row: AccountingEntryDto): void {
    this.editingId.set(row.idAccountingEntry);
    this.formFiscalPeriod.set(row.idFiscalPeriod);
    this.formCurrency.set(row.idCurrency);
    this.formNumber.set(row.numberEntry);
    this.formDate.set(row.dateEntry);
    this.formDescription.set(row.descriptionEntry);
    this.formStatus.set(row.statusEntry);
    this.formReference.set(row.referenceEntry ?? '');
    this.formExchangeRate.set(row.exchangeRateValue);
    this.formLines.set(row.lines.map((l: AccountingEntryLineDto) => ({
      idAccount: l.idAccount,
      debitAmount: l.debitAmount,
      creditAmount: l.creditAmount,
      descriptionLine: l.descriptionLine ?? '',
    })));
    this.showForm.set(true);
  }

  cancelForm(): void {
    this.showForm.set(false);
    this.editingId.set(null);
  }

  submitForm(): void {
    if (!this.isFormValid()) return;

    const lines: AccountingEntryLineRequest[] = this.formLines().map(l => ({
      idAccount: l.idAccount,
      debitAmount: l.debitAmount,
      creditAmount: l.creditAmount,
      descriptionLine: l.descriptionLine.trim() || null,
    }));

    const base = {
      idFiscalPeriod:    this.formFiscalPeriod(),
      idCurrency:        this.formCurrency(),
      numberEntry:       this.formNumber().trim(),
      dateEntry:         this.formDate(),
      descriptionEntry:  this.formDescription().trim(),
      statusEntry:       this.formStatus(),
      referenceEntry:    this.formReference().trim() || null,
      exchangeRateValue: this.formExchangeRate(),
      lines,
    };

    const id = this.editingId();
    if (id !== null) {
      this.editSave.emit({ ...base, id });
    } else {
      this.create.emit(base);
    }

    this.cancelForm();
  }

  // ── Eliminar ──────────────────────────────────────────────────────
  canEdit(entry: AccountingEntryDto): boolean   { return !entry.isLinkedToBankMovement && entry.statusEntry !== 'Anulado'; }
  canDelete(entry: AccountingEntryDto): boolean { return !entry.isLinkedToBankMovement && entry.statusEntry !== 'Anulado'; }

  askDelete(id: number): void    { this.confirmDeleteId.set(id); }
  cancelDelete(): void           { this.confirmDeleteId.set(null); }
  confirmDelete(): void {
    const id = this.confirmDeleteId();
    if (id !== null) { this.remove.emit(id); this.confirmDeleteId.set(null); }
  }

  // ── Totales por asiento (para el row detail) ──────────────────────
  getEntryTotalDebit(entry: AccountingEntryDto): number {
    return entry.lines.reduce((s, l) => s + l.debitAmount, 0);
  }

  getEntryTotalCredit(entry: AccountingEntryDto): number {
    return entry.lines.reduce((s, l) => s + l.creditAmount, 0);
  }
}
