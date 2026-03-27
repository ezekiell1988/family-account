import { Component, OnInit, effect, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router, NavigationEnd } from '@angular/router';
import { filter } from 'rxjs/operators';
import {
  IonMenu,
  IonHeader,
  IonToolbar,
  IonTitle,
  IonContent,
  IonList,
  IonItem,
  IonLabel,
  IonIcon,
  IonMenuToggle,
  IonButton,
  IonButtons,
  IonFooter,
  MenuController
} from '@ionic/angular/standalone';
import { addIcons } from 'ionicons';
import {
  homeOutline,
  personOutline,
  settingsOutline,
  logOutOutline,
  walletOutline,
  statsChartOutline,
  documentTextOutline,
  businessOutline,
  chevronForwardOutline,
  chevronDownOutline,
  folderOutline,
  documentOutline,
  closeOutline,
  checkmarkCircle,
  ellipseOutline
} from 'ionicons/icons';
import { AppMenuService } from '../../../../service/app-menus.service';
import { AppSettings } from '../../../../service/app-settings.service';
import { AuthService, LoggerService, Logger, MenuStateService } from '../../../../service';
import { UserData } from '../../../../shared/models';
import { TranslatePipe } from '@ngx-translate/core';

@Component({
  selector: 'sidebar-mobile',
  templateUrl: './sidebar-mobile.component.html',
  standalone: true,
  styles: [`
    .user-profile-mobile {
      padding: 20px;
      text-align: center;
      background: linear-gradient(135deg, var(--ion-color-primary), var(--ion-color-secondary));
      margin-bottom: 10px;
    }

    .user-profile-mobile .profile-avatar {
      margin: 0 auto 15px;
      width: 80px;
      height: 80px;
      border-radius: 50%;
      overflow: hidden;
      border: 3px solid white;
      box-shadow: 0 4px 10px rgba(0,0,0,0.2);
    }

    .user-profile-mobile .profile-avatar img {
      width: 100%;
      height: 100%;
      object-fit: cover;
    }

    .user-profile-mobile .profile-info h3 {
      color: white;
      margin: 0 0 5px 0;
      font-size: 1.2rem;
      font-weight: 600;
    }

    .user-profile-mobile .profile-info p {
      color: rgba(255,255,255,0.9);
      margin: 3px 0;
      font-size: 0.85rem;
    }

    .user-profile-mobile .profile-info .user-code {
      font-weight: 500;
      background: rgba(255,255,255,0.2);
      display: inline-block;
      padding: 2px 12px;
      border-radius: 12px;
      margin-top: 5px;
    }

    .user-profile-mobile .profile-info .user-email {
      font-size: 0.8rem;
    }

    .user-profile-mobile .profile-info .user-phone {
      font-size: 0.8rem;
    }
  `],
  imports: [
    CommonModule,
    RouterModule,
    IonMenu,
    IonHeader,
    IonToolbar,
    IonTitle,
    IonContent,
    IonList,
    IonItem,
    IonLabel,
    IonIcon,
    IonMenuToggle,
    IonButton,
    IonButtons,
    IonFooter,
    TranslatePipe,
  ]
})
export class SidebarMobileComponent implements OnInit {
  private readonly logger: Logger;
  menus: any[] = [];
  currentUser: UserData | null = null;

  constructor(
    public appSettings: AppSettings,
    private appMenuService: AppMenuService,
    private menuController: MenuController,
    private authService: AuthService,
    private cdr: ChangeDetectorRef,
    private menuStateService: MenuStateService,
    private router: Router,
    loggerService: LoggerService
  ) {
    this.logger = loggerService.getLogger('SidebarMobileComponent');

    addIcons({
      homeOutline,
      personOutline,
      settingsOutline,
      logOutOutline,
      walletOutline,
      statsChartOutline,
      documentTextOutline,
      businessOutline,
      chevronForwardOutline,
      chevronDownOutline,
      folderOutline,
      documentOutline,
      closeOutline,
      checkmarkCircle,
      ellipseOutline
    });

    effect(() => {
      const user = this.authService.currentUser();
      this.currentUser = user;
      const userRoles = user?.roles ?? [];
      this.menus = this.appMenuService.getMenuForRoles(userRoles);
      this.cdr.markForCheck();
    });
  }

  ngOnInit(): void {
    this.initializeMobileMenuState(this.menus);

    this.router.events.pipe(
      filter(event => event instanceof NavigationEnd)
    ).subscribe(() => {
      this.initializeMobileMenuState(this.menus);
      this.cdr.detectChanges();
    });

    document.addEventListener('ionDidOpen', (event: any) => {
      if (event.target.menuId === 'main-menu') {
        this.menuStateService.openMenu();
        this.logger.debug('Menu sidebar abierto');
      }
    });

    document.addEventListener('ionDidClose', (event: any) => {
      if (event.target.menuId === 'main-menu') {
        setTimeout(() => {
          this.menuStateService.closeMenu();
          this.logger.debug('Menu sidebar cerrado');
        }, 50);
      }
    });
  }

  private initializeMobileMenuState(items: any[]): void {
    const currentUrl = this.router.url;
    items.forEach(item => {
      if (item.submenu) {
        item.expanded = this.menuContainsActiveRoute(item, currentUrl);
        this.initializeMobileMenuState(item.submenu);
      }
    });
  }

  private menuContainsActiveRoute(menuItem: any, currentUrl: string): boolean {
    if (menuItem.url && currentUrl.startsWith(menuItem.url)) {
      return true;
    }
    if (menuItem.submenu) {
      return menuItem.submenu.some((subItem: any) =>
        this.menuContainsActiveRoute(subItem, currentUrl)
      );
    }
    return false;
  }

  toggleMobileSubmenu(item: any): void {
    item.expanded = !item.expanded;
  }

  async closeMenu(): Promise<void> {
    await this.menuController.close('main-menu');
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
}
