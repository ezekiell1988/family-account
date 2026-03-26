import {
  Component,
  Input,
  Output,
  EventEmitter,
  OnInit,
  inject,
  ChangeDetectionStrategy,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  IonHeader,
  IonToolbar,
  IonTitle,
  IonButtons,
  IonMenuButton,
  IonBackButton,
  IonButton,
  IonIcon,
  IonBadge,
} from '@ionic/angular/standalone';
import { addIcons } from 'ionicons';
import { notificationsOutline, menuOutline, languageOutline } from 'ionicons/icons';
import { AppSettings } from '../../../../service/app-settings.service';
import { AuthService, LanguageService, NotificationService } from '../../../../service';

@Component({
  selector: 'app-header-mobile',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CommonModule,
    IonHeader,
    IonToolbar,
    IonTitle,
    IonButtons,
    IonMenuButton,
    IonBackButton,
    IonButton,
    IonIcon,
    IonBadge,
  ],
  templateUrl: './header-mobile.component.html',
})
export class HeaderMobileComponent implements OnInit {
  private readonly authService      = inject(AuthService);
  readonly languageService          = inject(LanguageService);
  private readonly notificationService = inject(NotificationService);
  readonly appSettings              = inject(AppSettings);

  @Input() pageTitle        = '';
  @Input() color            = 'theme';
  @Input() translucent      = true;
  @Input() showBackButton   = false;
  @Input() backButtonHref   = '/';
  @Input() showNotifications = true;

  @Output() backClick = new EventEmitter<void>();

  readonly notifications = this.notificationService.notifications;
  readonly unreadCount   = this.notificationService.unreadCount;

  constructor() {
    addIcons({ notificationsOutline, menuOutline, languageOutline });
  }

  ngOnInit(): void {
    if (!this.pageTitle) {
      this.pageTitle = this.appSettings.nameCompany;
    }
  }

  isAuthenticated(): boolean {
    return this.authService.isAuthenticated();
  }

  handleBackClick(): void {
    this.backClick.emit();
  }

  markAllAsRead(): void {
    this.notificationService.markAllAsRead();
  }

  toggleLanguage(): void {
    const langs = this.languageService.languages;
    const currentIndex = langs.findIndex(l => l.code === this.languageService.currentLang());
    const nextIndex = (currentIndex + 1) % langs.length;
    this.languageService.setLanguage(langs[nextIndex].code);
  }
}
