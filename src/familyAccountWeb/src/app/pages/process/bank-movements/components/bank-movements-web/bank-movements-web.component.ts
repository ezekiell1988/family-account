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
  BankMovementDto,
  BankMovementDocumentDto,
  BankMovementDocumentRequest,
  CreateBankMovementRequest,
  UpdateBankMovementRequest,
  BankAccountDto,
  FiscalPeriodLookup,
  AccountingEntryDto,
  AccountingEntryLineRequest,
  UpdateAccountingEntryRequest,
  AccountDto,
} from '../../../../../shared/models';
import { BankMovementTypeDto } from '../../../../../shared/models';

interface FormDocument {
  typeDocument:        string;
  numberDocument:      string;
  dateDocument:        string;
  amountDocument:      number;
  descriptionDocument: string;
  idAccountingEntry:   number | null;
}

interface FormEntryLine {
  idAccount:       number;
  debitAmount:     number;
  creditAmount:    number;
  descriptionLine: string;
}

const STATUS_OPTIONS        = ['Borrador', 'Confirmado', 'Anulado'] as const;
const DOC_TYPE_OPTIONS       = ['Factura', 'Recibo', 'Transferencia', 'Cheque', 'Otro'] as const;
const ENTRY_STATUS_OPTIONS   = ['Borrador', 'Publicado', 'Anulado'] as const;

@Component({
  selector: 'app-bank-movements-web',
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: true,
  imports: [CommonModule, FormsModule, NgxDatatableModule, PanelComponent, DatePipe],
  templateUrl: './bank-movements-web.component.html',
})
export class BankMovementsWebComponent {
  // ── Inputs ────────────────────────────────────────────────────────
  movements     = input<BankMovementDto[]>([]);
  totalCount    = input(0);
  isLoading     = input(false);
  deletingId    = input<number | null>(null);
  errorMessage  = input('');
  bankAccounts  = input<BankAccountDto[]>([]);
  movementTypes = input<BankMovementTypeDto[]>([]);
  fiscalPeriods = input<FiscalPeriodLookup[]>([]);
  entries       = input<AccountingEntryDto[]>([]);
  accounts      = input<AccountDto[]>([]);

  // ── Outputs ───────────────────────────────────────────────────────
  refresh       = output<void>();
  create        = output<CreateBankMovementRequest>();
  editSave      = output<UpdateBankMovementRequest & { id: number }>();
  remove        = output<number>();
  confirm       = output<number>();
  cancel        = output<number>();
  clearError    = output<void>();
  editEntrySave = output<UpdateAccountingEntryRequest & { id: number }>();

  // ── Row detail ────────────────────────────────────────────────────
  @ViewChild(DatatableRowDetailDirective) rowDetail!: DatatableRowDetailDirective;
  private cdr = inject(ChangeDetectorRef);
  expandedId  = signal<number | null>(null);

  // ── Filtros ───────────────────────────────────────────────────────
  filterStatus = signal('');
  filterSearch = signal('');

  filteredItems = computed(() => {
    let items = this.movements();
    const status = this.filterStatus();
    const search = this.filterSearch().toLowerCase().trim();
    if (status) items = items.filter(m => m.statusMovement === status);
    if (search) items = items.filter(m =>
      m.numberMovement.toLowerCase().includes(search) ||
      m.descriptionMovement.toLowerCase().includes(search) ||
      m.codeBankAccount.toLowerCase().includes(search) ||
      m.nameBankMovementType.toLowerCase().includes(search),
    );
    return items;
  });

  // ── Formulario cabecera ────────────────────────────────────────────
  showForm         = signal(false);
  editingId        = signal<number | null>(null);
  formBankAccount  = signal(0);
  formMovementType = signal(0);
  formFiscalPeriod = signal(0);
  formNumber       = signal('');
  formDate         = signal('');
  formDescription  = signal('');
  formAmount       = signal(0);
  formStatus       = signal<string>('Borrador');
  formReference    = signal('');
  formExchangeRate = signal(1);

  // ── Documentos del formulario ──────────────────────────────────────
  formDocuments = signal<FormDocument[]>([]);

