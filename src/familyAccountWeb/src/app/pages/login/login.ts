import { Component, OnDestroy, OnInit, Renderer2, inject, signal, ChangeDetectionStrategy } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { AppSettings, AuthService, LoggerService } from '../../service';
import { ResponsiveComponent } from '../../shared';
import { LoginWebComponent } from './components/login-web/login-web.component';
import { LoginMobileComponent } from './components/login-mobile/login-mobile.component';

interface LoginPayload {
  emailUser: string;
  token: string;
}

@Component({
  selector: 'app-login',
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './login.html',
  styleUrls: ['./login.scss'],
  standalone: true,
  imports: [LoginWebComponent, LoginMobileComponent],
})

export class LoginPage extends ResponsiveComponent implements OnInit, OnDestroy {
  // Inyección moderna con inject()
  private readonly authService = inject(AuthService);
  private readonly logger      = inject(LoggerService).getLogger('LoginPage');
  private readonly route       = inject(ActivatedRoute);

  // Estado reactivo del componente
  loading       = signal(false);
  requestingPin = signal(false);
  pinRequested  = signal(false);
  errorMessage  = signal('');
  pinMessage    = signal('');
  currentYear   = new Date().getFullYear();

  // Fondo de login: imagen estática
  private readonly fallbackBgUrl = '/assets/img/login-bg.png';
  loginBgUrl = signal<string>('');
  bgLoading  = signal(true);

  // URL de retorno después del login
  private returnUrl: string = '/';

  constructor(
    private router: Router,
    private renderer: Renderer2,
    public appSettings: AppSettings
  ) {
    super();

    this.appSettings.appEmpty = true;
    this.renderer.addClass(document.body, 'bg-white');

    // Obtener returnUrl de los query params si existe
    this.route.queryParams.subscribe(params => {
      this.returnUrl = params['returnUrl'] || '/home';
      this.logger.debug('Return URL:', this.returnUrl);
    });

    // Verificar autenticación con el backend
    this.authService.checkAuthentication().subscribe({
      next: (isAuth) => {
        if (isAuth) {
          this.logger.info('Usuario ya autenticado, redirigiendo a:', this.returnUrl);
          this.router.navigate([this.returnUrl]);
        }
      },
      error: (err) => {
        // Si hay error en la verificación, simplemente mostrar el formulario
        this.logger.debug('No autenticado, mostrar formulario');
      }
    });
  }

  override ngOnDestroy() {
    this.appSettings.appEmpty = false;
    this.renderer.removeClass(document.body, 'bg-white');
    super.ngOnDestroy();
  }

  ngOnInit(): void {
    this.loginBgUrl.set(this.fallbackBgUrl);
    this.bgLoading.set(false);
  }

  /**
   * Solicitar PIN al backend (paso 1) — llamado desde sub-componentes
   */
  onRequestPin(email: string): void {
    if (!email || this.requestingPin()) return;

    this.requestingPin.set(true);
    this.pinMessage.set('');
    this.errorMessage.set('');

    this.authService.requestLoginToken(email).subscribe({
      next: () => {
        this.pinRequested.set(true);
        this.pinMessage.set('PIN enviado a tu correo. Revisa tu bandeja de entrada.');
        this.logger.info('PIN solicitado para:', email);
      },
      error: (err) => {
        this.errorMessage.set(err.status === 404
          ? 'No existe un usuario con ese correo.'
          : 'Error al solicitar el PIN. Intenta de nuevo.');
        this.logger.error('Error solicitando PIN:', err);
      },
      complete: () => this.requestingPin.set(false),
    });
  }

  /**
   * Manejar el envío del formulario de login — llamado desde sub-componentes
   */
  onLoginSubmit(payload: LoginPayload): void {
    const { emailUser, token } = payload;

    if (!emailUser || !token) {
      this.errorMessage.set('Por favor, ingresa tu correo y el PIN.');
      return;
    }

    if (!/^[0-9]{5}$/.test(token)) {
      this.errorMessage.set('El PIN debe tener exactamente 5 dígitos numéricos.');
      return;
    }

    this.logger.info('Iniciando login...', { emailUser });
    this.loading.set(true);
    this.errorMessage.set('');

    this.authService.loginWithToken(emailUser, token).subscribe({
      next: (response) => {
        this.logger.success('Login exitoso:', response);
        this.router.navigate([this.returnUrl]);
      },
      error: (error) => {
        this.loading.set(false);
        this.logger.error('Error en login:', error);

        if (error.status === 401) {
          this.errorMessage.set('Código de usuario o token incorrecto.');
        } else if (error.status === 404) {
          this.errorMessage.set('Usuario no encontrado.');
        } else if (error.status === 500) {
          this.errorMessage.set('Error del servidor. Intenta más tarde.');
        } else if (error.error?.detail) {
          this.errorMessage.set(error.error.detail);
        } else {
          this.errorMessage.set('Error al iniciar sesión. Verifica tu conexión.');
        }
      },
    });
  }
}
