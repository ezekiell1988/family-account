import {
  Component,
  Input,
  Output,
  EventEmitter,
  ChangeDetectionStrategy,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { AppSettings } from '../../service/app-settings.service';
import { ResponsiveComponent } from '../../shared/responsive-component.base';
import { HeaderMobileComponent } from './components/header-mobile/header-mobile.component';
import { HeaderWebComponent } from './components/header-web/header-web.component';

@Component({
  selector: 'header',
  templateUrl: './header.component.html',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, HeaderMobileComponent, HeaderWebComponent],
})
export class HeaderComponent extends ResponsiveComponent {
  @Input() appSidebarTwo     = false;
  @Input() pageTitle         = '';
  @Input() color             = 'theme';
  @Input() translucent       = true;
  @Input() showBackButton    = false;
  @Input() backButtonHref    = '/';
  @Input() showNotifications = true;
  @Input() hasCustomContent  = false;

  @Output() appSidebarEndToggled       = new EventEmitter<boolean>();
  @Output() appSidebarMobileToggled    = new EventEmitter<boolean>();
  @Output() appSidebarEndMobileToggled = new EventEmitter<boolean>();
  @Output() backClick                  = new EventEmitter<void>();

  constructor(public readonly appSettings: AppSettings) {
    super();
  }
}