  confirmDeleteId  = signal<number | null>(null);
  confirmActionId  = signal<{ id: number; action: 'confirm' | 'cancel' } | null>(null);

  isEditing = computed(() => this.editingId() !== null);
  formTitle = computed(() => this.isEditing() ? 'Editar Movimiento Bancario' : 'Nuevo Movimiento Bancario');

  isFormValid = computed(() =>
    this.formBankAccount() > 0 &&
    this.formMovementType() > 0 &&
    this.formFiscalPeriod() > 0 &&
    this.formNumber().trim().length > 0 &&
    this.formDate().length > 0 &&
    this.formDescription().trim().length > 0 &&
    this.formAmount() > 0 &&
    this.formExchangeRate() > 0,
  );

  // ── Editor de Asiento Contable ────────────────────────────────────
  showEntryForm         = signal(false);
  entryEditingId        = signal<number | null>(null);
  entryFormFiscalPeriod = signal(0);
  entryFormCurrency     = signal(0);
  entryFormCurrencyDisplay = signal('');
  entryFormNumber       = signal('');
  entryFormDate         = signal('');
  entryFormDescription  = signal('');
  entryFormStatus       = signal<string>('Borrador');
  entryFormReference    = signal('');
  entryFormExchangeRate = signal(1);
  entryFormLines        = signal<FormEntryLine[]>([]);

  entryTotalDebit  = computed(() => this.entryFormLines().reduce((s, l) => s + (l.debitAmount || 0), 0));
  entryTotalCredit = computed(() => this.entryFormLines().reduce((s, l) => s + (l.creditAmount || 0), 0));
  entryIsBalanced  = computed(() => this.entryTotalDebit() > 0 && this.entryTotalDebit() === this.entryTotalCredit());

  entryIsFormValid = computed(() =>
    this.entryFormFiscalPeriod() > 0 &&
    this.entryFormCurrency() > 0 &&
    this.entryFormNumber().trim().length > 0 &&
    this.entryFormDate().length > 0 &&
    this.entryFormDescription().trim().length > 0 &&
    this.entryFormExchangeRate() > 0 &&
    this.entryFormLines().length >= 2 &&
    this.entryIsBalanced(),
  );

  movementAccounts = computed(() => this.accounts().filter(a => a.allowsMovements && a.isActive));

  // ── Metadata ──────────────────────────────────────────────────────
  ColumnMode          = ColumnMode;
  statusOptions       = STATUS_OPTIONS;
  entryStatusOptions  = ENTRY_STATUS_OPTIONS;
  docTypeOptions      = DOC_TYPE_OPTIONS;

  // ── Helpers de display ────────────────────────────────────────────
  getStatusBadgeClass(status: string): string {
    const map: Record<string, string> = {
      Borrador:   'badge bg-warning text-dark',
      Confirmado: 'badge bg-success',
      Anulado:    'badge bg-secondary',
    };
    return map[status] ?? 'badge bg-light text-dark';
  }

  getSignBadgeClass(sign: string): string {
    return sign === 'Cargo' ? 'badge bg-danger' : 'badge bg-primary';
  }

