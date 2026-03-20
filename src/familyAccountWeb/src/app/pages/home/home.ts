import { Component, computed, inject } from "@angular/core";
import { CommonModule } from "@angular/common";
import { addIcons } from "ionicons";
import { AppSettings, AuthService } from "../../service";
import { HeaderComponent, FooterComponent, PanelComponent } from "../../components";
import {
  homeOutline,
  personCircleOutline,
  logOutOutline,
  shieldCheckmarkOutline,
} from "ionicons/icons";
import {
  IonContent,
  IonCard,
  IonCardContent,
  IonIcon,
  IonBadge,
  IonButton,
} from "@ionic/angular/standalone";
import { ResponsiveComponent } from "../../shared";

@Component({
  selector: "home",
  templateUrl: "./home.html",
  styleUrls: ["./home.scss"],
  standalone: true,
  imports: [
    CommonModule,
    HeaderComponent,
    FooterComponent,
    PanelComponent,
    IonContent,
    IonCard,
    IonCardContent,
    IonIcon,
    IonBadge,
    IonButton,
  ],
})
export class HomePage extends ResponsiveComponent {
  private readonly authService = inject(AuthService);

  readonly currentUser = this.authService.currentUser;
  readonly rolesLabel = computed(() => {
    const roles = this.currentUser()?.roles ?? [];
    return roles.length > 0 ? roles.join(", ") : "Sin roles asignados";
  });

  constructor(public appSettings: AppSettings) {
    super();

    addIcons({
      homeOutline,
      personCircleOutline,
      logOutOutline,
      shieldCheckmarkOutline,
    });
  }

  logout(): void {
    this.authService.logout().subscribe({
      error: () => this.authService.clearSession(),
    });
  }
}