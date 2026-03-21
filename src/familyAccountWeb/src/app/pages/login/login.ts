import { Component, OnDestroy, OnInit, Renderer2, inject, signal } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule, NgForm } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { TranslatePipe } from '@ngx-translate/core';
import { AppSettings, AuthService, LoggerService } from '../../service';
import { ResponsiveComponent } from '../../shared';
import { environment } from '../../../environments/environment';
import { addIcons } from 'ionicons';
import { logInOutline, keyOutline, personOutline } from 'ionicons/icons';
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
  IonSpinner
} from '@ionic/angular/standalone';

@Component({
  selector: 'app-login',
  templateUrl: './login.html',
  styleUrls: ['./login.scss'],
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
    IonSpinner
  ]
})

export class LoginPage extends ResponsiveComponent implements OnInit, OnDestroy {
  // Inyección moderna con inject()
  private readonly authService = inject(AuthService);
  private readonly logger      = inject(LoggerService).getLogger('LoginPage');
  private readonly route       = inject(ActivatedRoute);
  private readonly http        = inject(HttpClient);

  // Campos del formulario
  emailUser: string = '';
  token: string = '';

  // Estado del componente
  loading: boolean = false;
  requestingPin: boolean = false;
  pinRequested: boolean = false;
  errorMessage: string = '';
  pinMessage: string = '';
  currentYear: number = new Date().getFullYear();

  // Fondo de login: se intenta cargar desde la BD; fallback a imagen estática
  private readonly fallbackBgUrl = '/assets/img/login-bg.png';
  private bgObjectUrl: string | null = null;
  loginBgUrl = signal<string>('');
  bgLoading = signal(true);

  // URL de retorno después del login
  private returnUrl: string = '/';

  constructor(
    private router: Router, 
    private renderer: Renderer2, 
    public appSettings: AppSettings
  ) {
    super();
    
    // Registrar íconos de Ionic
    addIcons({
      logInOutline,
      keyOutline,
      personOutline
    });
    
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
    // Liberar blob URL si se creó
    if (this.bgObjectUrl) {
      URL.revokeObjectURL(this.bgObjectUrl);
      this.bgObjectUrl = null;
    }
  }

  ngOnInit(): void {
    const apiUrl = `${environment.apiUrl}Multimedia/login-background`;
    this.http.get(apiUrl, { responseType: 'blob' }).subscribe({
      next: (blob) => {
        this.bgObjectUrl = URL.createObjectURL(blob);
        this.loginBgUrl.set(this.bgObjectUrl);
        this.bgLoading.set(false);
        this.logger.debug('Login background cargado desde API');
      },
      error: () => {
        this.loginBgUrl.set(this.fallbackBgUrl);
        this.bgLoading.set(false);
        this.logger.debug('No hay login background en BD, usando fallback');
      },
    });
  }

  /**
   * Solicitar PIN al backend (paso 1)
   */
  requestPin(): void {
    if (!this.emailUser || this.requestingPin) return;

    this.requestingPin = true;
    this.pinMessage = '';
    this.errorMessage = '';

    this.authService.requestLoginToken(this.emailUser).subscribe({
      next: () => {
        this.pinRequested = true;
        this.pinMessage = 'PIN enviado a tu correo. Revisa tu bandeja de entrada.';
        this.logger.info('PIN solicitado para:', this.emailUser);
      },
      error: (err) => {
        this.errorMessage = err.status === 404
          ? 'No existe un usuario con ese correo.'
          : 'Error al solicitar el PIN. Intenta de nuevo.';
        this.logger.error('Error solicitando PIN:', err);
      },
      complete: () => { this.requestingPin = false; }
    });
  }

  /**
   * Manejar el envío del formulario de login
   */
  formSubmit(f: NgForm) {
    // Validar formulario
    if (!f.valid) {
      this.logger.warn('Formulario inválido');
      this.errorMessage = 'Por favor, completa todos los campos correctamente.';
      return;
    }
    
    // Validar campos manualmente
    if (!this.emailUser || !this.token) {
      this.logger.warn('Campos vacíos');
      this.errorMessage = 'Por favor, ingresa tu correo y el PIN.';
      return;
    }
    
    // Validar que el token tenga exactamente 5 dígitos
    if (!/^[0-9]{5}$/.test(this.token)) {
      this.logger.warn('Token inválido:', this.token);
      this.errorMessage = 'El PIN debe tener exactamente 5 dígitos numéricos.';
      return;
    }
    
    this.logger.info('Iniciando login...', { emailUser: this.emailUser });
    this.loading = true;
    this.errorMessage = '';
    
    // Llamar al servicio de autenticación
    this.authService.loginWithToken(this.emailUser, this.token).subscribe({
      next: (response) => {
        this.logger.success('Login exitoso:', response);
        
        // Redirigir al returnUrl o al home
        this.logger.info('Redirigiendo a:', this.returnUrl);
        this.router.navigate([this.returnUrl]);
      },
      error: (error) => {
        this.loading = false;
        this.logger.error('Error en login:', error);
        
        // Manejar errores específicos
        if (error.status === 401) {
          this.errorMessage = 'Código de usuario o token incorrecto.';
        } else if (error.status === 404) {
          this.errorMessage = 'Usuario no encontrado.';
        } else if (error.status === 500) {
          this.errorMessage = 'Error del servidor. Intenta más tarde.';
        } else if (error.error?.detail) {
          this.errorMessage = error.error.detail;
        } else {
          this.errorMessage = 'Error al iniciar sesión. Verifica tu conexión.';
        }
      }
    });
  }
}
