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
import { logInOutline, keyOutline, personOutline, alertCircleOutline, checkmarkCircleOutline } from 'ionicons/icons';
import {
  IonContent,
  IonCard,
  IonCardHeader,
  IonCardTitle,
  IonCardContent,
  IonItem,
  IonLabel,
  IonInput,
  IonButton,
  IonIcon,
  IonSpinner,
} from '@ionic/angular/standalone';

interface LoginPayload {
  emailUser: string;
  token: string;
}

@Component({
  selector: 'app-login-mobile',
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    TranslatePipe,
    IonContent,
    IonCard,
    IonCardHeader,
    IonCardTitle,
    IonCardContent,
    IonItem,
    IonLabel,
    IonInput,
    IonButton,
    IonIcon,
    IonSpinner,
  ],
  templateUrl: './login-mobile.component.html',
  styleUrls: ['./login-mobile.component.scss'],
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
