import { Routes } from "@angular/router";
import {
  AccountsPage,
  BankMovementTypesPage,
  BanksPage,
  AccountingEntriesPage,
  BankMovementsPage,
  PurchaseInvoicesPage,
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
    path: "maintenance/bank-movement-types",
    component: BankMovementTypesPage,
    data: { title: "Tipos de Movimiento Bancario" },
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
    path: "process/bank-movements",
    component: BankMovementsPage,
    data: { title: "Movimientos Bancarios" },
    canActivate: [AuthGuard],
  },
  {
    path: "process/purchase-invoices",
    component: PurchaseInvoicesPage,
    data: { title: "Facturas de Compra" },
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
