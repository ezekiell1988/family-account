import {
  Component,
  ChangeDetectionStrategy,
  input,
  output,
  OnInit,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { addIcons } from 'ionicons';
import {
  homeOutline,
  personCircleOutline,
  logOutOutline,
  shieldCheckmarkOutline,
} from 'ionicons/icons';
import {
  IonContent,
  IonCard,
  IonCardContent,
  IonIcon,
  IonBadge,
  IonButton,
  IonText,
} from '@ionic/angular/standalone';
import { HeaderComponent, FooterComponent } from '../../../../components';
import { UserData } from '../../../../shared/models';

@Component({
  selector: 'app-home-mobile',
  host: { class: 'ion-page' },
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: true,
  imports: [
    CommonModule,
    HeaderComponent,
    FooterComponent,
    IonContent,
    IonCard,
    IonCardContent,
    IonIcon,
    IonBadge,
    IonButton,
    IonText,
  ],
  templateUrl: './home-mobile.component.html',
})
export class HomeMobileComponent implements OnInit {
  currentUser     = input<UserData | null>(null);
  rolesLabel      = input('');
  nameCompany     = input('N/D');
  logoutRequested = output<void>();

  ngOnInit(): void {
    addIcons({
      homeOutline,
      personCircleOutline,
      logOutOutline,
      shieldCheckmarkOutline,
    });
  }
}