  formatAmount(value: number): string {
    if (!value) return '—';
    return value.toLocaleString('es-CR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
  }

  totalDocuments = computed(() =>
    this.formDocuments().reduce((s, d) => s + (d.amountDocument || 0), 0),
  );

  // ── Row detail ────────────────────────────────────────────────────
  toggleExpand(row: BankMovementDto): void {
    this.rowDetail.toggleExpandRow(row);
    const id = row.idBankMovement;
    this.expandedId.update(k => (k === id ? null : id));
    this.cdr.markForCheck();
  }

  // ── Documentos ────────────────────────────────────────────────────
  addDocument(): void {
    this.formDocuments.update(ds => [
      ...ds,
      { typeDocument: 'Factura', numberDocument: '', dateDocument: this.getTodayIso(), amountDocument: 0, descriptionDocument: '', idAccountingEntry: null },
    ]);
  }

  removeDocument(index: number): void {
    this.formDocuments.update(ds => ds.filter((_, i) => i !== index));
  }

  updateDoc<K extends keyof FormDocument>(index: number, key: K, value: FormDocument[K]): void {
    this.formDocuments.update(ds => ds.map((d, i) => i === index ? { ...d, [key]: value } : d));
  }

  // ── Formulario: abrir / cerrar ────────────────────────────────────
  private getTodayIso(): string {
    return new Date().toISOString().slice(0, 10);
  }

  openCreate(): void {
    this.editingId.set(null);
    this.formBankAccount.set(0);
    this.formMovementType.set(0);
    this.formFiscalPeriod.set(0);
    this.formNumber.set('');
    this.formDate.set(this.getTodayIso());
    this.formDescription.set('');
    this.formAmount.set(0);
    this.formStatus.set('Borrador');
    this.formReference.set('');
    this.formExchangeRate.set(1);
    this.formDocuments.set([]);
    this.showForm.set(true);
  }

  openEdit(row: BankMovementDto): void {
    this.editingId.set(row.idBankMovement);
    this.formBankAccount.set(row.idBankAccount);
    this.formMovementType.set(row.idBankMovementType);
    this.formFiscalPeriod.set(row.idFiscalPeriod);
    this.formNumber.set(row.numberMovement);
    this.formDate.set(row.dateMovement);
    this.formDescription.set(row.descriptionMovement);
    this.formAmount.set(row.amount);
    this.formStatus.set(row.statusMovement);
    this.formReference.set(row.referenceMovement ?? '');
    this.formExchangeRate.set(row.exchangeRateValue);
    this.formDocuments.set(row.documents
      .filter((d: BankMovementDocumentDto) => d.idPurchaseInvoice == null)
      .map((d: BankMovementDocumentDto) => ({
        typeDocument:        d.typeDocument,
        numberDocument:      d.numberDocument ?? '',
        dateDocument:        d.dateDocument,
        amountDocument:      d.amountDocument,
        descriptionDocument: d.descriptionDocument ?? '',
        idAccountingEntry:   null,
      })));
    this.showForm.set(true);
  }

  cancelForm(): void {
    this.showForm.set(false);
    this.editingId.set(null);
  }

  submitForm(): void {
    if (!this.isFormValid()) return;

    const documents: BankMovementDocumentRequest[] = this.formDocuments().map(d => ({
      typeDocument:        d.typeDocument,
      numberDocument:      d.numberDocument.trim() || null,
      dateDocument:        d.dateDocument,
      amountDocument:      d.amountDocument,
      descriptionDocument: d.descriptionDocument.trim() || null,
      idAccountingEntry:   null,
    }));

    const base = {
      idBankAccount:       this.formBankAccount(),
      idBankMovementType:  this.formMovementType(),
      idFiscalPeriod:      this.formFiscalPeriod(),
      numberMovement:      this.formNumber().trim(),
      dateMovement:        this.formDate(),
      descriptionMovement: this.formDescription().trim(),
      amount:              this.formAmount(),
      statusMovement:      this.formStatus(),
      referenceMovement:   this.formReference().trim() || null,
      exchangeRateValue:   this.formExchangeRate(),
      documents,
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
  askDelete(id: number): void    { this.confirmDeleteId.set(id); }
  cancelDelete(): void           { this.confirmDeleteId.set(null); }
  confirmDelete(): void {
    const id = this.confirmDeleteId();
    if (id !== null) { this.remove.emit(id); this.confirmDeleteId.set(null); }
  }

  // ── Confirmar / Anular ────────────────────────────────────────────
  askAction(id: number, action: 'confirm' | 'cancel'): void {
    this.confirmActionId.set({ id, action });
  }
  cancelAction(): void { this.confirmActionId.set(null); }
  doAction(): void {
    const a = this.confirmActionId();
    if (!a) return;
    if (a.action === 'confirm') this.confirm.emit(a.id);
    else                        this.cancel.emit(a.id);
    this.confirmActionId.set(null);
  }

  // ── Totales documentos (para row detail) ──────────────────────────
  getMovementDocTotal(movement: BankMovementDto): number {
    return movement.documents.reduce((s, d) => s + d.amountDocument, 0);
  }

  getDocTotal(docs: BankMovementDocumentDto[]): number {
    return docs.reduce((s, d) => s + d.amountDocument, 0);
  }

  getEntryTotalDebitInRow(entry: AccountingEntryDto): number {
    return entry.lines.reduce((s, l) => s + l.debitAmount, 0);
  }

  getEntryTotalCreditInRow(entry: AccountingEntryDto): number {
    return entry.lines.reduce((s, l) => s + l.creditAmount, 0);
  }

  // ── Helper: obtener asiento vinculado ────────────────────────────
  getLinkedEntry(movement: BankMovementDto): AccountingEntryDto | undefined {
    if (!movement.idAccountingEntry) return undefined;
    return this.entries().find(e => e.idAccountingEntry === movement.idAccountingEntry);
  }

  // ── Helpers: documentos propios vs. documentos desde facturas ────────
  getOwnDocuments(movement: BankMovementDto): BankMovementDocumentDto[] {
    return movement.documents.filter(d => d.idPurchaseInvoice == null);
  }

  getInvoiceDocuments(movement: BankMovementDto): BankMovementDocumentDto[] {
    return movement.documents.filter(d => d.idPurchaseInvoice != null);
  }

  // ── Editor de asiento ────────────────────────────────────────────
  openEditEntry(entry: AccountingEntryDto): void {
    this.entryEditingId.set(entry.idAccountingEntry);
    this.entryFormFiscalPeriod.set(entry.idFiscalPeriod);
    this.entryFormCurrency.set(entry.idCurrency);
    this.entryFormCurrencyDisplay.set(`${entry.codeCurrency} – ${entry.nameCurrency}`);
    this.entryFormNumber.set(entry.numberEntry);
    this.entryFormDate.set(entry.dateEntry);
    this.entryFormDescription.set(entry.descriptionEntry);
    this.entryFormStatus.set(entry.statusEntry);
    this.entryFormReference.set(entry.referenceEntry ?? '');
    this.entryFormExchangeRate.set(entry.exchangeRateValue);
    this.entryFormLines.set(entry.lines.map(l => ({
      idAccount:       l.idAccount,
      debitAmount:     l.debitAmount,
      creditAmount:    l.creditAmount,
      descriptionLine: l.descriptionLine ?? '',
    })));
    this.showEntryForm.set(true);
  }

  cancelEntryForm(): void {
    this.showEntryForm.set(false);
    this.entryEditingId.set(null);
  }

  addEntryLine(): void {
    this.entryFormLines.update(ls => [
      ...ls,
      { idAccount: 0, debitAmount: 0, creditAmount: 0, descriptionLine: '' },
    ]);
  }

  removeEntryLine(index: number): void {
    this.entryFormLines.update(ls => ls.filter((_, i) => i !== index));
  }

  updateEntryLine<K extends keyof FormEntryLine>(index: number, key: K, value: FormEntryLine[K]): void {
    this.entryFormLines.update(ls => ls.map((l, i) => i === index ? { ...l, [key]: value } : l));
  }

  submitEntryForm(): void {
    if (!this.entryIsFormValid()) return;
    const id = this.entryEditingId();
    if (id === null) return;
    const lines: AccountingEntryLineRequest[] = this.entryFormLines().map(l => ({
      idAccount:       l.idAccount,
      debitAmount:     l.debitAmount,
      creditAmount:    l.creditAmount,
      descriptionLine: l.descriptionLine.trim() || null,
    }));
    const payload: UpdateAccountingEntryRequest = {
      idFiscalPeriod:   this.entryFormFiscalPeriod(),
      idCurrency:       this.entryFormCurrency(),
      numberEntry:      this.entryFormNumber().trim(),
      dateEntry:        this.entryFormDate(),
      descriptionEntry: this.entryFormDescription().trim(),
      statusEntry:      this.entryFormStatus(),
      referenceEntry:   this.entryFormReference().trim() || null,
      exchangeRateValue: this.entryFormExchangeRate(),
      lines,
    };
    this.editEntrySave.emit({ ...payload, id });
    this.cancelEntryForm();
  }

  canEdit(status: string): boolean   { return status === 'Borrador'; }
  canDelete(status: string): boolean { return status === 'Borrador'; }
  canConfirm(status: string): boolean { return status === 'Borrador'; }
  canCancel(status: string): boolean { return status !== 'Anulado'; }
}
