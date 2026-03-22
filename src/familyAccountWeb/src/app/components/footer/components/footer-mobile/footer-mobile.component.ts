import { Component, Input, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { IonFooter, IonToolbar, IonTitle } from '@ionic/angular/standalone';

@Component({
  selector: 'app-footer-mobile',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, IonFooter, IonToolbar, IonTitle],
  templateUrl: './footer-mobile.component.html',
})
export class FooterMobileComponent {
  @Input() footerText = '';
  @Input() color = 'theme';
  @Input() translucent = false;
}
