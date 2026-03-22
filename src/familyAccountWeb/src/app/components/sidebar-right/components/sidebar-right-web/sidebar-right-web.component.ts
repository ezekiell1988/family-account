import { Component, Output, EventEmitter, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NgScrollbarModule } from 'ngx-scrollbar';

@Component({
  selector: 'sidebar-right-web',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, NgScrollbarModule],
  templateUrl: './sidebar-right-web.component.html',
})
export class SidebarRightWebComponent {
  @Output() appSidebarEndMobileToggled = new EventEmitter<boolean>();

  toggleAppSidebarEndMobile(): void {
    this.appSidebarEndMobileToggled.emit(true);
  }
}
