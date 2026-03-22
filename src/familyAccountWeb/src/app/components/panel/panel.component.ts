import { Component, Input, Output, EventEmitter } from "@angular/core";
import { CommonModule } from "@angular/common";
import { ResponsiveComponent } from '../../shared/responsive-component.base';
import { PanelWebComponent } from './components/panel-web/panel-web.component';
import { PanelMobileComponent } from './components/panel-mobile/panel-mobile.component';

@Component({
  selector: "panel",
  templateUrl: "./panel.component.html",
  standalone: true,
  imports: [CommonModule, PanelWebComponent, PanelMobileComponent],
})
export class PanelComponent extends ResponsiveComponent {
  // Inputs para configuración del panel
  @Input() title?: string;
  @Input() variant?: string;
  @Input() noBody: boolean = false;
  @Input() noButton: boolean = false;
  @Input() headerClass?: string;
  @Input() bodyClass?: string;
  @Input() footerClass?: string;
  @Input() panelClass?: string;
  @Input() showReloadButton: boolean = false;
  @Input() reload: boolean = false;

  // Outputs para eventos
  @Output() reloadChange = new EventEmitter<boolean>();
  @Output() onReload = new EventEmitter<void>();
}
