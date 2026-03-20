import { HttpInterceptorFn, HttpErrorResponse, HttpRequest, HttpEvent } from '@angular/common/http';
import { inject } from '@angular/core';
import { throwError, BehaviorSubject, Observable } from 'rxjs';
import { catchError, filter, take, switchMap } from 'rxjs/operators';
import { AuthService } from '../../service/auth.service';

let isRefreshing = false;
const refreshTokenSubject = new BehaviorSubject<string | null>(null);

/**
 * Interceptor funcional de autenticación (Angular 17+)
 * - Agrega withCredentials: true para enviar cookies httpOnly automáticamente
 * - Agrega token Bearer SOLO si está disponible y NO es un endpoint de auth
 * - Maneja errores 401 refrescando el token automáticamente
 */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);

  // SIEMPRE agregar withCredentials para enviar cookies httpOnly
  let clonedReq = req.clone({
    withCredentials: true
  });

  // No agregar token Bearer a rutas públicas ni a endpoints de autenticación
  if (!isPublicRoute(clonedReq.url) && !isAuthEndpoint(clonedReq.url)) {
    const token = authService.getToken();
    if (token) {
      clonedReq = addToken(clonedReq, token);
    }
  }

  return next(clonedReq).pipe(
    catchError((error) => {
      if (error instanceof HttpErrorResponse && error.status === 401) {
        // Token/Cookie inválido o expirado
        return handle401Error(clonedReq, next, authService);
      }
      
      return throwError(() => error);
    })
  );
};

/**
 * Agregar token al header Authorization
 */
function addToken(request: HttpRequest<unknown>, token: string): HttpRequest<unknown> {
  return request.clone({
    setHeaders: {
      Authorization: `Bearer ${token}`
    }
  });
}

/**
 * Verificar si la ruta es pública (no requiere autenticación)
 */
function isPublicRoute(url: string): boolean {
  const publicRoutes = [
    '/auth/login-token',
    '/auth/login',
    'config.json'
  ];
  
  return publicRoutes.some(route => url.includes(route));
}

/**
 * Verificar si es un endpoint de autenticación que usa cookies httpOnly
 * Estos endpoints NO deben tener el header Authorization Bearer
 */
function isAuthEndpoint(url: string): boolean {
  const authEndpoints = [
    '/auth/check',
    '/auth/refresh',
    '/auth/logout'
  ];
  
  return authEndpoints.some(route => url.includes(route));
}

/**
 * Manejar error 401 - intentar refrescar token
 */
function handle401Error(
  request: HttpRequest<unknown>, 
  next: (req: HttpRequest<unknown>) => Observable<HttpEvent<unknown>>, 
  authService: AuthService
): Observable<HttpEvent<unknown>> {
  if (!isRefreshing) {
    isRefreshing = true;
    refreshTokenSubject.next(null);

    return authService.refreshToken().pipe(
      switchMap((response: any) => {
        isRefreshing = false;
        const newToken = response.accessToken ?? response.token ?? authService.getToken();
        refreshTokenSubject.next(newToken);
        
        // Reintentar la solicitud original
        // Solo agregar token Bearer si NO es un endpoint de auth
        if (isAuthEndpoint(request.url)) {
          return next(request);
        } else {
          return next(addToken(request, newToken));
        }
      }),
      catchError((err) => {
        isRefreshing = false;
        
        // Si falla el refresh, cerrar sesión
        authService.clearSession();
        
        return throwError(() => err);
      })
    );
  } else {
    // Si ya se está refrescando, esperar a que termine
    return refreshTokenSubject.pipe(
      filter((token): token is string => token != null),
      take(1),
      switchMap(token => {
        return next(addToken(request, token));
      })
    );
  }
}
