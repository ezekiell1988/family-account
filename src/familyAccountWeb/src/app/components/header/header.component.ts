import {
  Component,
  Input,
  Output,
  EventEmitter,
  OnInit,
  OnDestroy,
  ChangeDetectorRef,
  effect,
  inject,
  signal
} from "@angular/core";
import { CommonModule } from "@angular/common";
import { HttpClient } from "@angular/common/http";
import { TranslatePipe } from '@ngx-translate/core';
import { CampaignResultModalComponent } from '../campaign-result-modal/campaign-result-modal.component';
import { ExportResultModalComponent } from '../export-result-modal/export-result-modal.component';
import { CampaignResult, ExportResult } from '../../service/notification.service';
import { 
  IonHeader,
  IonToolbar,
  IonTitle,
  IonButtons,
  IonMenuButton,
  IonBackButton,
  IonButton,
  IonIcon,
  IonBadge
} from '@ionic/angular/standalone';
import { addIcons } from 'ionicons';
import { notificationsOutline, menuOutline, languageOutline } from 'ionicons/icons';
import { AppSettings } from "../../service/app-settings.service";
import { AuthService, LoggerService, Logger, LanguageService, NotificationService } from '../../service';
import { ResponsiveComponent } from '../../shared/responsive-component.base';
import { UserData } from '../../shared/models';
import { environment } from '../../../environments/environment';

declare var slideToggle: any;

@Component({
  selector: "header",
  templateUrl: "./header.component.html",
  standalone: true,
  imports: [
    CommonModule,
    TranslatePipe,
    IonHeader,
    IonToolbar,
    IonTitle,
    IonButtons,
    IonMenuButton,
    IonBackButton,
    IonButton,
    IonIcon,
    IonBadge,
    CampaignResultModalComponent,
    ExportResultModalComponent
  ],
})
export class HeaderComponent extends ResponsiveComponent implements OnInit, OnDestroy {
  private readonly logger: Logger;
  private readonly http = inject(HttpClient);
  private readonly notificationService = inject(NotificationService);

  // Imagen de perfil del cliente: imagen estática
  private readonly fallbackPerfilUrl = 'assets/img/user.png';
  readonly perfilUrl = signal<string>('');
  readonly perfilLoading = signal(true);
  
  @Input() appSidebarTwo;
  @Input() pageTitle = ''; // Para versión móvil
  @Input() color = 'theme'; // Color del toolbar para versión móvil
  @Input() translucent = true; // Para header translúcido en iOS
  @Input() showBackButton = false; // Mostrar botón de retroceso
  @Input() backButtonHref = '/'; // URL por defecto del botón de retroceso
  @Input() showNotifications = true; // Mostrar botón de notificaciones
  @Input() hasCustomContent = false; // Indica si hay contenido personalizado (se auto-detecta en el template)
  @Output() appSidebarEndToggled = new EventEmitter<boolean>();
  @Output() appSidebarMobileToggled = new EventEmitter<boolean>();
  @Output() appSidebarEndMobileToggled = new EventEmitter<boolean>();
  @Output() backClick = new EventEmitter<void>(); // Evento para manejar el click del botón atrás
  
  currentUser: UserData | null = null;

  /** Señales del servicio de notificaciones */
  readonly notifications = this.notificationService.notifications;
  readonly unreadCount = this.notificationService.unreadCount;

  /** Control del modal de resultado de campaña */
  readonly showCampaignResultModal = signal(false);
  readonly campaignResult = signal<CampaignResult | null>(null);

  /** Control del modal de resultado de exportación */
  readonly showExportResultModal = signal(false);
  readonly exportResult = signal<ExportResult | null>(null);

  /** Modal para ver la imagen de perfil ampliada */
  readonly showPerfilModal = signal(false);

  /** Estado de carga al invalidar caché */
  readonly invalidatingCache = signal(false);

  toggleAppSidebarMobile() {
    this.appSidebarMobileToggled.emit(true);
  }

