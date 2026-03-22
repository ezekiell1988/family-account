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
import { NgScrollbarModule } from 'ngx-scrollbar';
import { TranslatePipe } from '@ngx-translate/core';
import Swal from 'sweetalert2';
import { AppVariablesService } from '../../../../service/app-variables.service';
import { AppSettings } from '../../../../service/app-settings.service';

declare var bootstrap: any;

@Component({
  selector: 'theme-panel-web',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, FormsModule, NgScrollbarModule, TranslatePipe],
  templateUrl: './theme-panel-web.component.html',
})
export class ThemePanelWebComponent implements OnInit {
  @Output() appDarkModeChanged = new EventEmitter<boolean>();
  @Output() appThemeChanged = new EventEmitter<boolean>();

  appVariables = this.appVariablesService.getAppVariables();
  active = false;
  appThemeDarkModeCheckbox = false;
  appHeaderFixedCheckbox = true;
  appHeaderInverseCheckbox = false;
  appSidebarFixedCheckbox = true;
  appSidebarGridCheckbox = false;
  appGradientEnabledCheckbox = false;
  appRtlEnabledCheckbox = false;
  selectedTheme = 'teal';
  themes = ['red', 'pink', 'orange', 'yellow', 'lime', 'green', 'teal', 'cyan', 'blue', 'purple', 'indigo', 'gray-500'];

  constructor(
    public appSettings: AppSettings,
    private appVariablesService: AppVariablesService,
    private cdr: ChangeDetectorRef,
  ) {}

  ngOnInit(): void {
    const elm = document.querySelectorAll('[data-bs-toggle="tooltip"]');
    for (let i = 0; i < elm.length; i++) {
      new bootstrap.Tooltip(elm[i]);
    }
    if (localStorage) {
      if (localStorage['appThemePanelActive']) {
        this.active = localStorage['appThemePanelActive'] === 'true';
      }
      if (localStorage['appTheme']) {
        this.toggleTheme(localStorage['appTheme']);
      }
      if (localStorage['appDarkMode'] === 'true') {
        this.appThemeDarkModeCheckbox = true;
        this.appSettings.appDarkMode = true;
        this.appDarkModeChanged.emit(true);
      }
      if (localStorage['appHeaderFixed'] !== undefined && localStorage['appHeaderFixed'] !== 'true') {
        this.appHeaderFixedCheckbox = false;
      }
      if (localStorage['appHeaderInverse'] === 'true') {
        this.appHeaderInverseCheckbox = true;
      }
      if (localStorage['appSidebarFixed'] !== undefined && localStorage['appSidebarFixed'] !== 'true') {
        this.appSidebarFixedCheckbox = false;
      }
      if (localStorage['appSidebarGrid'] === 'true') {
        this.appSidebarGridCheckbox = true;
      }
      if (localStorage['appGradientEnabled'] === 'true') {
        this.appGradientEnabledCheckbox = true;
      }
      if (localStorage['appRtlMode'] === 'true') {
        this.appRtlEnabledCheckbox = true;
        document.documentElement.setAttribute('dir', 'rtl');
      }
    }
  }

  toggleThemePanel(): void {
    if (localStorage) {
      localStorage['appThemePanelActive'] = !this.active;
    }
    this.active = !this.active;
  }

  toggleTheme(theme: string): void {
    this.appSettings.appTheme = theme;
    this.selectedTheme = theme;
    this.appThemeChanged.emit(true);
    if (localStorage) {
      localStorage['appTheme'] = theme;
    }
  }

  toggleDarkMode(e: Event): void {
    const checked = (e as any).srcElement?.checked || false;
    this.appSettings.appDarkMode = checked;
    this.appDarkModeChanged.emit(true);
    if (localStorage) {
      localStorage['appDarkMode'] = checked;
    }
  }

  toggleRtlEnabled(e: Event): void {
    const isRtl = (e as any).srcElement?.checked || false;
    if (isRtl) {
      document.documentElement.setAttribute('dir', 'rtl');
    } else {
      document.documentElement.removeAttribute('dir');
    }
    if (localStorage) {
      localStorage.setItem('appRtlMode', isRtl.toString());
    }
  }

  resetLocalStorage(): void {
    Swal.fire({
      title: '¿Restaurar Configuración?',
      text: '¿Estás seguro de que deseas restaurar la configuración predeterminada? Esto recargará la aplicación.',
      icon: 'warning',
      showCancelButton: true,
      confirmButtonColor: '#00acac',
      cancelButtonColor: '#6c757d',
      confirmButtonText: 'Sí, restaurar',
      cancelButtonText: 'Cancelar',
    }).then((result) => {
      if (result.isConfirmed) {
        this.clearThemeSettings();
        window.location.reload();
      }
    });
  }

  private clearThemeSettings(): void {
    ['appTheme', 'appThemePanelActive', 'appDarkMode', 'appHeaderFixed', 'appHeaderInverse',
     'appSidebarFixed', 'appSidebarGrid', 'appGradientEnabled', 'appRtlMode']
      .forEach((key) => localStorage.removeItem(key));
  }
}
