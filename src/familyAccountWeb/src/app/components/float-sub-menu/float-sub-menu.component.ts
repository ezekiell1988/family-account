import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ResponsiveComponent } from '../../shared/responsive-component.base';
import { FloatSubMenuWebComponent } from './components/float-sub-menu-web/float-sub-menu-web.component';
import { FloatSubMenuMobileComponent } from './components/float-sub-menu-mobile/float-sub-menu-mobile.component';

@Component({
  selector: 'float-sub-menu',
  templateUrl: './float-sub-menu.component.html',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, FloatSubMenuWebComponent, FloatSubMenuMobileComponent],
})
export class FloatSubMenuComponent extends ResponsiveComponent {
  @Input() menus: any;
  @Input() top: any;
  @Input() left: any;
  @Input() right: any;
  @Input() bottom: any;
  @Input() lineTop: any;
  @Input() lineBottom: any;
  @Input() arrowTop: any;
  @Input() arrowBottom: any;

  @Output() remainAppSidebarFloatSubMenu = new EventEmitter<boolean>();
  @Output() hideAppSidebarFloatSubMenu = new EventEmitter<boolean>();
  @Output() calculateFloatSubMenuPosition = new EventEmitter<void>();
}