  toggleAppSidebarEnd() {
    this.appSidebarEndToggled.emit(true);
  }

  toggleAppSidebarEndMobile() {
    this.appSidebarEndMobileToggled.emit(true);
  }

  toggleAppTopMenuMobile() {
    var target = document.querySelector(".app-top-menu");
    if (target) {
      slideToggle(target);
    }
  }

  toggleAppHeaderMegaMenuMobile() {
    this.appSettings.appHeaderMegaMenuMobileToggled =
      !this.appSettings.appHeaderMegaMenuMobileToggled;
  }

  isAuthenticated(): boolean {
    return this.authService.isAuthenticated();
  }

  logout(): void {
    this.authService.logout().subscribe({
      next: () => {
        this.logger.success('Sesión cerrada exitosamente');
      },
      error: (error) => {
        this.logger.error('Error al cerrar sesión:', error);
        this.authService.clearSession();
      }
    });
  }

  handleBackClick(): void {
    this.backClick.emit();
  }

  /** Cicla al siguiente idioma disponible (para uso en mobile) */
  toggleLanguage(): void {
    const langs = this.languageService.languages;
    const currentIndex = langs.findIndex(l => l.code === this.languageService.currentLang());
    const nextIndex = (currentIndex + 1) % langs.length;
    this.languageService.setLanguage(langs[nextIndex].code);
  }

  override ngOnDestroy() {
    this.appSettings.appHeaderMegaMenuMobileToggled = false;
    super.ngOnDestroy();
  }

  ngOnInit() {
    // Establecer el título por defecto si no se ha proporcionado
    if (!this.pageTitle) {
      this.pageTitle = this.appSettings.nameCompany;
    }

    this.perfilUrl.set(this.fallbackPerfilUrl);
    this.perfilLoading.set(false);
  }

  markAllAsRead(): void {
    this.notificationService.markAllAsRead();
  }

  invalidateCache(): void {
    if (this.invalidatingCache()) return;
    this.invalidatingCache.set(true);
    this.http.post(`${environment.apiUrl}DomainSettings/cache/invalidate`, {}).subscribe({
      next: () => {
        this.notificationService.addNotification({
          id: crypto.randomUUID(),
          type: 'success',
          title: 'Caché actualizado',
          message: 'La configuración de compañía fue recargada exitosamente.',
          createdAt: new Date(),
          read: false
        });
        this.invalidatingCache.set(false);
      },
      error: (err) => {
        this.logger.error('Error al invalidar caché:', err);
        this.notificationService.addNotification({
          id: crypto.randomUUID(),
          type: 'error',
          title: 'Error al limpiar caché',
          message: 'No se pudo recargar la configuración de compañía.',
          createdAt: new Date(),
          read: false
        });
        this.invalidatingCache.set(false);
      }
    });
  }

  clearNotifications(): void {
    this.notificationService.clearAll();
  }

  removeNotification(id: string): void {
    this.notificationService.removeNotification(id);
  }

  onNotificationAction(notif: any): void {
    if (!notif.actionData) return;
    if ('campaignId' in notif.actionData) {
      this.campaignResult.set(notif.actionData as CampaignResult);
      this.showCampaignResultModal.set(true);
    } else {
      this.exportResult.set(notif.actionData as ExportResult);
      this.showExportResultModal.set(true);
    }
  }

  constructor(
    public appSettings: AppSettings,
    private authService: AuthService,
    private cdr: ChangeDetectorRef,
    loggerService: LoggerService,
    public languageService: LanguageService
  ) {
    super();
    this.logger = loggerService.getLogger('HeaderComponent');
    
    // Registrar íconos de Ionicons para el header móvil
    addIcons({
      notificationsOutline,
      menuOutline,
      languageOutline
    });
    
    // Escuchar cambios en el usuario autenticado usando effect
    effect(() => {
      this.currentUser = this.authService.currentUser();
      // Forzar detección de cambios
      this.cdr.detectChanges();
    });
  }
}
