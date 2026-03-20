import { Injectable, inject } from '@angular/core';
import { HttpRequest, HttpHandler, HttpEvent, HttpInterceptor, HttpErrorResponse } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { Router } from '@angular/router';
import { LoggerService } from '../../service';

@Injectable()
export class AuthInterceptor implements HttpInterceptor {
  private readonly logger = inject(LoggerService).getLogger('AuthInterceptor');
  private readonly router = inject(Router);

  constructor() {}

  intercept(request: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    // Las cookies httpOnly se envían automáticamente por el navegador
    // No necesitamos agregar headers de autenticación manualmente
    
    // Asegurar que las cookies se incluyan en requests (withCredentials)
    const clonedRequest = request.clone({
      withCredentials: true // Permite enviar cookies httpOnly
    });

    return next.handle(clonedRequest).pipe(
      catchError((error) => {
        if (error instanceof HttpErrorResponse && error.status === 401) {
          // Cookie inválida o expirada - redirigir a login
          return this.handle401Error(request);
        }
        
        return throwError(() => error);
      })
    );
  }

  /**
   * Manejar error 401 - redirigir a login
   */
  private handle401Error(request: HttpRequest<any>): Observable<HttpEvent<any>> {
    this.logger.warn('Error 401 detectado - cookie inválida o expirada');
    this.logger.debug('Request URL:', request.url);
    
    // Evitar bucle: solo redirigir si NO estamos ya en /login
    const currentUrl = this.router.url;
    if (!currentUrl.startsWith('/login')) {
      this.logger.info('Redirigiendo a /login');
      this.router.navigate(['/login'], {
        queryParams: { returnUrl: currentUrl }
      });
    } else {
      this.logger.debug('Ya estamos en /login, no redirigir');
    }
    
    return throwError(() => new Error('No autorizado - sesión expirada'));
  }
}
