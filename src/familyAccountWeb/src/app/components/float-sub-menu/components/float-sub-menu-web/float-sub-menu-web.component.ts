import { Component, Input, Output, EventEmitter, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { AppSettings } from '../../../../service/app-settings.service';

declare var slideToggle: any;

@Component({
  selector: 'float-sub-menu-web',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, RouterModule],
  templateUrl: './float-sub-menu-web.component.html',
})
export class FloatSubMenuWebComponent {
  @Input() menus: any;
  @Input() top: any;
  @Input() left: any;
  @Input() right: any;
  @Input() bottom: any;
  @Input() lineTop: any;
  @Input() lineBottom: any;
  @Input() arrowTop: any;
  @Input() arrowBottom: any;

  @Output() remainAppSidebarFloatSubMenu  = new EventEmitter();
  @Output() hideAppSidebarFloatSubMenu    = new EventEmitter();
  @Output() calculateFloatSubMenuPosition = new EventEmitter();

  constructor(public appSettings: AppSettings) {}

  expandCollapseSubmenu(e: MouseEvent, currentMenu: any, allMenu: any, active: any): void {
    e.preventDefault();
    const targetItem = (e.target as HTMLElement).closest('.menu-item');
    const target = targetItem?.querySelector('.menu-submenu') as HTMLElement;
    slideToggle(target);
    this.calculateFloatSubMenuPosition.emit();
  }

  remainMenu(): void {
    this.remainAppSidebarFloatSubMenu.emit(true);
  }

  hideMenu(): void {
    this.hideAppSidebarFloatSubMenu.emit(true);
  }
}
