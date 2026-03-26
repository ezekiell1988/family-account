import {
  Component,
  ChangeDetectionStrategy,
  input,
  output,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, NgForm } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { LoginPayload } from '../../login.types';

@Component({
  selector: 'app-login-web',
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: true,
  imports: [CommonModule, FormsModule, TranslatePipe],
  templateUrl: './login-web.component.html',
})
export class LoginWebComponent {
  loading        = input(false);
  requestingPin  = input(false);
  errorMessage   = input('');
  pinMessage     = input('');
  currentYear    = input(new Date().getFullYear());
  loginBgUrl     = input('');
  bgLoading      = input(true);

  requestPin  = output<string>();
  loginSubmit = output<LoginPayload>();

  // Estado local del formulario
  emailUser = '';
  token     = '';

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
