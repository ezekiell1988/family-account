import { ChangeDetectionStrategy, Component, EventEmitter, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ResponsiveComponent } from '../../shared/responsive-component.base';
import { SidebarRightWebComponent } from './components/sidebar-right-web/sidebar-right-web.component';
import { SidebarRightMobileComponent } from './components/sidebar-right-mobile/sidebar-right-mobile.component';

@Component({
  selector: 'sidebar-right',
  templateUrl: './sidebar-right.component.html',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, SidebarRightWebComponent, SidebarRightMobileComponent],
})
export class SidebarRightComponent extends ResponsiveComponent {
  @Output() appSidebarEndMobileToggled = new EventEmitter<boolean>();
}
