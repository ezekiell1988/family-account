import { Injectable, signal, computed, effect, inject } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { tap, map, catchError, shareReplay, switchMap } from 'rxjs/operators';
import { Router } from '@angular/router';
import { environment } from '../../environments/environment';
import { LoggerService } from './logger.service';
import {
  RequestTokenRequest,
  RequestTokenResponse,
  LoginRequest,
  LoginResponse,
  LogoutResponse,
  RefreshTokenResponse,
  VerifyTokenResponse,
  UserData
} from '../shared/models';

/**
 * Servicio de autenticación modernizado con Angular 20+ Signals
 * 
 * Características:
 * - Signals para estado reactivo
 * - Computed signals para validaciones
 * - Effects para sincronización automática con localStorage
 * - 100% compatible con backend OpenAPI
 * - Tipado estricto
 * 
 * @example
 * ```typescript
 * // Inyectar servicio
 * private authService = inject(AuthService);
 * 
 * // Usar signals
 * isLoggedIn = this.authService.isAuthenticated;
 * currentUser = this.authService.currentUser;
 * 
 * // En template
 * @if (authService.isAuthenticated()) {
 *   <p>Bienvenido {{ authService.currentUser()?.nameLogin }}</p>
 * }
 * ```
 */
@Injectable({
  providedIn: 'root'
})
export class AuthService {
  // Servicios inyectados con inject() (Angular 20+)
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly logger = inject(LoggerService).getLogger('AuthService');
  
  // API base URL
  private readonly apiUrl = `${environment.apiUrl}auth/`;

  // ========================================
  // SIGNALS (Angular 20+)
  // ========================================
  
  /** Estado del usuario autenticado */
  readonly currentUser = signal<UserData | null>(this.getUserFromStorage());
  
  /** Token de autenticación JWE */
  readonly token = signal<string | null>(this.getTokenFromStorage());
  
  /** Refresh token para renovar el access token */
  readonly refreshTokenValue = signal<string | null>(localStorage.getItem('refreshToken'));

  /** Fecha de expiración del token */
  readonly tokenExpiresAt = signal<string | null>(localStorage.getItem('tokenExpiresAt'));
  
  /** Computed: estado de autenticación */
  readonly isAuthenticated = computed(() => {
    const token = this.token();
    if (!token) return false;
    
    const expiresAt = this.tokenExpiresAt();
    if (!expiresAt) return true; // Sin fecha de expiración, asumir válido
    
    const expirationDate = new Date(expiresAt);
    const now = new Date();
    
    return now < expirationDate; // Token válido si aún no expiró
  });
  
  /** Computed: verificar si el token expira pronto (< 5 min) */
  readonly isTokenExpiringSoon = computed(() => {
    const expiresAt = this.tokenExpiresAt();
    if (!expiresAt) return false;
    
    const expirationDate = new Date(expiresAt);
    const now = new Date();
    const fiveMinutesFromNow = new Date(now.getTime() + 5 * 60 * 1000);
    
    return expirationDate <= fiveMinutesFromNow && expirationDate > now;
  });
  
  /** Computed: verificar si el token está expirado */
  readonly isTokenExpired = computed(() => {
    const expiresAt = this.tokenExpiresAt();
    if (!expiresAt) return false;
    
    const expirationDate = new Date(expiresAt);
    const now = new Date();
    
    return now >= expirationDate;
  });

  constructor() {
    // Effect para sincronizar cambios en el usuario con localStorage
    effect(() => {
      const user = this.currentUser();
      if (user) {
        localStorage.setItem('userData', JSON.stringify(user));
      } else {
        localStorage.removeItem('userData');
      }
    });
    
    // Effect para sincronizar cambios en el token con localStorage
    effect(() => {
      const tokenValue = this.token();
      if (tokenValue) {
        localStorage.setItem('token', tokenValue);
      } else {
        localStorage.removeItem('token');
      }
    });

    // Effect para sincronizar el refresh token con localStorage
    effect(() => {
      const rt = this.refreshTokenValue();
      if (rt) {
        localStorage.setItem('refreshToken', rt);
      } else {
        localStorage.removeItem('refreshToken');
      }
    });
  }

  // ========================================
  // AUTHENTICATION FLOW
  // ========================================
  
  /**
   * Paso 1: Solicitar token temporal (PIN de 5 dígitos)
   * POST /api/v1/auth/request-token
   * 
   * @param codeLogin Código de usuario
   * @returns Observable con respuesta de solicitud de token
   */
  requestLoginToken(emailUser: string): Observable<RequestTokenResponse> {
    const url = `${this.apiUrl}request-pin`;
    const body = { emailUser };

    this.logger.debug('Solicitando PIN para:', emailUser);

    return this.http.post<RequestTokenResponse>(url, body).pipe(
      tap(response => {
        this.logger.debug('Respuesta solicitud PIN:', response);
      })
    );
  }

