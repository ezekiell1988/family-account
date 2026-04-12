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
  BankStatementImportDto,
  BankStatementTransactionDto,
  BankStatementTemplateDto,
  BankAccountDto,
  BankMovementTypeDto,
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
  isLoading      = input(false);
  isUploading    = input(false);
  isClassifying  = input(false);
  errorMessage   = input('');
  imports        = input<BankStatementImportDto[]>([]);
  transactions   = input<BankStatementTransactionDto[]>([]);
  templates      = input<BankStatementTemplateDto[]>([]);
  bankAccounts   = input<BankAccountDto[]>([]);
  movementTypes  = input<BankMovementTypeDto[]>([]);
  selectedImportId = input<number | null>(null);

  // ── Outputs ───────────────────────────────────────────────────────
  refresh    = output<void>();
  upload     = output<{ idBankAccount: number; idTemplate: number; file: File }>();
  expand     = output<BankStatementImportDto>();
  classify   = output<{ id: number; req: ClassifyTransactionRequest }>();
  clearError = output<void>();

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
  classifyingId   = signal<number | null>(null);
  classifyTypeMap = signal<Record<number, number>>({}); // idTx → idBankMovementType (temporal)

  setClassifyValue(idTx: number, value: number): void {
    this.classifyTypeMap.update(m => ({ ...m, [idTx]: value }));
  }

  getClassifyValue(idTx: number, current: number | null): number | null {
    const m = this.classifyTypeMap();
    return m[idTx] !== undefined ? m[idTx] : current;
  }

  submitClassify(tx: BankStatementTransactionDto): void {
    const idBankMovementType = this.getClassifyValue(
      tx.idBankStatementTransaction,
      tx.idBankMovementType,
    );
    if (!idBankMovementType) return;
    this.classifyingId.set(tx.idBankStatementTransaction);
    this.classify.emit({
      id:  tx.idBankStatementTransaction,
      req: { idBankMovementType },
    });
    // Limpiar estado temporal tras emitir
    setTimeout(() => {
      this.classifyingId.set(null);
      this.classifyTypeMap.update(m => {
        const updated = { ...m };
        delete updated[tx.idBankStatementTransaction];
        return updated;
      });
      this.cdr.markForCheck();
    }, 800);
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
