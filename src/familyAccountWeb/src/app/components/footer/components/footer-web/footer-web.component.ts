import { Component, Input, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-footer-web',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule],
  templateUrl: './footer-web.component.html',
})
export class FooterWebComponent {
  @Input() footerText = '';
  @Input() footerClass = '';
  @Input() hasCustomContent = false;
}
