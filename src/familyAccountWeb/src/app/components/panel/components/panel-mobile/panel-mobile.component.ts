import {
  Component,
  ViewChild,
  AfterViewInit,
  Input,
  Output,
  EventEmitter,
} from "@angular/core";
import { CommonModule } from "@angular/common";
import {
  IonCard,
  IonCardHeader,
  IonCardTitle,
  IonCardContent,
  IonButton,
  IonIcon,
  IonSpinner,
} from "@ionic/angular/standalone";
import { addIcons } from "ionicons";
import {
  expandOutline,
  refreshOutline,
  chevronUpOutline,
  chevronDownOutline,
  closeOutline,
} from "ionicons/icons";

@Component({
  selector: "panel-mobile",
  templateUrl: "./panel-mobile.component.html",
  styleUrls: ["./panel-mobile.component.scss"],
  standalone: true,
  imports: [
    CommonModule,
    IonCard,
    IonCardHeader,
    IonCardTitle,
    IonCardContent,
    IonButton,
    IonIcon,
    IonSpinner,
  ],
})
export class PanelMobileComponent implements AfterViewInit {
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
  collapse = false;
  remove = false;
  showFooter = false;

  constructor() {
    addIcons({
      expandOutline,
      refreshOutline,
      chevronUpOutline,
      chevronDownOutline,
      closeOutline,
    });
  }

  ngAfterViewInit() {
    setTimeout(() => {
      this.showFooter = this.panelFooter
        ? this.panelFooter.nativeElement &&
          this.panelFooter.nativeElement.children.length > 0
        : false;
    });
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
