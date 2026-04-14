import {
  Component,
  ChangeDetectionStrategy,
  input,
  output,
  signal,
  OnInit,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import {
  CreateBankStatementTemplateRequest,
  UpdateBankStatementTemplateRequest,
  BankMovementTypeDto,
  AccountDto,
  CostCenterDto,
} from '../../../../../shared/models';
import { BankStatementTemplatesKeywordEditorComponent } from '../bank-statement-templates-keyword-editor/bank-statement-templates-keyword-editor.component';

@Component({
  selector: 'app-bank-statement-templates-web-form',
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: true,
  imports: [CommonModule, FormsModule, BankStatementTemplatesKeywordEditorComponent],
  templateUrl: './bank-statement-templates-web-form.component.html',
})
export class BankStatementTemplatesWebFormComponent implements OnInit {
  form         = input<CreateBankStatementTemplateRequest | UpdateBankStatementTemplateRequest>({
    codeTemplate: '',
    nameTemplate: '',
    bankName: '',
    columnMappings: '',
    keywordRules: '',
    dateFormat: '',
    timeFormat: '',
    isActive: true,
    notes: '',
  });
  isEdit       = input(false);
  errorMessage = input('');
  movementTypes = input<BankMovementTypeDto[]>([]);
  accounts      = input<AccountDto[]>([]);
  costCenters   = input<CostCenterDto[]>([]);

  save   = output<CreateBankStatementTemplateRequest | UpdateBankStatementTemplateRequest>();
  cancel = output<void>();

  /** Copia mutable del formulario (necesaria para campos ngModel) */
  formLocal = signal<CreateBankStatementTemplateRequest | UpdateBankStatementTemplateRequest>({
    codeTemplate: '',
    nameTemplate: '',
    bankName: '',
    columnMappings: '',
    keywordRules: '',
    dateFormat: '',
    timeFormat: '',
    isActive: true,
    notes: '',
  });

  ngOnInit(): void {
    this.formLocal.set({ ...this.form() });
  }

  onKeywordRulesChange(json: string): void {
    this.formLocal.update(f => ({ ...f, keywordRules: json }));
  }

  onSubmit(): void {
    this.save.emit(this.formLocal());
  }
}
