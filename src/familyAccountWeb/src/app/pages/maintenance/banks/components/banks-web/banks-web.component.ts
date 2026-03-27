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
import { BankDto, CreateBankRequest, UpdateBankRequest } from '../../../../../shared/models';

@Component({
  selector: 'app-banks-web',
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: true,
  imports: [CommonModule, FormsModule, NgxDatatableModule, PanelComponent],
  templateUrl: './banks-web.component.html',
})
export class BanksWebComponent {
  banks        = input<BankDto[]>([]);
  totalCount   = input(0);
  isLoading    = input(false);
  deletingId   = input<number | null>(null);
  errorMessage = input('');

  refresh    = output<void>();
  create     = output<CreateBankRequest>();
  editSave   = output<UpdateBankRequest & { id: number }>();
  remove     = output<number>();
  clearError = output<void>();

  filterSearch    = signal('');
  filterActive    = signal('');
  showForm        = signal(false);
  editingId       = signal<number | null>(null);
  formCode        = signal('');
  formName        = signal('');
  formIsActive    = signal(true);
  confirmDeleteId = signal<number | null>(null);

  isEditing  = computed(() => this.editingId() !== null);
  formTitle  = computed(() => this.isEditing() ? 'Editar Banco' : 'Nuevo Banco');
  isFormValid = computed(() =>
    this.formCode().trim().length > 0 && this.formName().trim().length > 0,
  );

  filteredItems = computed(() => {
    let items = this.banks();
    const search = this.filterSearch().toLowerCase().trim();
    const active = this.filterActive();
    if (search) items = items.filter(b =>
      b.codeBank.toLowerCase().includes(search) ||
      b.nameBank.toLowerCase().includes(search),
    );
    if (active === 'true')  items = items.filter(b => b.isActive);
    if (active === 'false') items = items.filter(b => !b.isActive);
    return items;
  });

  ColumnMode = ColumnMode;

  openCreate(): void {
    this.editingId.set(null);
    this.formCode.set('');
    this.formName.set('');
    this.formIsActive.set(true);
    this.showForm.set(true);
  }

  openEdit(row: BankDto): void {
    this.editingId.set(row.idBank);
    this.formCode.set(row.codeBank);
    this.formName.set(row.nameBank);
    this.formIsActive.set(row.isActive);
    this.showForm.set(true);
  }

  cancelForm(): void {
    this.showForm.set(false);
    this.editingId.set(null);
  }

  submitForm(): void {
    if (!this.isFormValid()) return;
    const payload: CreateBankRequest = {
      codeBank: this.formCode().trim().toUpperCase(),
      nameBank: this.formName().trim(),
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

  askDelete(id: number): void { this.confirmDeleteId.set(id); }
  cancelDelete(): void { this.confirmDeleteId.set(null); }

  confirmDelete(): void {
    const id = this.confirmDeleteId();
    if (id !== null) {
      this.remove.emit(id);
      this.confirmDeleteId.set(null);
    }
  }
}
