import {
  Component,
  ChangeDetectionStrategy,
  input,
  output,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { PanelComponent, FooterComponent } from '../../../../components';
import { UserData } from '../../../../shared/models';

@Component({
  selector: 'app-home-web',
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: true,
  imports: [CommonModule, PanelComponent, FooterComponent],
  templateUrl: './home-web.component.html',
})
export class HomeWebComponent {
  currentUser     = input<UserData | null>(null);
  rolesLabel      = input('');
  nameCompany     = input('N/D');
  logoutRequested = output<void>();
}
