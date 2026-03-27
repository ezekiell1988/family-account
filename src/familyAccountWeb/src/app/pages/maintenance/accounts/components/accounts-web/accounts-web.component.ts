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
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import {
  NgxDatatableModule,
  ColumnMode,
  DatatableRowDetailDirective,
} from '@swimlane/ngx-datatable';
import { PanelComponent } from '../../../../../components';
import { AccountDto, CreateAccountRequest, UpdateAccountRequest } from '../../../../../shared/models';

const ACCOUNT_TYPES = ['Activo', 'Pasivo', 'Capital', 'Ingreso', 'Gasto', 'Control'] as const;

@Component({
  selector: 'app-accounts-web',
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: true,
  imports: [CommonModule, FormsModule, NgxDatatableModule, PanelComponent],
  templateUrl: './accounts-web.component.html',
})
export class AccountsWebComponent {
  // ── Inputs ────────────────────────────────────────────────────────
  accounts     = input<AccountDto[]>([]);
  totalCount   = input(0);
  isLoading    = input(false);
  deletingId   = input<number | null>(null);
  errorMessage = input('');

  // ── Outputs ───────────────────────────────────────────────
  refresh    = output<void>();
  create     = output<CreateAccountRequest>();
  editSave   = output<UpdateAccountRequest & { id: number }>();
  remove     = output<number>();
  clearError = output<void>();

  // ── Row detail ────────────────────────────────────────────────────
  @ViewChild(DatatableRowDetailDirective) rowDetail!: DatatableRowDetailDirective;
  private cdr = inject(ChangeDetectorRef);
  expandedId  = signal<number | null>(null);

  // ── Filtros ───────────────────────────────────────────────────────
  filterType   = signal('');
  filterSearch = signal('');

  filteredItems = computed(() => {
    let items = this.accounts();
    const type   = this.filterType();
    const search = this.filterSearch().toLowerCase().trim();
    if (type)   items = items.filter(a => a.typeAccount === type);
    if (search) items = items.filter(a =>
      a.codeAccount.toLowerCase().includes(search) ||
      a.nameAccount.toLowerCase().includes(search),
    );
    return items;
  });

  // ── Formulario ────────────────────────────────────────────────────
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

  isEditing  = computed(() => this.editingId() !== null);
  formTitle  = computed(() => this.isEditing() ? 'Editar Cuenta' : 'Nueva Cuenta');
  isFormValid = computed(() =>
    this.formCode().trim().length > 0 && this.formName().trim().length > 0,
  );

  // ── Metadata ──────────────────────────────────────────────────────
  ColumnMode   = ColumnMode;
  accountTypes = ACCOUNT_TYPES;

  // ── Helpers de display ────────────────────────────────────────────
  getTypeBadgeClass(type: string): string {
    const map: Record<string, string> = {
      Activo:  'bg-success',
      Pasivo:  'bg-danger',
      Capital: 'bg-warning text-dark',
      Ingreso: 'bg-info text-dark',
      Gasto:   'bg-secondary',
      Control: 'bg-dark',
    };
    return map[type] ?? 'bg-secondary';
  }

  getIndentStyle(level: number): string {
    return `padding-left: ${(level - 1) * 1.25}rem`;
  }

  parentOptions = computed(() =>
    this.accounts().filter(a => a.idAccount !== this.editingId()),
  );

  // ── Row detail ────────────────────────────────────────────────────
  toggleExpand(row: AccountDto): void {
    this.rowDetail.toggleExpandRow(row);
    const id = row.idAccount;
    this.expandedId.update(k => (k === id ? null : id));
    this.cdr.markForCheck();
  }

  // ── Formulario: abrir / cerrar ────────────────────────────────────
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
      codeAccount:    this.formCode().trim(),
      nameAccount:    this.formName().trim(),
      typeAccount:    this.formType(),
      levelAccount:   this.formLevel(),
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

  // ── Eliminar ──────────────────────────────────────────────────────
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
