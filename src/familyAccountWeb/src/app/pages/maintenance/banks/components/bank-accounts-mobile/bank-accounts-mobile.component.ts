import {
  Component,
  ChangeDetectionStrategy,
  input,
  output,
  signal,
  computed,
  linkedSignal,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { addIcons } from 'ionicons';
import {
  warningOutline,
  closeOutline,
  cardOutline,
  pencilOutline,
  saveOutline,
  addOutline,
  trashOutline,
  chevronDownOutline,
  chevronForwardOutline,
  albumsOutline,
} from 'ionicons/icons';
import {
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
  IonToggle,
  IonSelect,
  IonSelectOption,
  IonGrid,
  IonRow,
  IonCol,
} from '@ionic/angular/standalone';
import {
  BankAccountDto,
  BankDto,
  AccountDto,
  CurrencyDto,
  CreateBankAccountRequest,
  UpdateBankAccountRequest,
} from '../../../../../shared/models';

@Component({
  selector: 'app-bank-accounts-mobile',
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    TranslatePipe,
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
    IonToggle,
    IonSelect,
    IonSelectOption,
    IonGrid,
    IonRow,
    IonCol,
  ],
  templateUrl: './bank-accounts-mobile.component.html',
})
export class BankAccountsMobileComponent {
  // ── Inputs ──────────────────────────────────────────────────────
  bankAccounts = input<BankAccountDto[]>([]);
  banks        = input<BankDto[]>([]);
  accounts     = input<AccountDto[]>([]);
  currencies   = input<CurrencyDto[]>([]);
  isLoading    = input(false);
  deletingId   = input<number | null>(null);
  errorMessage = input('');

  // ── Outputs ──────────────────────────────────────────────────────
  refresh    = output<void>();
  create     = output<CreateBankAccountRequest>();
  editSave   = output<UpdateBankAccountRequest & { id: number }>();
  remove     = output<number>();
  clearError = output<void>();

  // ── Config ──────────────────────────────────────────────────────
  preselectedBankId = input<number | null>(null);

  // ── Estado de filtro ────────────────────────────────────────────
  filterBankId = linkedSignal<number | null>(() => this.preselectedBankId());

  // ── Estado del formulario ────────────────────────────────────────
  expandedId      = signal<number | null>(null);
  showForm        = signal(false);
  editingId       = signal<number | null>(null);
  formBankId      = signal<number | null>(null);
  formAccountId   = signal<number | null>(null);
  formCurrencyId  = signal<number | null>(null);
  formCode        = signal('');
  formNumber      = signal('');
  formHolder      = signal('');
  formIsActive    = signal(true);
  confirmDeleteId = signal<number | null>(null);

  // ── Computados ───────────────────────────────────────────────────
  isEditing   = computed(() => this.editingId() !== null);
  isFormValid = computed(() =>
    this.formBankId() !== null &&
    this.formAccountId() !== null &&
    this.formCurrencyId() !== null &&
    this.formCode().trim().length > 0 &&
    this.formNumber().trim().length > 0 &&
    this.formHolder().trim().length > 0,
  );

  filteredItems = computed(() => {
    const bankId = this.filterBankId();
    if (bankId === null) return this.bankAccounts();
    return this.bankAccounts().filter(b => b.idBank === bankId);
  });

  activeAccounts = computed(() =>
    this.accounts().filter(a => a.allowsMovements && a.isActive),
  );

  constructor() {
    addIcons({
      warningOutline,
      closeOutline,
      cardOutline,
      pencilOutline,
      saveOutline,
      addOutline,
      trashOutline,
      chevronDownOutline,
      chevronForwardOutline,
      albumsOutline,
    });
  }

  toggleExpand(id: number): void {
    this.expandedId.update(v => (v === id ? null : id));
  }

  openCreate(): void {
    this.editingId.set(null);
    this.formBankId.set(this.filterBankId());
    this.formAccountId.set(null);
    this.formCurrencyId.set(null);
    this.formCode.set('');
    this.formNumber.set('');
    this.formHolder.set('');
    this.formIsActive.set(true);
    this.showForm.set(true);
  }

  openEdit(row: BankAccountDto): void {
    this.editingId.set(row.idBankAccount);
    this.formBankId.set(row.idBank);
    this.formAccountId.set(row.idAccount);
    this.formCurrencyId.set(row.idCurrency);
    this.formCode.set(row.codeBankAccount);
    this.formNumber.set(row.accountNumber);
    this.formHolder.set(row.accountHolder);
    this.formIsActive.set(row.isActive);
    this.showForm.set(true);
  }

  cancelForm(): void { this.showForm.set(false); this.editingId.set(null); }

  submitForm(): void {
    if (!this.isFormValid()) return;
    const payload: CreateBankAccountRequest = {
      idBank:          this.formBankId()!,
      idAccount:       this.formAccountId()!,
      idCurrency:      this.formCurrencyId()!,
      codeBankAccount: this.formCode().trim().toUpperCase(),
      accountNumber:   this.formNumber().trim(),
      accountHolder:   this.formHolder().trim(),
      isActive:        this.formIsActive(),
    };
    const id = this.editingId();
    if (id !== null) {
      this.editSave.emit({ ...payload, id });
    } else {
      this.create.emit(payload);
    }
    this.cancelForm();
  }

  askDelete(id: number): void    { this.confirmDeleteId.set(id); }
  cancelDelete(): void           { this.confirmDeleteId.set(null); }

  confirmDelete(): void {
    const id = this.confirmDeleteId();
    if (id !== null) { this.remove.emit(id); this.confirmDeleteId.set(null); }
  }
}
