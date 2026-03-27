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
import { addIcons } from 'ionicons';
import {
  warningOutline,
  closeOutline,
  bookOutline,
  pencilOutline,
  saveOutline,
  addOutline,
  trashOutline,
  chevronDownOutline,
  chevronForwardOutline,
  listOutline,
  checkmarkCircleOutline,
  alertCircleOutline,
} from 'ionicons/icons';
import {
  IonContent,
  IonRefresher,
  IonRefresherContent,
  IonSpinner,
  IonText,
  IonList,
  IonItem,
  IonLabel,
  IonCard,
  IonCardHeader,
  IonCardTitle,
  IonCardContent,
  IonBadge,
  IonButton,
  IonIcon,
  IonInput,
  IonSelect,
  IonSelectOption,
  IonGrid,
  IonRow,
  IonCol,
  IonFab,
  IonFabButton,
  IonNote,
} from '@ionic/angular/standalone';
import {
  AccountingEntryDto,
  AccountingEntryLineRequest,
  CreateAccountingEntryRequest,
  UpdateAccountingEntryRequest,
  FiscalPeriodLookup,
  CurrencyLookup,
  AccountDto,
} from '../../../../../shared/models';
import { HeaderComponent, FooterComponent } from '../../../../../components';

interface FormLine {
  idAccount: number;
  debitAmount: number;
  creditAmount: number;
  descriptionLine: string;
}

const STATUS_OPTIONS = ['Borrador', 'Publicado', 'Anulado'] as const;

@Component({
  selector: 'app-accounting-entries-mobile',
  host: { class: 'ion-page' },
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    HeaderComponent,
    FooterComponent,
    IonContent,
    IonRefresher,
    IonRefresherContent,
    IonSpinner,
    IonText,
    IonList,
    IonItem,
    IonLabel,
    IonCard,
    IonCardHeader,
    IonCardTitle,
    IonCardContent,
    IonBadge,
    IonButton,
    IonIcon,
    IonInput,
    IonSelect,
    IonSelectOption,
    IonGrid,
    IonRow,
    IonCol,
    IonFab,
    IonFabButton,
    IonNote,
  ],
  templateUrl: './accounting-entries-mobile.component.html',
})
export class AccountingEntriesMobileComponent {
  // ── Inputs ────────────────────────────────────────────────────────
  entries       = input<AccountingEntryDto[]>([]);
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

  // ── Estado local ──────────────────────────────────────────────────
  expandedId      = signal<number | null>(null);
  showForm        = signal(false);
  editingId       = signal<number | null>(null);
  confirmDeleteId = signal<number | null>(null);

  // Señales del formulario (cabecera)
  formFiscalPeriod  = signal(0);
  formCurrency      = signal(0);
  formNumber        = signal('');
  formDate          = signal('');
  formDescription   = signal('');
  formStatus        = signal<string>('Borrador');
  formReference     = signal('');
  formExchangeRate  = signal(1);

  // Líneas del formulario
  formLines = signal<FormLine[]>([]);

  // ── Derivados ─────────────────────────────────────────────────────
  isEditing  = computed(() => this.editingId() !== null);
  formTitle  = computed(() => this.isEditing() ? 'Editar Asiento' : 'Nuevo Asiento');

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
    this.isBalanced(),
  );

  movementAccounts = computed(() =>
    this.accounts().filter(a => a.allowsMovements && a.isActive),
  );

  statusOptions = STATUS_OPTIONS;

  constructor() {
    addIcons({
      warningOutline,
      closeOutline,
      bookOutline,
      pencilOutline,
      saveOutline,
      addOutline,
      trashOutline,
      chevronDownOutline,
      chevronForwardOutline,
      listOutline,
      checkmarkCircleOutline,
      alertCircleOutline,
    });
  }

  // ── Helpers de display ────────────────────────────────────────────
  getStatusColor(status: string): string {
    const map: Record<string, string> = {
      Borrador:  'medium',
      Publicado: 'success',
      Anulado:   'danger',
    };
    return map[status] ?? 'medium';
  }

  getEntryTotalDebit(entry: AccountingEntryDto): number {
    return entry.lines.reduce((s, l) => s + (l.debitAmount || 0), 0);
  }

  formatAmount(value: number): string {
    return value.toLocaleString('es-CR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
  }

  toggleExpand(id: number): void {
    this.expandedId.update(v => (v === id ? null : id));
  }

  async handleRefresh(event: CustomEvent): Promise<void> {
    this.refresh.emit();
    await new Promise(r => setTimeout(r, 800));
    (event.target as HTMLIonRefresherElement).complete();
  }

  // ── Líneas del formulario ─────────────────────────────────────────
  addLine(): void {
    this.formLines.update(lines => [
      ...lines,
      { idAccount: 0, debitAmount: 0, creditAmount: 0, descriptionLine: '' },
    ]);
  }

  removeLine(index: number): void {
    this.formLines.update(lines => lines.filter((_, i) => i !== index));
  }

  updateLineAccount(index: number, value: number): void {
    this.formLines.update(lines =>
      lines.map((l, i) => i === index ? { ...l, idAccount: value } : l),
    );
  }

  updateLineDebit(index: number, value: number): void {
    this.formLines.update(lines =>
      lines.map((l, i) => i === index ? { ...l, debitAmount: value, creditAmount: 0 } : l),
    );
  }

  updateLineCredit(index: number, value: number): void {
    this.formLines.update(lines =>
      lines.map((l, i) => i === index ? { ...l, creditAmount: value, debitAmount: 0 } : l),
    );
  }

  updateLineDescription(index: number, value: string): void {
    this.formLines.update(lines =>
      lines.map((l, i) => i === index ? { ...l, descriptionLine: value } : l),
    );
  }

  // ── Formulario ────────────────────────────────────────────────────
  openCreate(): void {
    this.editingId.set(null);
    this.formFiscalPeriod.set(0);
    this.formCurrency.set(0);
    this.formNumber.set('');
    this.formDate.set('');
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
    this.formLines.set(row.lines.map(l => ({
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
      idAccount:       l.idAccount,
      debitAmount:     l.debitAmount,
      creditAmount:    l.creditAmount,
      descriptionLine: l.descriptionLine.trim() || undefined,
    }));
    const payload: CreateAccountingEntryRequest = {
      idFiscalPeriod:    this.formFiscalPeriod(),
      idCurrency:        this.formCurrency(),
      numberEntry:       this.formNumber().trim(),
      dateEntry:         this.formDate(),
      descriptionEntry:  this.formDescription().trim(),
      statusEntry:       this.formStatus(),
      referenceEntry:    this.formReference().trim() || undefined,
      exchangeRateValue: this.formExchangeRate(),
      lines,
    };
    const id = this.editingId();
    if (id !== null) {
      this.editSave.emit({ ...payload, id });
    } else {
      this.create.emit(payload);
    }
    this.cancelForm();
  }

  askDelete(id: number): void {
    this.confirmDeleteId.set(id);
  }

  cancelDelete(): void {
    this.confirmDeleteId.set(null);
  }

  confirmDelete(): void {
    const id = this.confirmDeleteId();
    if (id !== null) {
      this.remove.emit(id);
      this.confirmDeleteId.set(null);
    }
  }
}
