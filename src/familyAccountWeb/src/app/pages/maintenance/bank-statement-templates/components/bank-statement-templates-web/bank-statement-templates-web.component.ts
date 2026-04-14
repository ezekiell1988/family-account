import {
  Component,
  ChangeDetectionStrategy,
  input,
  output,
  signal,
  computed,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  BankStatementTemplateDto,
  CreateBankStatementTemplateRequest,
  UpdateBankStatementTemplateRequest,
  BankMovementTypeDto,
  AccountDto,
  CostCenterDto,
} from '../../../../../shared/models';
import { BankStatementTemplatesListWebComponent } from '../bank-statement-templates-list-web/bank-statement-templates-list-web.component';
import { BankStatementTemplatesWebFormComponent } from '../bank-statement-templates-web-form/bank-statement-templates-web-form.component';

const EMPTY_FORM: CreateBankStatementTemplateRequest = {
  codeTemplate: '',
  nameTemplate: '',
  bankName: '',
  columnMappings: '',
  keywordRules: '',
  dateFormat: '',
  timeFormat: '',
  isActive: true,
  notes: '',
};

@Component({
  selector: 'app-bank-statement-templates-web',
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: true,
  imports: [CommonModule, BankStatementTemplatesListWebComponent, BankStatementTemplatesWebFormComponent],
  templateUrl: './bank-statement-templates-web.component.html',
})
export class BankStatementTemplatesWebComponent {
  items         = input<BankStatementTemplateDto[]>([]);
  isLoading     = input(false);
  errorMessage  = input('');
  movementTypes = input<BankMovementTypeDto[]>([]);
  accounts      = input<AccountDto[]>([]);
  costCenters   = input<CostCenterDto[]>([]);

  refresh  = output<void>();
  create   = output<CreateBankStatementTemplateRequest>();
  editSave = output<UpdateBankStatementTemplateRequest & { id: number }>();
  remove   = output<number>();

  showForm  = signal(false);
  editingId = signal<number | null>(null);
  formData  = signal<CreateBankStatementTemplateRequest | UpdateBankStatementTemplateRequest>({ ...EMPTY_FORM });

  isEditing = computed(() => this.editingId() !== null);

  openCreate(): void {
    this.editingId.set(null);
    this.formData.set({ ...EMPTY_FORM });
    this.showForm.set(true);
  }

  openEdit(id: number): void {
    const item = this.items().find(i => i.idBankStatementTemplate === id);
    if (!item) return;
    this.editingId.set(id);
    this.formData.set({
      codeTemplate:   item.codeTemplate,
      nameTemplate:   item.nameTemplate,
      bankName:       item.bankName,
      columnMappings: item.columnMappings,
      keywordRules:   item.keywordRules ?? '',
      dateFormat:     item.dateFormat   ?? '',
      timeFormat:     item.timeFormat   ?? '',
      isActive:       item.isActive,
      notes:          item.notes        ?? '',
    });
    this.showForm.set(true);
  }

  cancelForm(): void {
    this.showForm.set(false);
    this.editingId.set(null);
  }

  submitForm(payload: CreateBankStatementTemplateRequest | UpdateBankStatementTemplateRequest): void {
    const id = this.editingId();
    if (id !== null) {
      this.editSave.emit({ ...payload, id });
    } else {
      this.create.emit(payload as CreateBankStatementTemplateRequest);
    }
    this.showForm.set(false);
    this.editingId.set(null);
  }
}
