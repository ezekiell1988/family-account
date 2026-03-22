import {
  Component,
  ChangeDetectionStrategy,
  CUSTOM_ELEMENTS_SCHEMA,
  input,
  output,
  signal,
  computed,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { AccountDto, CreateAccountRequest, UpdateAccountRequest } from '../../../../shared/models';

const ACCOUNT_TYPES = ['Activo', 'Pasivo', 'Capital', 'Ingreso', 'Gasto', 'Control'] as const;

@Component({
  selector: 'app-accounts-mobile',
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: true,
  imports: [CommonModule, FormsModule, TranslatePipe],
  schemas: [CUSTOM_ELEMENTS_SCHEMA],
  templateUrl: './accounts-mobile.component.html',
  styleUrls: ['./accounts-mobile.component.scss'],
})
export class AccountsMobileComponent {
  // ── Inputs ────────────────────────────────────────────────────────
  accounts   = input<AccountDto[]>([]);
  isLoading  = input(false);
  deletingId = input<number | null>(null);

  // ── Outputs ───────────────────────────────────────────────────────
  refresh  = output<void>();
  create   = output<CreateAccountRequest>();
  editSave = output<UpdateAccountRequest & { id: number }>();
  remove   = output<number>();

  // ── Estado local ──────────────────────────────────────────────────
  expandedId      = signal<number | null>(null);
  showForm        = signal(false);
  editingId       = signal<number | null>(null);
  formCode        = signal('');
  formName        = signal('');
  formType        = signal('Activo');
  formLevel       = signal(1);
  formParentId    = signal<number | null>(null);
  formAllowsMov   = signal(true);
  formIsActive    = signal(true);
  confirmDeleteId = signal<number | null>(null);

  isEditing   = computed(() => this.editingId() !== null);
  isFormValid = computed(() =>
    this.formCode().trim().length > 0 && this.formName().trim().length > 0,
  );

  accountTypes = ACCOUNT_TYPES;

  parentOptions = computed(() =>
    this.accounts().filter(a => a.idAccount !== this.editingId()),
  );

  // ── Helpers de display ────────────────────────────────────────────
  getTypeBadgeClass(type: string): string {
    const map: Record<string, string> = {
      Activo:  'success',
      Pasivo:  'danger',
      Capital: 'warning',
      Ingreso: 'info',
      Gasto:   'medium',
      Control: 'dark',
    };
    return map[type] ?? 'medium';
  }

  toggleExpand(id: number): void {
    this.expandedId.update(v => (v === id ? null : id));
  }

  async handleRefresh(event: CustomEvent): Promise<void> {
    this.refresh.emit();
    await new Promise(r => setTimeout(r, 800));
    (event.target as HTMLIonRefresherElement).complete();
  }

  // ── Formulario ────────────────────────────────────────────────────
  openCreate(): void {
    this.editingId.set(null);
    this.formCode.set('');
    this.formName.set('');
    this.formType.set('Activo');
    this.formLevel.set(1);
    this.formParentId.set(null);
    this.formAllowsMov.set(true);
    this.formIsActive.set(true);
    this.showForm.set(true);
  }

  openEdit(row: AccountDto): void {
    this.editingId.set(row.idAccount);
    this.formCode.set(row.codeAccount);
    this.formName.set(row.nameAccount);
    this.formType.set(row.typeAccount);
    this.formLevel.set(row.levelAccount);
    this.formParentId.set(row.idAccountParent);
    this.formAllowsMov.set(row.allowsMovements);
    this.formIsActive.set(row.isActive);
    this.showForm.set(true);
  }

  cancelForm(): void {
    this.showForm.set(false);
    this.editingId.set(null);
  }

  submitForm(): void {
    if (!this.isFormValid()) return;
    const payload: CreateAccountRequest = {
      codeAccount:     this.formCode().trim(),
      nameAccount:     this.formName().trim(),
      typeAccount:     this.formType(),
      levelAccount:    this.formLevel(),
      idAccountParent: this.formParentId(),
      allowsMovements:  this.formAllowsMov(),
      isActive:         this.formIsActive(),
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