  /**
   * Paso 2: Login con token temporal (PIN de 5 dígitos)
   * POST /api/v1/auth/login
   * 
   * @param codeLogin Código de usuario
   * @param token PIN de 5 dígitos
   * @returns Observable con respuesta de login
   */
  loginWithToken(emailUser: string, pin: string): Observable<LoginResponse> {
    const url = `${this.apiUrl}login`;
    const body = { emailUser, pin };

    this.logger.debug('Iniciando sesión:', { emailUser, pin: '****' });

    return this.http.post<any>(url, body).pipe(
      switchMap((response: any) => {
        this.logger.debug('Respuesta login recibida');

        // La API devuelve: { accessToken, refreshToken, expiresAt }
        if (response?.accessToken) {
          this.token.set(response.accessToken);
          this.refreshTokenValue.set(response.refreshToken ?? null);
          if (response.expiresAt) {
            this.tokenExpiresAt.set(response.expiresAt);
            localStorage.setItem('tokenExpiresAt', response.expiresAt);
          }
          this.clearAuthCache();

          // Obtener datos del usuario desde /auth/me.json
          return this.http.get<any>(`${this.apiUrl}me.json`).pipe(
            tap(me => {
              const userData: UserData = {
                idLogin:    me.idUser,
                codeLogin:  me.codeUser,
                nameLogin:  me.nameUser,
                phoneLogin: null,
                emailLogin: me.emailUser,
                roles:      me.roles ?? []
              };
              this.currentUser.set(userData);
              this.logger.success('Sesión iniciada:', userData.nameLogin);
            }),
            map(() => ({ success: true, message: 'OK', user: this.currentUser()!, accessToken: response.accessToken, expiresAt: response.expiresAt } as LoginResponse))
          );
        }

        return of({ success: false, message: 'Login fallido', user: null as any, accessToken: null, expiresAt: null } as LoginResponse);
      })
    );
  }

  /**
   * Logout - cerrar sesión
   * DELETE /api/v1/auth/logout
   * Requiere autenticación: Bearer token o cookie de sesión
   * 
   * @param microsoftLogout Si se debe cerrar sesión en Microsoft también
   * @returns Observable con respuesta de logout
   */
  logout(microsoftLogout: boolean = false): Observable<LogoutResponse> {
    const url = `${this.apiUrl}logout?microsoft_logout=${microsoftLogout}`;
    
    this.logger.debug('Cerrando sesión...');
    
    return this.http.delete<LogoutResponse>(url, {
      withCredentials: true, // Incluir cookies
      headers: this.getAuthHeaders()
    }).pipe(
      tap({
        next: (response) => {
          this.logger.success('Sesión cerrada en servidor');
          
          // Limpiar estado local
          this.clearAuth();
          this.clearAuthCache();
          
          // Redirigir a Microsoft si es necesario
          if (response.redirect_required && response.microsoft_logout_url) {
            window.location.href = response.microsoft_logout_url;
          } else {
            this.router.navigate(['/login']);
          }
        },
        error: (error) => {
          this.logger.error('Error al cerrar sesión en servidor:', error);
          // Aunque falle el backend, limpiar sesión local
          this.clearAuth();
          this.clearAuthCache();
          this.router.navigate(['/login']);
        }
      })
    );
  }

  /**
   * Limpia la sesión localmente (logout offline)
   */
  clearSession(): void {
    this.clearAuth();
    this.router.navigate(['/login']);
  }

  /**
   * Verificar token y obtener información del usuario
   * GET /api/v1/auth/verify-token.json
   * Requiere autenticación: Bearer token
   * 
   * @returns Observable con datos del usuario y fechas del token
   */
  verifyToken(): Observable<VerifyTokenResponse> {
    const url = `${this.apiUrl}verify-token.json`;
    const headers = this.getAuthHeaders();
    
    this.logger.debug('Verificando token...');
    
    return this.http.get<VerifyTokenResponse>(url, { headers }).pipe(
      tap((response: VerifyTokenResponse) => {
        this.logger.success('Token válido, usuario:', response.user.nameLogin);
        
        // Actualizar usuario y fechas
        this.currentUser.set(response.user);
        
        if (response.expiresAt) {
          this.tokenExpiresAt.set(response.expiresAt);
          localStorage.setItem('tokenExpiresAt', response.expiresAt);
        }
      })
    );
  }

