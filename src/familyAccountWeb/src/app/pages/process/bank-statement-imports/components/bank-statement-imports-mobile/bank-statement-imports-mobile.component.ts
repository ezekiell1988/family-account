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
  BankStatementImportDto,
  BankStatementTransactionDto,
  BankStatementTemplateDto,
  BankAccountDto,
  BankMovementTypeDto,
  ClassifyTransactionRequest,
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

  // ── Estado local ──────────────────────────────────────────────────
  showUploadForm     = signal(false);
  selectedAccountId  = signal<number | null>(null);
  selectedTemplateId = signal<number | null>(null);
  selectedFile       = signal<File | null>(null);
  selectedFileName   = signal('');
  classifyMap        = signal<Record<number, number>>({});

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

  setClassifyValue(idTx: number, value: number): void {
    this.classifyMap.update(m => ({ ...m, [idTx]: value }));
  }

  getClassifyValue(idTx: number, current: number | null): number | null {
    const m = this.classifyMap();
    return m[idTx] !== undefined ? m[idTx] : current;
  }

  submitClassify(tx: BankStatementTransactionDto): void {
    const idBankMovementType = this.getClassifyValue(
      tx.idBankStatementTransaction,
      tx.idBankMovementType,
    );
    if (!idBankMovementType) return;
    this.classify.emit({ id: tx.idBankStatementTransaction, req: { idBankMovementType } });
    this.classifyMap.update(m => {
      const updated = { ...m };
      delete updated[tx.idBankStatementTransaction];
      return updated;
    });
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
