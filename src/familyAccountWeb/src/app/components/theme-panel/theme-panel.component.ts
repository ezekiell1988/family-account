import { ChangeDetectionStrategy, Component, EventEmitter, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ResponsiveComponent } from '../../shared/responsive-component.base';
import { ThemePanelWebComponent } from './components/theme-panel-web/theme-panel-web.component';
import { ThemePanelMobileComponent } from './components/theme-panel-mobile/theme-panel-mobile.component';

@Component({
  selector: 'theme-panel',
  templateUrl: './theme-panel.component.html',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, ThemePanelWebComponent, ThemePanelMobileComponent],
})
export class ThemePanelComponent extends ResponsiveComponent {
  @Output() appDarkModeChanged = new EventEmitter<boolean>();
  @Output() appThemeChanged = new EventEmitter<boolean>();
}