  /**
   * Refrescar token de autenticación (extender expiración)
   * PUT /api/v1/auth/refresh
   * Requiere autenticación: Bearer token o cookie de sesión
   * 
   * @returns Observable con nuevo token y fecha de expiración
   */
  refreshToken(): Observable<RefreshTokenResponse> {
    const url = `${this.apiUrl}refresh`;
    const rt = this.refreshTokenValue();

    this.logger.debug('Refrescando token...');

    return this.http.post<any>(url, { refreshToken: rt ?? '' }, {
      withCredentials: true
    }).pipe(
      tap((response: any) => {
        this.logger.success('Token refrescado exitosamente');

        // La API devuelve AuthResponse: { accessToken, refreshToken, expiresAt }
        if (response?.accessToken) {
          this.token.set(response.accessToken);
          this.refreshTokenValue.set(response.refreshToken ?? null);
          if (response.expiresAt) {
            this.tokenExpiresAt.set(response.expiresAt);
            localStorage.setItem('tokenExpiresAt', response.expiresAt);
          }
          this.logger.debug('Nueva expiración:', response.expiresAt);
        }
      }),
      map(response => ({
        success: !!response?.accessToken,
        message: response?.accessToken ? 'Token refrescado' : 'Refresh fallido',
        accessToken: response?.accessToken ?? null,
        expiresAt:   response?.expiresAt   ?? null
      } as RefreshTokenResponse))
    );
  }

  // ========================================
  // MÉTODOS PÚBLICOS (COOKIE-BASED AUTH)
  // ========================================
  
  // Cache para evitar múltiples llamadas simultáneas
  private authCheckCache$: Observable<boolean> | null = null;
  private authCheckCacheTime: number = 0;
  private readonly AUTH_CACHE_DURATION = 5000; // 5 segundos de cache
  
  /**
   * Verificar si el usuario está autenticado (basado en cookies)
   * Este método hace un request al backend para verificar la cookie httpOnly
   * Incluye cache para evitar múltiples llamadas simultáneas
   * 
   * @returns Observable<boolean> - true si está autenticado
   */
  checkAuthentication(): Observable<boolean> {
    // Si no hay token local, no tiene sentido llamar al backend
    if (!this.getToken()) {
      this.logger.debug('⚡ Sin token local — no autenticado');
      return of(false);
    }

    const now = Date.now();
    
    // Si hay un cache válido, retornarlo
    if (this.authCheckCache$ && (now - this.authCheckCacheTime) < this.AUTH_CACHE_DURATION) {
      this.logger.debug('⚡ Usando cache de autenticación');
      return this.authCheckCache$;
    }
    
    const url = `${this.apiUrl}check`;
    
    this.logger.debug('🔍 Verificando autenticación con backend...');
    this.logger.debug('📡 URL completa:', url);
    this.logger.debug('🍪 withCredentials: true');
    
    // Crear nuevo Observable con cache compartido
    this.authCheckCache$ = this.http.get<VerifyTokenResponse>(url, {
      withCredentials: true // Importante: incluir cookies httpOnly
    }).pipe(
      map(response => {
        this.logger.debug('📥 Respuesta cruda del backend:', JSON.stringify(response));
        
        // El backend devuelve: { success: boolean, isValid: boolean, user, expiresAt, message }
        const isAuth = response.success && response.isValid;
        this.logger.debug(`📊 Estado de autenticación: ${isAuth} (success=${response.success}, isValid=${response.isValid})`);
        this.logger.debug('📊 Mensaje del backend:', response.message);
        this.logger.debug('📊 Usuario en respuesta:', response.user);
        
        // Si está autenticado, actualizar el usuario en el signal
        if (isAuth && response.user) {
          this.logger.debug('✅ Actualizando usuario en signal');
          this.currentUser.set(response.user);
          if (response.expiresAt) {
            this.tokenExpiresAt.set(response.expiresAt);
            localStorage.setItem('tokenExpiresAt', response.expiresAt);
          }
        }
        
        return isAuth;
      }),
      catchError(error => {
        this.logger.error('⚠️ ERROR en checkAuthentication:', error);
        this.logger.error('📊 Status HTTP:', error.status);
        this.logger.error('📊 Status Text:', error.statusText);
        this.logger.error('📊 URL:', error.url);
        this.logger.error('📊 Error completo:', JSON.stringify(error));
        this.authCheckCache$ = null; // Limpiar cache en caso de error
        return of(false); // Si hay error, asumir no autenticado
      }),
      shareReplay({ 
        bufferSize: 1, 
        refCount: true,
        windowTime: this.AUTH_CACHE_DURATION 
      }) // Compartir resultado entre múltiples suscriptores
    );
    
    this.authCheckCacheTime = now;
    
    return this.authCheckCache$;
  }
  
  /**
   * Limpiar el cache de autenticación
   * Útil después de login/logout para forzar una nueva verificación
   */
  clearAuthCache(): void {
    this.logger.debug('🗑️ Limpiando cache de autenticación');
    this.authCheckCache$ = null;
    this.authCheckCacheTime = 0;
  }

