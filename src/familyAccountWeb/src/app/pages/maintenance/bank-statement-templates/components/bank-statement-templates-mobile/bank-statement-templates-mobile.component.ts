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
  addOutline, pencilOutline, trashOutline,
  chevronDownOutline, chevronForwardOutline,
  albumsOutline, warningOutline, closeOutline, saveOutline,
} from 'ionicons/icons';
import {
  IonContent,
  IonRefresher,
  IonRefresherContent,
  IonSpinner,
  IonText,
  IonButton,
  IonIcon,
  IonGrid,
  IonRow,
  IonCol,
} from '@ionic/angular/standalone';
import { HeaderComponent, FooterComponent } from '../../../../../components';

@Component({
  selector: 'app-bank-statement-templates-mobile',
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
    IonButton,
    IonIcon,
    IonGrid,
    IonRow,
    IonCol,
  ],
  templateUrl: './bank-statement-templates-mobile.component.html',
})
export class BankStatementTemplatesMobileComponent {
  loading      = input(false);
  errorMessage = input('');
  refresh      = output<void>();

  constructor() {
    addIcons({
      addOutline, pencilOutline, trashOutline,
      chevronDownOutline, chevronForwardOutline,
      albumsOutline, warningOutline, closeOutline, saveOutline,
    });
  }
}
