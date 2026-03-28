import {
  Component,
  ChangeDetectionStrategy,
  input,
  output,
  signal,
  computed,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { addIcons } from 'ionicons';
import {
  warningOutline,
  closeOutline,
  swapHorizontalOutline,
  pencilOutline,
  saveOutline,
  addOutline,
  trashOutline,
  chevronDownOutline,
  chevronForwardOutline,
  listOutline,
  checkmarkOutline,
  banOutline,
  documentOutline,
  documentAttachOutline,
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
  IonInput,
  IonSelect,
  IonSelectOption,
  IonGrid,
  IonRow,
  IonCol,
  IonFab,
  IonFabButton,
  IonNote,
} from '@ionic/angular/standalone';
import {
  BankMovementDto,
  BankMovementDocumentRequest,
  CreateBankMovementRequest,
  UpdateBankMovementRequest,
  BankAccountDto,
  BankMovementTypeDto,
  FiscalPeriodLookup,
} from '../../../../../shared/models';
import { HeaderComponent, FooterComponent } from '../../../../../components';

interface FormDocument {
  typeDocument: string;
  numberDocument: string;
  dateDocument: string;
  amountDocument: number;
  descriptionDocument: string;
  idAccountingEntry: number | null;
}

const STATUS_OPTIONS = ['Borrador', 'Confirmado', 'Anulado'] as const;
const DOC_TYPE_OPTIONS = ['Factura', 'Recibo', 'Transferencia', 'Cheque', 'Otro'] as const;

@Component({
  selector: 'app-bank-movements-mobile',
  host: { class: 'ion-page' },
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: true,
  imports: [
    CommonModule,
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
    IonInput,
    IonSelect,
    IonSelectOption,
    IonGrid,
    IonRow,
    IonCol,
    IonFab,
    IonFabButton,
    IonNote,
  ],
  templateUrl: './bank-movements-mobile.component.html',
})
export class BankMovementsMobileComponent {
  // ── Inputs ────────────────────────────────────────────────────────
  movements     = input<BankMovementDto[]>([]);
  isLoading     = input(false);
  deletingId    = input<number | null>(null);
  errorMessage  = input('');
  bankAccounts  = input<BankAccountDto[]>([]);
  movementTypes = input<BankMovementTypeDto[]>([]);
  fiscalPeriods = input<FiscalPeriodLookup[]>([]);

  // ── Outputs ───────────────────────────────────────────────────────
  refresh    = output<void>();
  create     = output<CreateBankMovementRequest>();
  editSave   = output<UpdateBankMovementRequest & { id: number }>();
  remove     = output<number>();
  confirm    = output<number>();
  cancel     = output<number>();
  clearError = output<void>();

  // ── Estado local ──────────────────────────────────────────────────
  expandedId      = signal<number | null>(null);
  showForm        = signal(false);
  editingId       = signal<number | null>(null);
  confirmDeleteId = signal<number | null>(null);
  confirmActionId = signal<{ id: number; action: 'confirm' | 'cancel' } | null>(null);

  // Señales del formulario
  formBankAccount  = signal(0);
  formMovementType = signal(0);
  formFiscalPeriod = signal(0);
  formNumber       = signal('');
  formDate         = signal('');
  formAmount       = signal(0);
  formExchangeRate = signal(1);
  formStatus       = signal<string>('Borrador');
  formDescription  = signal('');
  formReference    = signal('');
  formDocuments    = signal<FormDocument[]>([]);

  // ── Derivados ─────────────────────────────────────────────────────
  isEditing = computed(() => this.editingId() !== null);
  formTitle = computed(() => this.isEditing() ? 'Editar Movimiento' : 'Nuevo Movimiento');

  isFormValid = computed(() =>
    this.formBankAccount() > 0 &&
    this.formMovementType() > 0 &&
    this.formFiscalPeriod() > 0 &&
    this.formNumber().trim().length > 0 &&
    this.formDate().length > 0 &&
    this.formAmount() > 0 &&
    this.formExchangeRate() > 0 &&
    this.formDescription().trim().length > 0,
  );

  statusOptions  = STATUS_OPTIONS;
  docTypeOptions = DOC_TYPE_OPTIONS;

  constructor() {
    addIcons({
      warningOutline,
      closeOutline,
      swapHorizontalOutline,
      pencilOutline,
      saveOutline,
      addOutline,
      trashOutline,
      chevronDownOutline,
      chevronForwardOutline,
      listOutline,
      checkmarkOutline,
      banOutline,
      documentOutline,
      documentAttachOutline,
    });
  }

  // ── Helpers ───────────────────────────────────────────────────────
  getStatusColor(status: string): string {
    const map: Record<string, string> = {
      Borrador:   'medium',
      Confirmado: 'success',
      Anulado:    'danger',
    };
    return map[status] ?? 'medium';
  }

  getSignColor(sign: string): string {
    return sign === 'Cargo' ? 'primary' : 'success';
  }

  formatAmount(value: number): string {
    return (value ?? 0).toLocaleString('es-CR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
  }

  getDocTotal(mov: BankMovementDto): number {
    return (mov.documents ?? []).reduce((s, d) => s + (d.amountDocument ?? 0), 0);
  }

  canEdit(status: string): boolean   { return status === 'Borrador'; }
  canDelete(status: string): boolean { return status === 'Borrador'; }
  canConfirm(status: string): boolean { return status === 'Borrador'; }
  canCancel(status: string): boolean  { return status === 'Confirmado'; }

  toggleExpand(id: number): void {
    this.expandedId.update(v => (v === id ? null : id));
  }

  async handleRefresh(event: CustomEvent): Promise<void> {
    this.refresh.emit();
    await new Promise(r => setTimeout(r, 800));
    (event.target as HTMLIonRefresherElement).complete();
  }

  // ── Documentos ────────────────────────────────────────────────────
  addDocument(): void {
    this.formDocuments.update(docs => [
      ...docs,
      { typeDocument: 'Factura', numberDocument: '', dateDocument: '', amountDocument: 0, descriptionDocument: '', idAccountingEntry: null },
    ]);
  }

  removeDocument(index: number): void {
    this.formDocuments.update(docs => docs.filter((_, i) => i !== index));
  }

  updateDoc(index: number, field: keyof FormDocument, value: string | number | null): void {
    this.formDocuments.update(docs =>
      docs.map((d, i) => i === index ? { ...d, [field]: value } : d),
    );
  }

  // ── Formulario ────────────────────────────────────────────────────
  openCreate(): void {
    this.editingId.set(null);
    this.formBankAccount.set(0);
    this.formMovementType.set(0);
    this.formFiscalPeriod.set(0);
    this.formNumber.set('');
    this.formDate.set('');
    this.formAmount.set(0);
    this.formExchangeRate.set(1);
    this.formStatus.set('Borrador');
    this.formDescription.set('');
    this.formReference.set('');
    this.formDocuments.set([]);
    this.showForm.set(true);
  }

  openEdit(row: BankMovementDto): void {
    this.editingId.set(row.idBankMovement);
    this.formBankAccount.set(row.idBankAccount);
    this.formMovementType.set(row.idBankMovementType);
    this.formFiscalPeriod.set(row.idFiscalPeriod);
    this.formNumber.set(row.numberMovement);
    this.formDate.set(row.dateMovement);
    this.formAmount.set(row.amount);
    this.formExchangeRate.set(row.exchangeRateValue);
    this.formStatus.set(row.statusMovement);
    this.formDescription.set(row.descriptionMovement);
    this.formReference.set(row.referenceMovement ?? '');
    this.formDocuments.set(
      (row.documents ?? [])
        .filter(d => d.typeDocument !== 'Asiento')
        .map(d => ({
          typeDocument:       d.typeDocument,
          numberDocument:     d.numberDocument ?? '',
          dateDocument:       d.dateDocument,
          amountDocument:     d.amountDocument,
          descriptionDocument: d.descriptionDocument ?? '',
          idAccountingEntry:  null,
        })),
    );
    this.showForm.set(true);
  }

  cancelForm(): void {
    this.showForm.set(false);
    this.editingId.set(null);
  }

  submitForm(): void {
    if (!this.isFormValid()) return;
    const documents: BankMovementDocumentRequest[] = this.formDocuments().map(d => ({
      typeDocument:        d.typeDocument,
      numberDocument:      d.numberDocument.trim() || null,
      dateDocument:        d.dateDocument,
      amountDocument:      d.amountDocument,
      descriptionDocument: d.descriptionDocument.trim() || null,
      idAccountingEntry:   null,
    }));
    const payload: CreateBankMovementRequest = {
      idBankAccount:      this.formBankAccount(),
      idBankMovementType: this.formMovementType(),
      idFiscalPeriod:     this.formFiscalPeriod(),
      numberMovement:     this.formNumber().trim(),
      dateMovement:       this.formDate(),
      amount:             this.formAmount(),
      exchangeRateValue:  this.formExchangeRate(),
      statusMovement:     this.formStatus(),
      descriptionMovement: this.formDescription().trim(),
      referenceMovement:  this.formReference().trim() || undefined,
      documents,
    };
    const id = this.editingId();
    if (id !== null) {
      this.editSave.emit({ ...payload, id });
    } else {
      this.create.emit(payload);
    }
    this.cancelForm();
  }

  askDelete(id: number): void   { this.confirmDeleteId.set(id); }
  cancelDelete(): void          { this.confirmDeleteId.set(null); }
  confirmDelete(): void {
    const id = this.confirmDeleteId();
    if (id !== null) { this.remove.emit(id); this.confirmDeleteId.set(null); }
  }

  askAction(id: number, action: 'confirm' | 'cancel'): void {
    this.confirmActionId.set({ id, action });
  }
  cancelAction(): void { this.confirmActionId.set(null); }
  doAction(): void {
    const a = this.confirmActionId();
    if (!a) return;
    if (a.action === 'confirm') this.confirm.emit(a.id);
    else this.cancel.emit(a.id);
    this.confirmActionId.set(null);
  }
}