  /**
   * Obtener información del usuario autenticado desde el backend
   * Usa la cookie httpOnly para autenticación
   * 
   * @returns Observable<UserData> - Datos del usuario
   */
  getCurrentUserFromBackend(): Observable<UserData> {
    const url = `${this.apiUrl}me.json`;
    
    this.logger.debug('Obteniendo usuario actual desde backend...');
    
    return this.http.get<UserData>(url, {
      withCredentials: true // Importante: incluir cookies httpOnly
    }).pipe(
      tap(user => {
        this.logger.success('Usuario obtenido desde backend:', user.nameLogin);
        // Actualizar el signal con los datos del usuario
        this.currentUser.set(user);
      }),
      catchError(error => {
        this.logger.error('Error obteniendo usuario:', error);
        this.currentUser.set(null);
        throw error;
      })
    );
  }

  /**
   * Logout - cerrar sesión (basado en cookies)
   * DELETE /api/v1/auth/logout
   * Elimina la sesión en el backend y borra la cookie
   * 
   * @returns Observable con respuesta del logout
   */
  logoutWithCookie(): Observable<{success: boolean; message: string}> {
    const url = `${this.apiUrl}logout`;
    
    this.logger.debug('Cerrando sesión (cookie-based)...');
    
    return this.http.delete<{success: boolean; message: string}>(url, {
      withCredentials: true // Importante: incluir cookies httpOnly
    }).pipe(
      tap(() => {
        this.logger.success('Sesión cerrada correctamente');
        this.clearAuth();
        this.clearAuthCache();
        this.router.navigate(['/login']);
      }),
      catchError(error => {
        this.logger.error('Error cerrando sesión:', error);
        // Aunque falle, limpiar el frontend
        this.clearAuth();
        this.clearAuthCache();
        this.router.navigate(['/login']);
        return of({success: false, message: 'Error al cerrar sesión'});
      })
    );
  }

  /**
   * Renovar sesión activa
   * PUT /api/v1/auth/refresh
   * Extiende el tiempo de expiración de la cookie
   * 
   * @returns Observable con respuesta del refresh
   */
  refreshSessionCookie(): Observable<{success: boolean; message: string; expires_in: number}> {
    const url = `${this.apiUrl}refresh`;
    
    this.logger.debug('Renovando sesión...');
    
    return this.http.put<{success: boolean; message: string; expires_in: number}>(url, {}, {
      withCredentials: true
    }).pipe(
      tap(response => {
        if (response.success) {
          this.logger.debug('Sesión renovada, expira en:', response.expires_in, 'segundos');
        }
      })
    );
  }

  // ========================================
  // MÉTODOS PÚBLICOS (LEGACY JWT - mantener compatibilidad)
  // ========================================
  
  /**
   * Obtener el token actual
   * @returns Token JWT o null
   */
  getToken(): string | null {
    return this.token();
  }

  /**
   * Obtener el usuario actual
   * @returns Datos del usuario o null
   */
  getCurrentUser(): UserData | null {
    return this.currentUser();
  }

  /**
   * Obtener headers con autenticación
   * @returns HttpHeaders con Authorization Bearer
   */
  getAuthHeaders(): HttpHeaders {
    const token = this.getToken();
    return new HttpHeaders({
      'Authorization': `Bearer ${token}`,
      'Content-Type': 'application/json'
    });
  }

  // ========================================
  // MÉTODOS PRIVADOS
  // ========================================
  
  /**
   * Leer token desde localStorage al inicializar
   */
  private getTokenFromStorage(): string | null {
    try {
      return localStorage.getItem('token');
    } catch (error) {
      this.logger.error('Error leyendo token de localStorage:', error);
      return null;
    }
  }

  /**
   * Leer usuario desde localStorage al inicializar
   */
  private getUserFromStorage(): UserData | null {
    try {
      const userData = localStorage.getItem('userData');
      return userData ? JSON.parse(userData) : null;
    } catch (error) {
      this.logger.error('Error leyendo usuario de localStorage:', error);
      return null;
    }
  }

  /**
   * Limpiar toda la autenticación
   */
  private clearAuth(): void {
    this.logger.debug('Limpiando autenticación...');
    
    // Limpiar cache de autenticación
    this.clearAuthCache();
    
    // Limpiar signals
    this.token.set(null);
    this.refreshTokenValue.set(null);
    this.currentUser.set(null);
    this.tokenExpiresAt.set(null);
    
    // Limpiar localStorage
    localStorage.removeItem('token');
    localStorage.removeItem('refreshToken');
    localStorage.removeItem('userData');
    localStorage.removeItem('tokenExpiresAt');
    
    this.logger.debug('Autenticación limpiada');
  }
}
