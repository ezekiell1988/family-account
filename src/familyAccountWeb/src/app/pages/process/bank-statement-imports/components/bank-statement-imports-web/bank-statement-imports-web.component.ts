import {
  Component,
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  ViewChild,
  inject,
  input,
  output,
  signal,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import {
  NgxDatatableModule,
  ColumnMode,
  DatatableRowDetailDirective,
} from '@swimlane/ngx-datatable';
import { PanelComponent } from '../../../../../components';
import {
  AccountDto,
  BankStatementImportDto,
  BankStatementTransactionDto,
  BankStatementTemplateDto,
  BankAccountDto,
  BankMovementTypeDto,
  BulkClassifyItem,
  ClassifyTransactionRequest,
} from '../../../../../shared/models';

@Component({
  selector: 'app-bank-statement-imports-web',
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: true,
  imports: [CommonModule, FormsModule, NgxDatatableModule, PanelComponent],
  templateUrl: './bank-statement-imports-web.component.html',
})
export class BankStatementImportsWebComponent {
  @ViewChild(DatatableRowDetailDirective) rowDetail!: DatatableRowDetailDirective;

  private readonly cdr = inject(ChangeDetectorRef);

  // ── Inputs ────────────────────────────────────────────────────────
  isLoading         = input(false);
  isUploading       = input(false);
  isBulkClassifying = input(false);
  errorMessage      = input('');
  imports           = input<BankStatementImportDto[]>([]);
  transactions      = input<BankStatementTransactionDto[]>([]);
  templates         = input<BankStatementTemplateDto[]>([]);
  bankAccounts      = input<BankAccountDto[]>([]);
  movementTypes     = input<BankMovementTypeDto[]>([]);
  accounts          = input<AccountDto[]>([]);
  selectedImportId  = input<number | null>(null);

  // ── Outputs ─────────────────────────────────────────────
  refresh       = output<void>();
  upload        = output<{ idBankAccount: number; idTemplate: number; file: File }>();
  expand        = output<BankStatementImportDto>();
  classify      = output<{ id: number; req: ClassifyTransactionRequest }>();
  batchClassify = output<BulkClassifyItem[]>();
  clearError    = output<void>();

  ColumnMode = ColumnMode;

  // ── Estado del formulario de upload ──────────────────────────────
  selectedAccountId  = signal<number | null>(null);
  selectedTemplateId = signal<number | null>(null);
  selectedFile       = signal<File | null>(null);
  selectedFileName   = signal<string>('');

  onFileChange(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file  = input.files?.[0] ?? null;
    this.selectedFile.set(file);
    this.selectedFileName.set(file?.name ?? '');
  }

  canUpload(): boolean {
    return (
      this.selectedAccountId() !== null &&
      this.selectedTemplateId() !== null &&
      this.selectedFile() !== null &&
      !this.isUploading()
    );
  }

  submitUpload(): void {
    const idBankAccount = this.selectedAccountId();
    const idTemplate    = this.selectedTemplateId();
    const file          = this.selectedFile();
    if (!idBankAccount || !idTemplate || !file) return;
    this.upload.emit({ idBankAccount, idTemplate, file });
    // Limpiar formulario tras envío
    this.selectedAccountId.set(null);
    this.selectedTemplateId.set(null);
    this.selectedFile.set(null);
    this.selectedFileName.set('');
  }

  // ── Estado de clasificación en la tabla de transacciones ─────────
  classifyingId    = signal<number | null>(null);
  classifyTypeMap  = signal<Record<number, number>>({}); // idTx → idBankMovementType
  classifyAccMap   = signal<Record<number, number | null>>({}); // idTx → idAccountCounterpart

  setClassifyType(idTx: number, value: number): void {
    this.classifyTypeMap.update(m => ({ ...m, [idTx]: value }));
    // Auto-rellenar cuenta contrapartida del tipo seleccionado
    const type = this.movementTypes().find(t => t.idBankMovementType === value);
    const defaultAcc = type?.idAccountCounterpart ?? null;
    this.classifyAccMap.update(m => ({ ...m, [idTx]: defaultAcc }));
  }

  setClassifyAccount(idTx: number, value: number | null): void {
    this.classifyAccMap.update(m => ({ ...m, [idTx]: value }));
  }

  getClassifyType(idTx: number, current: number | null): number | null {
    const m = this.classifyTypeMap();
    return m[idTx] !== undefined ? m[idTx] : current;
  }

  getClassifyAccount(idTx: number, current: number | null): number | null {
    const m = this.classifyAccMap();
    return m[idTx] !== undefined ? m[idTx] : current;
  }

  submitClassify(tx: BankStatementTransactionDto): void {
    const idBankMovementType = this.getClassifyType(
      tx.idBankStatementTransaction,
      tx.idBankMovementType,
    );
    if (!idBankMovementType) return;
    const idAccountCounterpart = this.getClassifyAccount(
      tx.idBankStatementTransaction,
      tx.idAccountCounterpart,
    );
    this.classifyingId.set(tx.idBankStatementTransaction);
    this.classify.emit({
      id:  tx.idBankStatementTransaction,
      req: { idBankMovementType, idAccountCounterpart },
    });
    setTimeout(() => {
      this.classifyingId.set(null);
      this.classifyTypeMap.update(m => { const u = { ...m }; delete u[tx.idBankStatementTransaction]; return u; });
      this.classifyAccMap.update(m => { const u = { ...m }; delete u[tx.idBankStatementTransaction]; return u; });
      this.cdr.markForCheck();
    }, 800);
  }

  submitBatchClassify(): void {
    const txs = this.transactions();
    if (txs.length === 0) return;
    const items = txs
      .filter(tx => !tx.idAccountingEntry) // no re-clasificar si ya tiene asiento contable
      .map(tx => {
        const idType = this.getClassifyType(tx.idBankStatementTransaction, tx.idBankMovementType);
        if (!idType) return null;
        const idAcc    = this.getClassifyAccount(tx.idBankStatementTransaction, tx.idAccountCounterpart);
        const isManual = this.classifyTypeMap()[tx.idBankStatementTransaction] !== undefined;
        const item: BulkClassifyItem = {
          idBankStatementTransaction: tx.idBankStatementTransaction,
          idBankMovementType:         idType,
          idAccountCounterpart:       idAcc,
          learnKeyword:               isManual,
        };
        return item;
      })
      .filter((x): x is NonNullable<typeof x> => x !== null);

    if (items.length === 0) return;
    this.batchClassify.emit(items);
    // Limpiar estado temporal
    this.classifyTypeMap.set({});
    this.classifyAccMap.set({});
    this.cdr.markForCheck();
  }

  // ── Helpers de display ────────────────────────────────────────────
  statusBadge(status: string): string {
    switch (status) {
      case 'Completado': return 'badge bg-success';
      case 'Error':      return 'badge bg-danger';
      case 'Procesando': return 'badge bg-warning text-dark';
      default:           return 'badge bg-secondary';
    }
  }

  formatDate(dateStr: string): string {
    if (!dateStr) return '';
    return new Date(dateStr).toLocaleDateString('es-CR');
  }

  formatAmount(amount: number | null): string {
    if (amount == null) return '—';
    return amount.toLocaleString('es-CR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
  }
}
