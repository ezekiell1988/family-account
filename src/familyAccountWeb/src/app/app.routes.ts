import { Routes } from "@angular/router";
import {
  AccountsPage,
  BanksPage,
  AccountingEntriesPage,
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
    path: "maintenance/accounts",
    component: AccountsPage,
    data: { title: "Cuentas Contables" },
    canActivate: [AuthGuard],
  },
  {
    path: "maintenance/banks",
    component: BanksPage,
    data: { title: "Bancos" },
    canActivate: [AuthGuard],
  },
  {
    path: "process/accounting-entries",
    component: AccountingEntriesPage,
    data: { title: "Asientos Contables" },
    canActivate: [AuthGuard],
  },
  {
    path: "home",
    component: AccountsPage,
    data: { title: "Cuentas Contables" },
    canActivate: [AuthGuard],
  },
  {
    path: "**",
    component: ErrorPage,
    data: { title: "404 Error" },
  },
];
