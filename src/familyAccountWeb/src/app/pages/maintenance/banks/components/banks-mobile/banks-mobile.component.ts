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
import { TranslatePipe } from '@ngx-translate/core';
import { addIcons } from 'ionicons';
import {
  warningOutline, closeOutline, businessOutline, pencilOutline,
  saveOutline, addOutline, trashOutline, chevronDownOutline,
  chevronForwardOutline, albumsOutline, cardOutline,
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
  IonToggle,
  IonGrid,
  IonRow,
  IonCol,
  IonFab,
  IonFabButton,
} from '@ionic/angular/standalone';
import { BankDto, CreateBankRequest, UpdateBankRequest, BankAccountDto, AccountDto, CurrencyDto, CreateBankAccountRequest, UpdateBankAccountRequest } from '../../../../../shared/models';
import { HeaderComponent, FooterComponent } from '../../../../../components';
import { BankAccountsMobileComponent } from '../bank-accounts-mobile/bank-accounts-mobile.component';

@Component({
  selector: 'app-banks-mobile',
  host: { class: 'ion-page' },
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    TranslatePipe,
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
    IonToggle,
    IonGrid,
    IonRow,
    IonCol,
    IonFab,
    IonFabButton,
    BankAccountsMobileComponent,
  ],
  templateUrl: './banks-mobile.component.html',
})
export class BanksMobileComponent {
  banks        = input<BankDto[]>([]);
  isLoading    = input(false);
  deletingId   = input<number | null>(null);
  errorMessage = input('');

  // ── Inputs cuentas bancarias ───────────────────────────────────
  bankAccounts          = input<BankAccountDto[]>([]);
  accounts              = input<AccountDto[]>([]);
  currencies            = input<CurrencyDto[]>([]);
  bankAccountsLoading   = input(false);
  bankAccountsDeletingId = input<number | null>(null);
  bankAccountsError     = input('');
  selectedBankId        = input<number | null>(null);

  refresh    = output<void>();
  create     = output<CreateBankRequest>();
  editSave   = output<UpdateBankRequest & { id: number }>();
  remove     = output<number>();
  clearError = output<void>();

  // ── Outputs cuentas bancarias ─────────────────────────────────
  selectBank             = output<number | null>();
  createBankAccount      = output<CreateBankAccountRequest>();
  editSaveBankAccount    = output<UpdateBankAccountRequest & { id: number }>();
  removeBankAccount      = output<number>();
  clearBankAccountError  = output<void>();
  refreshBankAccounts    = output<void>();

  expandedId      = signal<number | null>(null);
  showForm        = signal(false);
  editingId       = signal<number | null>(null);
  formCode        = signal('');
  formName        = signal('');
  formIsActive    = signal(true);
  confirmDeleteId = signal<number | null>(null);

  isEditing   = computed(() => this.editingId() !== null);
  isFormValid = computed(() =>
    this.formCode().trim().length > 0 && this.formName().trim().length > 0,
  );

  selectedBankName = computed(() => {
    const id = this.selectedBankId();
    if (id === null) return '';
    return this.banks().find(b => b.idBank === id)?.nameBank ?? '';
  });

  constructor() {
    addIcons({
      warningOutline, closeOutline, businessOutline, pencilOutline,
      saveOutline, addOutline, trashOutline, chevronDownOutline,
      chevronForwardOutline, albumsOutline, cardOutline,
    });
  }

  toggleExpand(id: number): void {
    this.expandedId.update(v => (v === id ? null : id));
  }

  async handleRefresh(event: CustomEvent): Promise<void> {
    this.refresh.emit();
    await new Promise(r => setTimeout(r, 800));
    (event.target as HTMLIonRefresherElement).complete();
  }

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
