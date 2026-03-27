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
import { NgxDatatableModule, ColumnMode } from '@swimlane/ngx-datatable';
import { PanelComponent } from '../../../../../components';
import {
  BankAccountDto,
  BankDto,
  AccountDto,
  CurrencyDto,
  CreateBankAccountRequest,
  UpdateBankAccountRequest,
} from '../../../../../shared/models';

@Component({
  selector: 'app-bank-accounts-web',
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: true,
  imports: [CommonModule, FormsModule, NgxDatatableModule, PanelComponent],
  templateUrl: './bank-accounts-web.component.html',
})
export class BankAccountsWebComponent {
  // ── Inputs ──────────────────────────────────────────────────────
  bankAccounts = input<BankAccountDto[]>([]);
  totalCount   = input(0);
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

  // ── Estado de filtros ────────────────────────────────────────────
  filterBankId    = signal<number | null>(null);
  filterSearch    = signal('');
  filterActive    = signal('');

  // ── Estado del formulario ────────────────────────────────────────
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

  ColumnMode = ColumnMode;

  // ── Computados ───────────────────────────────────────────────────
  isEditing  = computed(() => this.editingId() !== null);
  formTitle  = computed(() =>
    this.isEditing() ? 'Editar Cuenta Bancaria' : 'Nueva Cuenta Bancaria',
  );
  isFormValid = computed(() =>
    this.formBankId() !== null &&
    this.formAccountId() !== null &&
    this.formCurrencyId() !== null &&
    this.formCode().trim().length > 0 &&
    this.formNumber().trim().length > 0 &&
    this.formHolder().trim().length > 0,
  );

  filteredItems = computed(() => {
    let items = this.bankAccounts();
    const bankId = this.filterBankId();
    const search = this.filterSearch().toLowerCase().trim();
    const active = this.filterActive();
    if (bankId !== null) items = items.filter(b => b.idBank === bankId);
    if (search) {
      items = items.filter(b =>
        b.codeBankAccount.toLowerCase().includes(search) ||
        b.accountNumber.toLowerCase().includes(search) ||
        b.accountHolder.toLowerCase().includes(search) ||
        b.nameAccount.toLowerCase().includes(search),
      );
    }
    if (active === 'true')  items = items.filter(b => b.isActive);
    if (active === 'false') items = items.filter(b => !b.isActive);
    return items;
  });

  selectedBankName = computed(() => {
    const id = this.filterBankId();
    if (id === null) return null;
    return this.banks().find(b => b.idBank === id)?.nameBank ?? null;
  });

  // ── Acciones del formulario ──────────────────────────────────────
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

  cancelForm(): void {
    this.showForm.set(false);
    this.editingId.set(null);
  }

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
    if (id !== null) {
      this.remove.emit(id);
      this.confirmDeleteId.set(null);
    }
  }

  onFilterBankChange(value: string): void {
    this.filterBankId.set(value ? +value : null);
  }
}
