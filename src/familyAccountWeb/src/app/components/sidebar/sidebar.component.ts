import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ResponsiveComponent } from '../../shared/responsive-component.base';
import { SidebarWebComponent } from './components/sidebar-web/sidebar-web.component';
import { SidebarMobileComponent } from './components/sidebar-mobile/sidebar-mobile.component';

@Component({
  selector: 'sidebar',
  templateUrl: './sidebar.component.html',
  standalone: true,
  imports: [
    CommonModule,
    SidebarWebComponent,
    SidebarMobileComponent,
  ]
})
export class SidebarComponent extends ResponsiveComponent {
  @Output() appSidebarMinifiedToggled = new EventEmitter<boolean>();
  @Output() hideMobileSidebar = new EventEmitter<boolean>();
  @Output() setPageFloatSubMenu = new EventEmitter();
  @Output() appSidebarMobileToggled = new EventEmitter<boolean>();

  @Input() appSidebarTransparent: any;
  @Input() appSidebarGrid: any;
  @Input() appSidebarFixed: any;
  @Input() appSidebarMinified: any;
}
