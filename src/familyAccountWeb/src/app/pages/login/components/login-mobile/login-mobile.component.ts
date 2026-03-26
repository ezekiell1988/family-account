import {
  Component,
  ChangeDetectionStrategy,
  input,
  output,
  OnInit,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, NgForm } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { addIcons } from 'ionicons';
import {
  logInOutline,
  keyOutline,
  personOutline,
  alertCircleOutline,
  checkmarkCircleOutline,
  paperPlaneOutline,
} from 'ionicons/icons';
import {
  IonContent,
  IonGrid,
  IonRow,
  IonCol,
  IonCard,
  IonCardHeader,
  IonCardTitle,
  IonCardContent,
  IonInput,
  IonButton,
  IonIcon,
  IonSpinner,
  IonText,
  IonNote,
} from '@ionic/angular/standalone';
import { LoginPayload } from '../../login.types';

@Component({
  selector: 'app-login-mobile',
  host: { class: 'ion-page' },
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    TranslatePipe,
    IonContent,
    IonGrid,
    IonRow,
    IonCol,
    IonCard,
    IonCardHeader,
    IonCardTitle,
    IonCardContent,
    IonInput,
    IonButton,
    IonIcon,
    IonSpinner,
    IonText,
    IonNote,
  ],
  templateUrl: './login-mobile.component.html',
})
export class LoginMobileComponent implements OnInit {
  loading        = input(false);
  requestingPin  = input(false);
  errorMessage   = input('');
  pinMessage     = input('');
  currentYear    = input(new Date().getFullYear());

  requestPin  = output<string>();
  loginSubmit = output<LoginPayload>();

  // Estado local del formulario
  emailUser = '';
  token     = '';

  ngOnInit(): void {
    addIcons({
      logInOutline,
      keyOutline,
      personOutline,
      alertCircleOutline,
      checkmarkCircleOutline,
      paperPlaneOutline,
    });
  }

  onRequestPin(): void {
    if (this.emailUser) {
      this.requestPin.emit(this.emailUser);
    }
  }

  formSubmit(f: NgForm): void {
    if (!f.valid) return;
    this.loginSubmit.emit({ emailUser: this.emailUser, token: this.token });
  }
}
