import {
  Component,
  ChangeDetectionStrategy,
  input,
  output,
  signal,
  computed,
} from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { addIcons } from 'ionicons';
import {
  warningOutline, closeOutline, swapHorizontalOutline, pencilOutline,
  saveOutline, addOutline, trashOutline, chevronDownOutline,
  chevronForwardOutline, albumsOutline,
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
  IonToggle,
  IonGrid,
  IonRow,
  IonCol,
  IonFab,
  IonFabButton,
} from '@ionic/angular/standalone';
import { HeaderComponent, FooterComponent } from '../../../../../components';
import {
  BankMovementTypeDto,
  CreateBankMovementTypeRequest,
  UpdateBankMovementTypeRequest,
  AccountDto,
} from '../../../../../shared/models';

const MOVEMENT_SIGNS = ['Cargo', 'Abono'] as const;

@Component({
  selector: 'app-bank-movement-types-mobile',
  host: { class: 'ion-page' },
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: true,
  imports: [
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
    IonSelect,
    IonSelectOption,
    IonToggle,
    IonGrid,
    IonRow,
    IonCol,
    IonFab,
    IonFabButton,
  ],
  templateUrl: './bank-movement-types-mobile.component.html',
})
export class BankMovementTypesMobileComponent {
  items        = input<BankMovementTypeDto[]>([]);
  isLoading    = input(false);
  deletingId   = input<number | null>(null);
  errorMessage = input('');
  accounts     = input<AccountDto[]>([]);

  refresh    = output<void>();
  create     = output<CreateBankMovementTypeRequest>();
  editSave   = output<UpdateBankMovementTypeRequest & { id: number }>();
  remove     = output<number>();
  clearError = output<void>();

  movementSigns   = MOVEMENT_SIGNS;
  expandedId      = signal<number | null>(null);
  showForm        = signal(false);
  editingId       = signal<number | null>(null);
  formCode        = signal('');
  formName        = signal('');
  formAccountId   = signal<number | null>(null);
  formSign        = signal('Cargo');
  formIsActive    = signal(true);
  confirmDeleteId = signal<number | null>(null);

  isEditing   = computed(() => this.editingId() !== null);
  isFormValid = computed(() =>
    this.formCode().trim().length > 0 &&
    this.formName().trim().length > 0 &&
    this.formAccountId() !== null &&
    this.formSign().length > 0,
  );

  activeAccounts = computed(() => this.accounts().filter(a => a.isActive && a.allowsMovements));

  constructor() {
    addIcons({
      warningOutline, closeOutline, swapHorizontalOutline, pencilOutline,
      saveOutline, addOutline, trashOutline, chevronDownOutline,
      chevronForwardOutline, albumsOutline,
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
    this.formAccountId.set(null);
    this.formSign.set('Cargo');
    this.formIsActive.set(true);
    this.showForm.set(true);
  }

  openEdit(item: BankMovementTypeDto): void {
    this.editingId.set(item.idBankMovementType);
    this.formCode.set(item.codeBankMovementType);
    this.formName.set(item.nameBankMovementType);
    this.formAccountId.set(item.idAccountCounterpart);
    this.formSign.set(item.movementSign);
    this.formIsActive.set(item.isActive);
    this.showForm.set(true);
  }

  cancelForm(): void {
    this.showForm.set(false);
    this.editingId.set(null);
  }

  submitForm(): void {
    if (!this.isFormValid()) return;
    const payload: CreateBankMovementTypeRequest = {
      codeBankMovementType: this.formCode().trim().toUpperCase(),
      nameBankMovementType: this.formName().trim(),
      idAccountCounterpart: this.formAccountId()!,
      movementSign: this.formSign(),
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

  askDelete(id: number): void  { this.confirmDeleteId.set(id); }
  cancelDelete(): void         { this.confirmDeleteId.set(null); }
  confirmDelete(): void {
    const id = this.confirmDeleteId();
    if (id !== null) { this.remove.emit(id); this.confirmDeleteId.set(null); }
  }

  signColor(sign: string): string {
    return sign === 'Cargo' ? 'danger' : 'primary';
  }
}
