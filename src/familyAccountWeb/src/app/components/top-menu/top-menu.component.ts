import { ChangeDetectionStrategy, Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ResponsiveComponent } from '../../shared/responsive-component.base';
import { TopMenuWebComponent } from './components/top-menu-web/top-menu-web.component';
import { TopMenuMobileComponent } from './components/top-menu-mobile/top-menu-mobile.component';

@Component({
  selector: 'top-menu',
  templateUrl: './top-menu.component.html',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, TopMenuWebComponent, TopMenuMobileComponent],
})
export class TopMenuComponent extends ResponsiveComponent {}
