import {
  Component,
  ViewChild,
  AfterViewInit,
  Input,
  Output,
  EventEmitter,
} from "@angular/core";
import { CommonModule } from "@angular/common";

@Component({
  selector: "panel-web",
  templateUrl: "./panel-web.component.html",
  standalone: true,
  imports: [CommonModule],
})
export class PanelWebComponent implements AfterViewInit {
  @ViewChild("panelFooter", { static: false }) panelFooter: any;

  // Inputs de configuración
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

  // Output de eventos
  @Output() onReload = new EventEmitter<void>();

  // Estado interno
  expand = false;
  collapse = false;
  remove = false;
  showFooter = false;

  ngAfterViewInit() {
    setTimeout(() => {
      this.showFooter = this.panelFooter
        ? this.panelFooter.nativeElement &&
          this.panelFooter.nativeElement.children.length > 0
        : false;
    });
  }

  panelExpand() {
    this.expand = !this.expand;
  }

  panelReload() {
    this.onReload.emit();
  }

  panelCollapse() {
    this.collapse = !this.collapse;
  }

  panelRemove() {
    this.remove = !this.remove;
  }
}
