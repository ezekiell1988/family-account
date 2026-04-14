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
  playOutline, refreshOutline, calendarOutline,
  trendingUpOutline, trendingDownOutline, analyticsOutline, warningOutline, closeOutline,
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
  IonListHeader,
  IonNote,
} from '@ionic/angular/standalone';
import { HeaderComponent, FooterComponent } from '../../../../../components';
import { FinancialStatementFilter, IncomeStatementDto } from '../../../../../shared/models';

@Component({
  selector: 'app-income-statement-mobile',
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
    IonListHeader,
    IonNote,
  ],
  templateUrl: './income-statement-mobile.component.html',
})
export class IncomeStatementMobileComponent {
  isLoading    = input(false);
  errorMessage = input('');
  report       = input<IncomeStatementDto | null>(null);
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
    addIcons({ playOutline, refreshOutline, calendarOutline, trendingUpOutline, trendingDownOutline, analyticsOutline, warningOutline, closeOutline });
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
}
