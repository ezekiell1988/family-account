import {
  Component,
  ChangeDetectionStrategy,
  input,
  output,
  signal,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { addIcons } from 'ionicons';
import {
  cloudUploadOutline,
  listOutline,
  refreshOutline,
  checkmarkOutline,
  chevronDownOutline,
  chevronForwardOutline,
  warningOutline,
  closeOutline,
  documentTextOutline,
  pricetagOutline,
} from 'ionicons/icons';
import {
  IonContent,
  IonRefresher,
  IonRefresherContent,
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
  IonSelect,
  IonSelectOption,
  IonGrid,
  IonRow,
  IonCol,
  IonFab,
  IonFabButton,
} from '@ionic/angular/standalone';
import { HeaderComponent, FooterComponent } from '../../../../../components';
import {
  AccountDto,
  BankStatementImportDto,
  BankStatementTransactionDto,
  BankStatementTemplateDto,
  BankAccountDto,
  BankMovementTypeDto,
  BulkClassifyItem,
  CostCenterDto,
} from '../../../../../shared/models';

@Component({
  selector: 'app-bank-statement-imports-mobile',
  host: { class: 'ion-page' },
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    HeaderComponent,
    FooterComponent,
    IonContent,
    IonRefresher,
    IonRefresherContent,
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
    IonSelect,
    IonSelectOption,
    IonGrid,
    IonRow,
    IonCol,
    IonFab,
    IonFabButton,
  ],
  templateUrl: './bank-statement-imports-mobile.component.html',
})
export class BankStatementImportsMobileComponent {
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
  costCenters       = input<CostCenterDto[]>([]);
  selectedImportId  = input<number | null>(null);
  pendingCount      = input(0);
  isLoadingCatalogs = input(false);

  // ── Outputs ─────────────────────────────────────────────
  refresh       = output<void>();
  upload        = output<{ idBankAccount: number; idTemplate: number; file: File }>();
  expand        = output<BankStatementImportDto>();
  batchClassify = output<BulkClassifyItem[]>();
  clearError    = output<void>();

  // ── Estado local ──────────────────────────────────────────────────
  showUploadForm     = signal(false);
  selectedAccountId  = signal<number | null>(null);
  selectedTemplateId = signal<number | null>(null);
  selectedFile       = signal<File | null>(null);
  selectedFileName   = signal('');
  classifyTypeMap       = signal<Record<number, number>>({});
  classifyAccMap        = signal<Record<number, number | null>>({});
  classifyCostCenterMap = signal<Record<number, number | null>>({}); // idTx → idCostCenter
  learnKeywordMap       = signal<Record<number, boolean>>({}); // idTx → guardar como regla

  constructor() {
    addIcons({
      cloudUploadOutline,
      listOutline,
      refreshOutline,
      checkmarkOutline,
      chevronDownOutline,
      chevronForwardOutline,
      warningOutline,
      closeOutline,
      documentTextOutline,
      pricetagOutline,
    });
  }

  async handleRefresh(event: CustomEvent): Promise<void> {
    this.refresh.emit();
    await new Promise(r => setTimeout(r, 800));
    (event.target as HTMLIonRefresherElement).complete();
  }

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
    this.showUploadForm.set(false);
    this.selectedAccountId.set(null);
    this.selectedTemplateId.set(null);
    this.selectedFile.set(null);
    this.selectedFileName.set('');
  }

  setClassifyType(idTx: number, value: number): void {
    this.classifyTypeMap.update(m => ({ ...m, [idTx]: value }));
    const type = this.movementTypes().find(t => t.idBankMovementType === value);
    this.classifyAccMap.update(m => ({ ...m, [idTx]: type?.idAccountCounterpart ?? null }));
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

  getClassifyCostCenter(idTx: number, current: number | null): number | null {
    const m = this.classifyCostCenterMap();
    return m[idTx] !== undefined ? m[idTx] : current;
  }

  setClassifyCostCenter(idTx: number, value: number | null): void {
    this.classifyCostCenterMap.update(m => ({ ...m, [idTx]: value }));
  }

  getLearnKeyword(idTx: number): boolean {
    return this.learnKeywordMap()[idTx] ?? false;
  }

  toggleLearnKeyword(idTx: number): void {
    this.learnKeywordMap.update(m => ({ ...m, [idTx]: !this.getLearnKeyword(idTx) }));
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
    const idCostCenter = this.getClassifyCostCenter(
      tx.idBankStatementTransaction,
      tx.idCostCenter,
    );
    const learnKeyword = this.getLearnKeyword(tx.idBankStatementTransaction);
    this.batchClassify.emit([{
      idBankStatementTransaction: tx.idBankStatementTransaction,
      idBankMovementType,
      idAccountCounterpart,
      idCostCenter,
      learnKeyword,
    }]);
    this.classifyTypeMap.update(m => { const u = { ...m }; delete u[tx.idBankStatementTransaction]; return u; });
    this.classifyAccMap.update(m => { const u = { ...m }; delete u[tx.idBankStatementTransaction]; return u; });
    this.classifyCostCenterMap.update(m => { const u = { ...m }; delete u[tx.idBankStatementTransaction]; return u; });
    this.learnKeywordMap.update(m => { const u = { ...m }; delete u[tx.idBankStatementTransaction]; return u; });
  }

  submitBatchClassify(): void {
    const items = this.transactions()
      .filter(tx => !tx.idAccountingEntry) // no re-clasificar si ya tiene asiento contable
      .map(tx => {
        const idType = this.getClassifyType(tx.idBankStatementTransaction, tx.idBankMovementType);
        if (!idType) return null;
        const idAcc    = this.getClassifyAccount(tx.idBankStatementTransaction, tx.idAccountCounterpart);
        const idCostCenter = this.getClassifyCostCenter(tx.idBankStatementTransaction, tx.idCostCenter);
        const learnKeyword = this.getLearnKeyword(tx.idBankStatementTransaction);
        const item: BulkClassifyItem = {
          idBankStatementTransaction: tx.idBankStatementTransaction,
          idBankMovementType:         idType,
          idAccountCounterpart:       idAcc,
          idCostCenter,
          learnKeyword,
        };
        return item;
      })
      .filter((x): x is NonNullable<typeof x> => x !== null);
    if (items.length === 0) return;
    this.batchClassify.emit(items);
    this.classifyTypeMap.set({});
    this.classifyAccMap.set({});
    this.classifyCostCenterMap.set({});
    this.learnKeywordMap.set({});
  }

  statusColor(status: string): string {
    switch (status) {
      case 'Completado': return 'success';
      case 'Error':      return 'danger';
      case 'Procesando': return 'warning';
      default:           return 'medium';
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
