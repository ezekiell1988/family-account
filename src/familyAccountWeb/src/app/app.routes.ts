import { Routes } from "@angular/router";
import {
  HomePage,
  ErrorPage,
  LoginPage,
} from "./pages";
import { AuthGuard } from "./shared/guards";

export const routes: Routes = [
  {
    path: "",
    redirectTo: "/home",
    pathMatch: "full",
  },
  {
    path: "login",
    component: LoginPage,
    data: { title: "Iniciar Sesión" },
  },
  {
    path: "home",
    component: HomePage,
    data: { title: "Home" },
    canActivate: [AuthGuard],
  },
  {
    path: "**",
    component: ErrorPage,
    data: { title: "404 Error" },
  },
];
