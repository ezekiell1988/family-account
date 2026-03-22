import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  EventEmitter,
  OnInit,
  Output,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import {
  IonFab,
  IonFabButton,
  IonIcon,
  IonMenu,
  IonHeader,
  IonToolbar,
  IonTitle,
  IonButtons,
  IonButton,
  IonContent,
  IonList,
  IonListHeader,
  IonItem,
  IonLabel,
  IonToggle,
  MenuController,
  AlertController,
} from '@ionic/angular/standalone';
import { addIcons } from 'ionicons';
import {
  settingsOutline,
  closeOutline,
  checkmarkOutline,
  colorPaletteOutline,
  moonOutline,
  textOutline,
  refreshOutline,
} from 'ionicons/icons';
import { AppSettings } from '../../../../service/app-settings.service';
import { LoggerService, MenuStateService } from '../../../../service';

@Component({
  selector: 'theme-panel-mobile',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CommonModule,
    FormsModule,
    TranslatePipe,
    IonFab,
    IonFabButton,
    IonIcon,
    IonMenu,
    IonHeader,
    IonToolbar,
    IonTitle,
    IonButtons,
    IonButton,
    IonContent,
    IonList,
    IonListHeader,
    IonItem,
    IonLabel,
    IonToggle,
  ],
  templateUrl: './theme-panel-mobile.component.html',
})
export class ThemePanelMobileComponent implements OnInit {
  @Output() appDarkModeChanged = new EventEmitter<boolean>();
  @Output() appThemeChanged = new EventEmitter<boolean>();

  showFab = true;
  appThemeDarkModeCheckbox = false;
  appRtlEnabledCheckbox = false;
  selectedTheme = 'teal';
  toolbarColor = '#00acac';
  themes = ['red', 'pink', 'orange', 'yellow', 'lime', 'green', 'teal', 'cyan', 'blue', 'purple', 'indigo', 'gray-500'];

  constructor(
    public appSettings: AppSettings,
    private menuController: MenuController,
    private alertController: AlertController,
    private cdr: ChangeDetectorRef,
    private menuStateService: MenuStateService,
    private logger: LoggerService,
  ) {
    addIcons({
      settingsOutline,
      closeOutline,
      checkmarkOutline,
      colorPaletteOutline,
      moonOutline,
      textOutline,
      refreshOutline,
    });
  }

  ngOnInit(): void {
    this.menuStateService.isMenuOpen$.subscribe((isOpen) => {
      if (isOpen) {
        this.showFab = false;
      } else {
        setTimeout(() => { this.showFab = true; }, 300);
      }
      this.cdr.detectChanges();
    });

    if (localStorage) {
      if (localStorage['appTheme']) {
        this.toggleTheme(localStorage['appTheme']);
      } else {
        setTimeout(() => this.updateIonicThemeColors(), 100);
      }
      if (localStorage['appDarkMode'] === 'true') {
        this.appThemeDarkModeCheckbox = true;
        this.appSettings.appDarkMode = true;
        this.appDarkModeChanged.emit(true);
      }
      if (localStorage['appRtlMode'] === 'true') {
        this.appRtlEnabledCheckbox = true;
        document.documentElement.setAttribute('dir', 'rtl');
      }
    }
  }

  async openMobileSettings(): Promise<void> {
    await this.menuController.open('settings-menu');
    this.logger.debug('Menú de configuración abierto');
  }

  async closeMobileSettings(): Promise<void> {
    await this.menuController.close('settings-menu');
    this.logger.debug('Menú de configuración cerrado');
  }

  toggleTheme(theme: string, event?: Event): void {
    if (event) {
      event.stopPropagation();
      event.preventDefault();
    }
    this.appSettings.appTheme = theme;
    this.selectedTheme = theme;
    this.appThemeChanged.emit(true);
    if (localStorage) {
      localStorage['appTheme'] = theme;
    }
    setTimeout(() => this.updateIonicThemeColors(), 150);
  }

  toggleDarkMode(e: any): void {
    const checked = e.detail?.checked || false;
    this.appSettings.appDarkMode = checked;
    this.appDarkModeChanged.emit(true);
    if (checked) {
      document.body.classList.add('dark');
    } else {
      document.body.classList.remove('dark');
    }
    if (localStorage) {
      localStorage['appDarkMode'] = checked;
    }
  }

  toggleRtlEnabled(e: any): void {
    const isRtl = e.detail?.checked || false;
    if (isRtl) {
      document.documentElement.setAttribute('dir', 'rtl');
    } else {
      document.documentElement.removeAttribute('dir');
    }
    if (localStorage) {
      localStorage.setItem('appRtlMode', isRtl.toString());
    }
  }

  async resetLocalStorage(): Promise<void> {
    const alert = await this.alertController.create({
      header: '¿Restaurar Configuración?',
      message: '¿Estás seguro de que deseas restaurar la configuración predeterminada? Esto recargará la aplicación.',
      buttons: [
        { text: 'Cancelar', role: 'cancel', cssClass: 'secondary' },
        {
          text: 'Sí, restaurar',
          handler: () => {
            this.clearThemeSettings();
            window.location.reload();
          },
        },
      ],
    });
    await alert.present();
  }

  private clearThemeSettings(): void {
    ['appTheme', 'appThemePanelActive', 'appDarkMode', 'appHeaderFixed', 'appHeaderInverse',
     'appSidebarFixed', 'appSidebarGrid', 'appGradientEnabled', 'appRtlMode']
      .forEach((key) => localStorage.removeItem(key));
  }

  private updateIonicThemeColors(): void {
    const root = document.documentElement;
    const styles = getComputedStyle(root);
    const themeColor = styles.getPropertyValue(`--bs-${this.selectedTheme}`).trim();
    if (themeColor && themeColor.startsWith('#')) {
      const rgb = this.hexToRgb(themeColor);
      if (rgb) {
        this.toolbarColor = themeColor;
        root.style.setProperty('--ion-color-theme', themeColor);
        root.style.setProperty('--ion-color-theme-rgb', `${rgb.r}, ${rgb.g}, ${rgb.b}`);
        root.style.setProperty('--ion-color-theme-shade', this.adjustBrightness(themeColor, -10));
        root.style.setProperty('--ion-color-theme-tint', this.adjustBrightness(themeColor, 10));
        this.cdr.detectChanges();
      }
    }
  }

  private hexToRgb(hex: string): { r: number; g: number; b: number } | null {
    hex = hex.replace('#', '');
    const r = parseInt(hex.substring(0, 2), 16);
    const g = parseInt(hex.substring(2, 4), 16);
    const b = parseInt(hex.substring(4, 6), 16);
    if (isNaN(r) || isNaN(g) || isNaN(b)) return null;
    return { r, g, b };
  }

  private adjustBrightness(hex: string, percent: number): string {
    const rgb = this.hexToRgb(hex);
    if (!rgb) return hex;
    const adjust = (v: number) => Math.min(255, Math.max(0, Math.round(v + (v * percent) / 100)));
    return `#${adjust(rgb.r).toString(16).padStart(2, '0')}${adjust(rgb.g).toString(16).padStart(2, '0')}${adjust(rgb.b).toString(16).padStart(2, '0')}`;
  }
}
