import { Injectable, inject } from '@angular/core';
import { Router, CanActivate, ActivatedRouteSnapshot, RouterStateSnapshot, UrlTree } from '@angular/router';
import { Observable, of } from 'rxjs';
import { map, catchError, take } from 'rxjs/operators';
import { LoggerService, AuthService } from '../../service';

@Injectable({
  providedIn: 'root'
})
export class AuthGuard implements CanActivate {
  private readonly logger = inject(LoggerService).getLogger('AuthGuard');
  private readonly authService = inject(AuthService);
  
  constructor(
    private router: Router
  ) {}

  canActivate(
    route: ActivatedRouteSnapshot,
    state: RouterStateSnapshot
  ): Observable<boolean | UrlTree> | Promise<boolean | UrlTree> | boolean | UrlTree {
    
    this.logger.debug('🔒 AuthGuard ejecutado para:', state.url);
    
    // SIEMPRE permitir acceso a /login para evitar bucles
    if (state.url === '/login' || state.url.startsWith('/login?')) {
      this.logger.debug('✅ Ruta de login - acceso permitido');
      return true;
    }
    
    // Si la URL contiene access_token, es porque venimos del callback de Microsoft
    // Permitir acceso temporalmente mientras el login.ts procesa la redirección
    if (state.url.includes('access_token=')) {
      this.logger.debug('✅ Access token en URL detectado - permitiendo acceso temporal');
      return true;
    }
    
    // Primero verificar el signal local (más rápido)
    if (this.authService.isAuthenticated()) {
      this.logger.debug('✅ Usuario autenticado (signal local) - acceso permitido');
      return true;
    }
    
    // Si no está autenticado localmente, verificar con el backend
    // Usar take(1) para completar el Observable automáticamente después del primer valor
    this.logger.debug('🔍 NO autenticado localmente - verificando con backend...');
    this.logger.debug('📍 Ruta actual:', state.url);
    this.logger.debug('📍 Llamando a /api/v1/auth/check');
    
    return this.authService.checkAuthentication().pipe(
      take(1), // ⚡ Importante: completa el Observable después del primer valor
      map(isAuth => {
        this.logger.debug('📥 Respuesta de checkAuthentication:', isAuth);
        
        if (isAuth) {
          this.logger.debug('✅ Usuario autenticado (backend) - acceso permitido');
          return true;
        } else {
          this.logger.warn('❌ Usuario NO autenticado - redirigiendo a /login');
          this.logger.debug('🔄 Creando redirección con returnUrl:', state.url);
          return this.router.createUrlTree(['/login'], {
            queryParams: { returnUrl: state.url }
          });
        }
      }),
      catchError(error => {
        this.logger.error('❌ Error verificando autenticación:', error);
        this.logger.error('📊 Status del error:', error.status);
        this.logger.error('📊 Mensaje del error:', error.message);
        return of(this.router.createUrlTree(['/login'], {
          queryParams: { returnUrl: state.url }
        }));
      })
    );
  }
}
