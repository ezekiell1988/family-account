import {
  Component,
  Input,
  Output,
  EventEmitter,
  OnDestroy,
  inject,
  signal,
  computed,
  ChangeDetectionStrategy,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { TranslatePipe } from '@ngx-translate/core';
import { CampaignResult, ExportResult } from '../../../../service/notification.service';
import { AppSettings } from '../../../../service/app-settings.service';
import {
  AuthService,
  LoggerService,
  Logger,
  LanguageService,
  NotificationService,
} from '../../../../service';
import { UserData } from '../../../../shared/models';
import { environment } from '../../../../../environments/environment';

declare var slideToggle: any;

@Component({
  selector: 'app-header-web',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, TranslatePipe],
  templateUrl: './header-web.component.html',
})
export class HeaderWebComponent implements OnDestroy {
  private readonly http                = inject(HttpClient);
  private readonly authService         = inject(AuthService);
  private readonly notificationService = inject(NotificationService);
  private readonly logger: Logger;
  readonly appSettings                 = inject(AppSettings);
  readonly languageService             = inject(LanguageService);

  @Input() appSidebarTwo = false;

  @Output() appSidebarEndToggled       = new EventEmitter<boolean>();
  @Output() appSidebarMobileToggled    = new EventEmitter<boolean>();
  @Output() appSidebarEndMobileToggled = new EventEmitter<boolean>();

  // ── Usuario autenticado ──────────────────────────────────────────
  readonly currentUser = computed<UserData | null>(() => this.authService.currentUser());

  /** Iniciales del usuario para el avatar (ej: "EB" de "Ezequiel Baltodano") */
  readonly initials = computed<string>(() => {
    const name = this.currentUser()?.nameLogin?.trim() ?? '';
    if (!name) return '?';
    const parts = name.split(/\s+/);
    return parts.length >= 2
      ? (parts[0][0] + parts[parts.length - 1][0]).toUpperCase()
      : name.slice(0, 2).toUpperCase();
  });

  // ── Notificaciones ───────────────────────────────────────────────
  readonly notifications = this.notificationService.notifications;
  readonly unreadCount   = this.notificationService.unreadCount;

  // ── Imagen de perfil ─────────────────────────────────────────────
  private readonly fallbackPerfilUrl = 'assets/img/user.png';
  readonly perfilUrl     = signal<string>('assets/img/user.png');
  readonly perfilLoading = signal(false);
  /** Reservado para object URL de imagen subida; null = usa fallback */
  readonly perfilObjectUrl: string | null = null;

  // ── Modales ──────────────────────────────────────────────────────
  readonly showCampaignResultModal = signal(false);
  readonly campaignResult          = signal<CampaignResult | null>(null);
  readonly showExportResultModal   = signal(false);
  readonly exportResult            = signal<ExportResult | null>(null);
  readonly showPerfilModal         = signal(false);

  // ── Estado auxiliar ──────────────────────────────────────────────
  readonly invalidatingCache = signal(false);

  constructor(loggerService: LoggerService) {
    this.logger = loggerService.getLogger('HeaderWebComponent');
  }

  ngOnDestroy(): void {
    this.appSettings.appHeaderMegaMenuMobileToggled = false;
  }

  // ── Sidebar toggles ──────────────────────────────────────────────
  toggleAppSidebarMobile(): void {
    this.appSidebarMobileToggled.emit(true);
  }

  toggleAppSidebarEnd(): void {
    this.appSidebarEndToggled.emit(true);
  }

  toggleAppSidebarEndMobile(): void {
    this.appSidebarEndMobileToggled.emit(true);
  }

  toggleAppTopMenuMobile(): void {
    const target = document.querySelector('.app-top-menu');
    if (target) {
      slideToggle(target);
    }
  }

  toggleAppHeaderMegaMenuMobile(): void {
    this.appSettings.appHeaderMegaMenuMobileToggled =
      !this.appSettings.appHeaderMegaMenuMobileToggled;
  }

  // ── Auth ─────────────────────────────────────────────────────────
  logout(): void {
    this.authService.logout().subscribe({
      next: () => this.logger.success('Sesión cerrada exitosamente'),
      error: (error) => {
        this.logger.error('Error al cerrar sesión:', error);
        this.authService.clearSession();
      },
    });
  }

  // ── Notificaciones ───────────────────────────────────────────────
  markAllAsRead(): void {
    this.notificationService.markAllAsRead();
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

  // ── Caché ────────────────────────────────────────────────────────
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
          read: false,
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
          read: false,
        });
        this.invalidatingCache.set(false);
      },
    });
  }
}
