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
  playOutline, calendarOutline, waterOutline, arrowUpOutline,
  arrowDownOutline, warningOutline, closeOutline, analyticsOutline,
} from 'ionicons/icons';
import {
  IonContent,
  IonRefresher,
  IonRefresherContent,
  IonSpinner,
  IonText,
  IonCard,
  IonCardHeader,
  IonCardTitle,
  IonCardContent,
  IonButton,
  IonIcon,
  IonInput,
  IonSelect,
  IonSelectOption,
  IonGrid,
  IonRow,
  IonCol,
  IonItem,
  IonLabel,
  IonList,
  IonNote,
} from '@ionic/angular/standalone';
import { HeaderComponent, FooterComponent } from '../../../../../components';
import { FinancialStatementFilter, CashFlowStatementDto } from '../../../../../shared/models';

@Component({
  selector: 'app-cash-flow-mobile',
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
    IonCard,
    IonCardHeader,
    IonCardTitle,
    IonCardContent,
    IonButton,
    IonIcon,
    IonInput,
    IonSelect,
    IonSelectOption,
    IonGrid,
    IonRow,
    IonCol,
    IonItem,
    IonLabel,
    IonList,
    IonNote,
  ],
  templateUrl: './cash-flow-mobile.component.html',
})
export class CashFlowMobileComponent {
  isLoading    = input(false);
  errorMessage = input('');
  report       = input<CashFlowStatementDto | null>(null);
  filter       = input<FinancialStatementFilter>({});

  filterChange = output<FinancialStatementFilter>();
  refresh      = output<void>();
  clearError   = output<void>();

  filterYear  = signal<number | null>(new Date().getFullYear());
  filterMonth = signal<number | null>(null);

  months = [
    { value: 1, label: 'Enero' }, { value: 2, label: 'Febrero' },
    { value: 3, label: 'Marzo' }, { value: 4, label: 'Abril' },
    { value: 5, label: 'Mayo' }, { value: 6, label: 'Junio' },
    { value: 7, label: 'Julio' }, { value: 8, label: 'Agosto' },
    { value: 9, label: 'Septiembre' }, { value: 10, label: 'Octubre' },
    { value: 11, label: 'Noviembre' }, { value: 12, label: 'Diciembre' },
  ];

  constructor() {
    addIcons({ playOutline, calendarOutline, waterOutline, arrowUpOutline, arrowDownOutline, warningOutline, closeOutline, analyticsOutline });
  }

  applyFilter(): void {
    const f: FinancialStatementFilter = {};
    if (this.filterYear() != null && this.filterMonth() != null) {
      f.year = this.filterYear()!;
      f.month = this.filterMonth()!;
    } else if (this.filterYear() != null) {
      f.year = this.filterYear()!;
    }
    this.filterChange.emit(f);
  }

  async handleRefresh(event: CustomEvent): Promise<void> {
    this.refresh.emit();
    await new Promise(r => setTimeout(r, 800));
    (event.target as HTMLIonRefresherElement).complete();
  }

  formatCurrency(value: number): string {
    return value.toLocaleString('es-CR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
  }

  changeColor(value: number): string {
    return value > 0 ? 'success' : value < 0 ? 'danger' : 'medium';
  }
}
