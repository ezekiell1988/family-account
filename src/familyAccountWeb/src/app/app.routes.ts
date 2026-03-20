import { Routes } from "@angular/router";
import {
  HomePage,
  ErrorPage,
  LoginPage,
  CampaignsListPage,
  CampaignDetailPage,
  ClientPage,
  ContaboStoragePage,
  RedisCachePage,
  HangfireJobsPage,
  EmailTemplatesPage,
  EmailTemplatePreviewPage,
  MultimediaPage,
  SendgridSuppressionGroupsPage,
  SendgridListsPage,
  LoginsPage,
  IntegrationOnePage,
  IntegrationTwoPage,
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
    path: "reports/campaigns",
    component: CampaignsListPage,
    data: { title: "Campañas" },
    canActivate: [AuthGuard],
  },
  {
    path: "reports/campaigns/:id",
    component: CampaignDetailPage,
    data: { title: "Detalles de Campaña" },
    canActivate: [AuthGuard],
  },
  {
    path: "maintenance/client",
    component: ClientPage,
    data: { title: "Configuración de Clientes" },
    canActivate: [AuthGuard],
  },
  {
    path: "maintenance/contabo-storage",
    component: ContaboStoragePage,
    data: { title: "Almacenamiento Contabo" },
    canActivate: [AuthGuard],
  },
  {
    path: "maintenance/redis-cache",
    component: RedisCachePage,
    data: { title: "Redis Cache" },
    canActivate: [AuthGuard],
  },
  {
    path: "maintenance/hangfire-jobs",
    component: HangfireJobsPage,
    data: { title: "Hangfire Jobs" },
    canActivate: [AuthGuard],
  },
  {
    path: "maintenance/email-templates",
    component: EmailTemplatesPage,
    data: { title: "Plantillas de Email" },
    canActivate: [AuthGuard],
  },
  {
    path: "maintenance/email-templates/:id/preview",
    component: EmailTemplatePreviewPage,
    data: { title: "Vista Previa de Plantilla" },
    canActivate: [AuthGuard],
  },
  {
    path: "maintenance/multimedia",
    component: MultimediaPage,
    data: { title: "Multimedia" },
    canActivate: [AuthGuard],
  },
  {
    path: "maintenance/sendgrid-suppression-groups",
    component: SendgridSuppressionGroupsPage,
    data: { title: "Suppression Groups" },
    canActivate: [AuthGuard],
  },
  {
    path: "maintenance/sendgrid-lists",
    component: SendgridListsPage,
    data: { title: "Listas de Contactos" },
    canActivate: [AuthGuard],
  },
  {
    path: "maintenance/logins",
    component: LoginsPage,
    data: { title: "Usuarios" },
    canActivate: [AuthGuard],
  },
  {
    path: "process/integration-one",
    component: IntegrationOnePage,
    data: { title: "Integración Facturas → Elasticsearch" },
    canActivate: [AuthGuard],
  },
  {
    path: "process/integration-two",
    component: IntegrationTwoPage,
    data: { title: "Integración Clientes → Elasticsearch" },
    canActivate: [AuthGuard],
  },
  {
    path: "**",
    component: ErrorPage,
    data: { title: "404 Error" },
  },
];
