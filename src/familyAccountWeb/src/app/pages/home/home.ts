import { Component, computed, inject, ChangeDetectionStrategy } from "@angular/core";
import { AppSettings, AuthService, LoggerService } from "../../service";
import { ResponsiveComponent } from "../../shared";
import { HomeWebComponent, HomeMobileComponent } from "./components";

@Component({
  selector: "home",
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: "./home.html",
  standalone: true,
  imports: [HomeWebComponent, HomeMobileComponent]
})
export class HomePage extends ResponsiveComponent {
  private readonly authService = inject(AuthService);
  private readonly logger = inject(LoggerService).getLogger("HomePage");

  readonly currentUser = this.authService.currentUser;
  readonly rolesLabel = computed(() => {
    const roles = this.currentUser()?.roles ?? [];
    return roles.length > 0 ? roles.join(", ") : "Sin roles asignados";
  });

  constructor(public appSettings: AppSettings) {
    super();
    this.logger.info("🏠 HomePage cargado");
  }

  logout(): void {
    this.authService.logout().subscribe({
      error: () => this.authService.clearSession(),
    });
  }
}