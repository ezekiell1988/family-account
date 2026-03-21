import { ApplicationConfig, provideZoneChangeDetection, provideAppInitializer, inject } from '@angular/core';
import { provideRouter, withHashLocation } from '@angular/router';
import { provideAnimations } from '@angular/platform-browser/animations';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { Title } from '@angular/platform-browser';
import { provideIonicAngular } from '@ionic/angular/standalone';
import { provideTranslateService } from '@ngx-translate/core';
import { provideTranslateHttpLoader } from '@ngx-translate/http-loader';
import { routes } from './app.routes';
import { AppSettings } from './service/app-settings.service';
import { AppVariablesService } from './service/app-variables.service';
import { AppMenuService } from './service/app-menus.service';
import { PlatformDetectorService } from './service/platform-detector.service';
import { AuthService, LoggerService } from './service';
import { authInterceptor } from './shared/interceptors';

/**
 * Factory para inicializar la configuración remota ANTES de que arranque la aplicación
 * Este es el patrón recomendado por Angular para inicialización asíncrona
 * También valida y refresca el token de autenticación si está próximo a expirar
 * Y inicializa el Device ID + registra el dispositivo en el backend
 */
async function initializeAppConfig(appSettings: AppSettings, authService: AuthService, loggerService: LoggerService, appVariables: AppVariablesService): Promise<void> {
  const logger = loggerService.getLogger('AppInitializer');

    // 1. Cargar configuración remota y registrar el dispositivo en el mismo paso
    const deviceId = await appVariables.getDeviceId();
    const configUrl = deviceId ? `/health/${encodeURIComponent(deviceId)}.json` : '/health.json';
    const maxRetries = 3;
    let retryCount = 0;

    while (retryCount < maxRetries) {
      try {
        const response = await fetch(configUrl, { credentials: 'include' });
        
        if (!response.ok) {
          throw new Error(`HTTP ${response.status}: ${response.statusText}`);
        }
        
        const config = await response.json();
        
        if (config.status !== 'ok') {
          throw new Error('La respuesta del servidor no fue exitosa');
        }
        
        // Inyectar configuración directamente en AppSettings
        appSettings.remoteConfigCharge = true;
        appSettings.nameCompany = config.nameCustomer || 'N/D';
        appSettings.sloganCompany = config.sloganCustomer || 'N/D';
        appSettings.apiVersion = config.apiVersion || 'N/D';
        appSettings.configLoaded = true;
        
        break;
        
      } catch (error) {        retryCount++;
        logger.error(`Error al cargar configuración (intento ${retryCount}/${maxRetries}):`, error);
        
        if (retryCount < maxRetries) {
          await new Promise(resolve => setTimeout(resolve, 1000));
        } else {
          logger.warn('Usando configuración por defecto después de múltiples intentos fallidos');
          appSettings.remoteConfigCharge = false;
          appSettings.nameCompany = 'N/D';
          appSettings.sloganCompany = 'N/D';
          appSettings.apiVersion = 'N/D';
          appSettings.configLoaded = true;
        }
      }
    }

    // 2. Validar y refrescar token de autenticación si es necesario
    try {
      const token = authService.getToken();
      
      if (token) {
        logger.debug('Token detectado en inicialización');
        
        // Si el token está expirado, limpiar sesión
        if (authService.isTokenExpired()) {
          logger.warn('Token expirado - limpiando sesión');
          authService.clearSession();
          return;
        }
        
        // Si el token está próximo a expirar, intentar refrescarlo
        if (authService.isTokenExpiringSoon()) {
          logger.debug('Token próximo a expirar - intentando refrescar');
          
          try {
            const refreshResponse = await new Promise<any>((resolve, reject) => {
              authService.refreshToken().subscribe({
                next: (response) => resolve(response),
                error: (error) => reject(error)
              });
            });
            
            if (refreshResponse?.success) {
              logger.success('Token refrescado exitosamente en inicialización');
            }
          } catch (refreshError) {
            logger.error('Error al refrescar token en inicialización:', refreshError);
          }
        } else {
          logger.debug('Token válido y vigente');
        }
      }
    } catch (error) {
      logger.error('Error validando token en inicialización:', error);
    }
}

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes, withHashLocation()),
    provideAnimations(),
    provideHttpClient(withInterceptors([authInterceptor])),
    provideTranslateService({ fallbackLang: 'es' }),
    provideTranslateHttpLoader({ prefix: './assets/i18n/', suffix: '.json' }),
    provideIonicAngular({
      // Configuración de Ionic
      mode: 'ios', // Usar estilo iOS para consistencia
      animated: true,
      // Configurar la ruta de assets de Ionicons
      innerHTMLTemplatesEnabled: true,
      // Los iconos se cargarán desde CDN de Ionicons
    }),
    Title,
    AppSettings,
    AppVariablesService,
    AppMenuService,
    PlatformDetectorService,
    AuthService,
    // provideAppInitializer: Forma moderna (no deprecada) de APP_INITIALIZER
    // Se ejecuta ANTES de que la app arranque
    // IMPORTANTE: Usar inject() para obtener la instancia singleton del servicio
    provideAppInitializer(() => {
      const appSettings = inject(AppSettings);
      const authService = inject(AuthService);
      const loggerService = inject(LoggerService);
      const appVariables = inject(AppVariablesService);
      return initializeAppConfig(appSettings, authService, loggerService, appVariables);
    })
  ]
};
