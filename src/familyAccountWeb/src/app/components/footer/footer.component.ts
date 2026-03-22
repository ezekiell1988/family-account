import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AppSettings } from '../../service/app-settings.service';
import { ResponsiveComponent } from '../../shared/responsive-component.base';
import { FooterWebComponent } from './components/footer-web/footer-web.component';
import { FooterMobileComponent } from './components/footer-mobile/footer-mobile.component';

@Component({
  selector: 'app-footer',
  templateUrl: './footer.component.html',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, FooterWebComponent, FooterMobileComponent],
})
export class FooterComponent extends ResponsiveComponent {
  @Input() footerText = '';
  @Input() color = 'theme';
  @Input() translucent = false;
  @Input() footerClass = '';
  @Input() hasCustomContent = false;

  constructor(public appSettings: AppSettings) {
    super();
    if (!this.footerText) {
      this.footerText = `© ${this.appSettings.nameCompany} BackOffice`;
    }
  }
}
