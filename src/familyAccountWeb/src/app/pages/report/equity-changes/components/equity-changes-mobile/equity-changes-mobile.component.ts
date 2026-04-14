import {
  Component,
  ChangeDetectionStrategy,
  input,
  output,
  signal,
} from '@angular/core';
import {
  IonContent, IonList, IonItem,
  IonLabel, IonNote, IonCard, IonCardHeader, IonCardTitle, IonCardContent,
  IonGrid, IonRow, IonCol, IonButton, IonIcon, IonSpinner, IonText,
  IonSelect, IonSelectOption, IonRefresher, IonRefresherContent, IonInput,
} from '@ionic/angular/standalone';
import { addIcons } from 'ionicons';
import { pieChartOutline, calendarOutline, closeOutline, warningOutline, playOutline, analyticsOutline } from 'ionicons/icons';
import { HeaderComponent, FooterComponent } from '../../../../../components';
import { FinancialStatementFilter, EquityStatementDto } from '../../../../../shared/models';

@Component({
  selector: 'app-equity-changes-mobile',
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: true,
  host: { class: 'ion-page' },
  imports: [
    IonContent, IonList, IonItem,
    IonLabel, IonNote, IonCard, IonCardHeader, IonCardTitle, IonCardContent,
    IonGrid, IonRow, IonCol, IonButton, IonIcon, IonSpinner, IonText,
    IonSelect, IonSelectOption, IonRefresher, IonRefresherContent, IonInput,
    HeaderComponent, FooterComponent,
  ],
  templateUrl: './equity-changes-mobile.component.html',
})
export class EquityChangesMobileComponent {
  isLoading    = input(false);
  errorMessage = input('');
  report       = input<EquityStatementDto | null>(null);
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
    addIcons({ pieChartOutline, calendarOutline, closeOutline, warningOutline, playOutline, analyticsOutline });
  }

  handleRefresh(event: CustomEvent): void {
    this.refresh.emit();
    (event.target as HTMLIonRefresherElement).complete();
  }

  formatCurrency(value: number): string {
    return new Intl.NumberFormat('es-CR', { style: 'currency', currency: 'CRC', minimumFractionDigits: 2 }).format(value);
  }

  valueColor(value: number): string {
    if (value > 0) return 'success';
    if (value < 0) return 'danger';
    return 'medium';
  }

  applyFilter(): void {
    const f: FinancialStatementFilter = {};
    if (this.filterYear() != null && this.filterMonth() != null) {
      f.year  = this.filterYear()!;
      f.month = this.filterMonth()!;
    } else if (this.filterYear() != null) {
      f.year = this.filterYear()!;
    }
    this.filterChange.emit(f);
  }
}
