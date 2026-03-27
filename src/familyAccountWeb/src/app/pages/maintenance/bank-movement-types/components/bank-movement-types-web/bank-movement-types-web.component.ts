import {
  Component,
  ChangeDetectionStrategy,
  input,
  output,
  signal,
  computed,
} from '@angular/core';
import { NgxDatatableModule, ColumnMode } from '@swimlane/ngx-datatable';
import { TranslatePipe } from '@ngx-translate/core';
import { PanelComponent } from '../../../../../components';
import {
  BankMovementTypeDto,
  CreateBankMovementTypeRequest,
  UpdateBankMovementTypeRequest,
  AccountDto,
} from '../../../../../shared/models';

const MOVEMENT_SIGNS = ['Cargo', 'Abono'] as const;

@Component({
  selector: 'app-bank-movement-types-web',
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: true,
  imports: [NgxDatatableModule, TranslatePipe, PanelComponent],
  templateUrl: './bank-movement-types-web.component.html',
})
export class BankMovementTypesWebComponent {
  items        = input<BankMovementTypeDto[]>([]);
  totalCount   = input(0);
  isLoading    = input(false);
  deletingId   = input<number | null>(null);
  errorMessage = input('');
  accounts     = input<AccountDto[]>([]);

  refresh    = output<void>();
  create     = output<CreateBankMovementTypeRequest>();
  editSave   = output<UpdateBankMovementTypeRequest & { id: number }>();
  remove     = output<number>();
  clearError = output<void>();

  movementSigns = MOVEMENT_SIGNS;

  filterSearch    = signal('');
  filterActive    = signal('');
  filterSign      = signal('');
  showForm        = signal(false);
  editingId       = signal<number | null>(null);
  formCode        = signal('');
  formName        = signal('');
  formAccountId   = signal<number | null>(null);
  formSign        = signal('Cargo');
  formIsActive    = signal(true);
  confirmDeleteId = signal<number | null>(null);

  isEditing  = computed(() => this.editingId() !== null);
  isFormValid = computed(() =>
    this.formCode().trim().length > 0 &&
    this.formName().trim().length > 0 &&
    this.formAccountId() !== null &&
    this.formSign().length > 0,
  );

  activeAccounts = computed(() => this.accounts().filter(a => a.isActive && a.allowsMovements));

  filteredItems = computed(() => {
    let items = this.items();
    const search = this.filterSearch().toLowerCase().trim();
    const active = this.filterActive();
    const sign   = this.filterSign();
    if (search) items = items.filter(i =>
      i.codeBankMovementType.toLowerCase().includes(search) ||
      i.nameBankMovementType.toLowerCase().includes(search),
    );
    if (active === 'true')  items = items.filter(i => i.isActive);
    if (active === 'false') items = items.filter(i => !i.isActive);
    if (sign) items = items.filter(i => i.movementSign === sign);
    return items;
  });

  ColumnMode = ColumnMode;

  openCreate(): void {
    this.editingId.set(null);
    this.formCode.set('');
    this.formName.set('');
    this.formAccountId.set(null);
    this.formSign.set('Cargo');
    this.formIsActive.set(true);
    this.showForm.set(true);
  }

  openEdit(row: BankMovementTypeDto): void {
    this.editingId.set(row.idBankMovementType);
    this.formCode.set(row.codeBankMovementType);
    this.formName.set(row.nameBankMovementType);
    this.formAccountId.set(row.idAccountCounterpart);
    this.formSign.set(row.movementSign);
    this.formIsActive.set(row.isActive);
    this.showForm.set(true);
  }

  cancelForm(): void {
    this.showForm.set(false);
    this.editingId.set(null);
  }

  submitForm(): void {
    if (!this.isFormValid()) return;
    const payload: CreateBankMovementTypeRequest = {
      codeBankMovementType: this.formCode().trim().toUpperCase(),
      nameBankMovementType: this.formName().trim(),
      idAccountCounterpart: this.formAccountId()!,
      movementSign: this.formSign(),
      isActive: this.formIsActive(),
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

  signBadgeClass(sign: string): string {
    return sign === 'Cargo' ? 'bg-danger' : 'bg-primary';
  }
}
