import { Injectable } from "@angular/core";

/**
 * Interfaz para items del menú
 */
export interface MenuItem {
  icon?: string;
  iconMobile?: string;
  title: string;
  url: string;
  caret?: string;
  /** IDs de roles que pueden ver este item. Si está vacío/undefined = visible para todos. */
  roles?: number[];
  submenu?: MenuItem[];
}

/**
 * Servicio para gestionar el menú de la aplicación
 * Siguiendo buenas prácticas de Angular 20+
 */
@Injectable({
  providedIn: "root",
})
export class AppMenuService {
  /**
   * Configuración del menú centralizada
   *
   * Roles:
   *   1 = DEV        → acceso total
   *   2 = ADMIN      → todo excepto Redis, Hangfire y Contabo
   *   3 = LOCAL      → solo Inicio + Reportes/Campañas
   *   4 = SUPPORT    → solo Inicio
   */
  private readonly menuConfig: MenuItem[] = [
    {
      icon: "fa fa-sitemap",
      iconMobile: "home-outline",
      title: "Inicio",
      url: "/home",
      // Visible para todos los roles
    },
    {
      icon: "fa fa-arrows-rotate",
      iconMobile: "sync-outline",
      title: "Procesos",
      url: "/process",
      caret: "true",
      roles: [1, 2],
      submenu: [
        {
          url: "/process/integration-one",
          title: "Integración Facturas",
          icon: "fa fa-database",
          iconMobile: "server-outline",
          roles: [1, 2],
        },
        {
          url: "/process/integration-two",
          title: "Integración Clientes",
          icon: "fa fa-users",
          iconMobile: "people-outline",
          roles: [1, 2],
        },
      ],
    },
    {
      icon: "fa fa-chart-bar",
      iconMobile: "bar-chart-outline",
      title: "Reportes",
      url: "/reports",
      caret: "true",
      roles: [1, 2, 3],
      submenu: [
        {
          url: "/reports/campaigns",
          title: "Campañas",
          icon: "fa fa-envelope-open-text",
          iconMobile: "mail-outline",
          roles: [1, 2, 3],
        },
      ],
    },
    {
      icon: "fa fa-cogs",
      iconMobile: "settings-outline",
      title: "Mantenimiento",
      url: "/maintenance",
      caret: "true",
      roles: [1, 2],
      submenu: [
        {
          url: "/maintenance/client",
          title: "Configuración de Clientes",
          icon: "fa fa-building",
          iconMobile: "business-outline",
          roles: [1, 2],
        },
        {
          url: "/maintenance/contabo-storage",
          title: "Almacenamiento Contabo",
          icon: "fa fa-cloud",
          iconMobile: "cloud-outline",
          roles: [1],
        },
        {
          url: "/maintenance/redis-cache",
          title: "Redis Cache",
          icon: "fa fa-database",
          iconMobile: "server-outline",
          roles: [1],
        },
        {
          url: "/maintenance/hangfire-jobs",
          title: "Hangfire Jobs",
          icon: "fa fa-cog",
          iconMobile: "construct-outline",
          roles: [1],
        },
        {
          url: "/maintenance/email-templates",
          title: "Plantillas de Email",
          icon: "fa fa-file-code",
          iconMobile: "document-text-outline",
          roles: [1, 2],
        },
        {
          url: "/maintenance/multimedia",
          title: "Multimedia",
          icon: "fa fa-photo-video",
          iconMobile: "images-outline",
          roles: [1, 2],
        },
        {
          url: "/maintenance/sendgrid-suppression-groups",
          title: "Suppression Groups",
          icon: "fa fa-ban",
          iconMobile: "remove-circle-outline",
          roles: [1, 2],
        },
        {
          url: "/maintenance/sendgrid-lists",
          title: "Listas de Contactos",
          icon: "fa fa-list",
          iconMobile: "list-outline",
          roles: [1, 2],
        },
        {
          url: "/maintenance/logins",
          title: "Usuarios",
          icon: "fa fa-users",
          iconMobile: "people-outline",
          roles: [1, 2],
        },
      ],
    },
  ];

  /**
   * Retorna una copia profunda del menú completo (sin filtrar).
   * Usar `getMenuForRoles()` cuando se necesite filtrar por roles.
   */
  getAppMenus(): MenuItem[] {
    return JSON.parse(JSON.stringify(this.menuConfig));
  }

  /**
   * Retorna el menú filtrado según los roles del usuario.
   *
   * @param userRoleIds IDs de rol del usuario tal como llegan en `UserData.roles` desde el backend.
   */
  getMenuForRoles(userRoleIds: number[]): MenuItem[] {
    const userRoleIdSet = new Set(userRoleIds);

    const filterItems = (items: MenuItem[]): MenuItem[] =>
      items
        .filter(
          (item) =>
            !item.roles?.length || item.roles.some((r) => userRoleIdSet.has(r))
        )
        .map((item) => ({
          ...item,
          submenu: item.submenu ? filterItems(item.submenu) : undefined,
        }));

    return filterItems(JSON.parse(JSON.stringify(this.menuConfig)));
  }

  /**
   * Busca un item del menú por URL
   */
  findMenuItemByUrl(url: string): MenuItem | null {
    const search = (items: MenuItem[]): MenuItem | null => {
      for (const item of items) {
        if (item.url === url) return item;
        if (item.submenu) {
          const found = search(item.submenu);
          if (found) return found;
        }
      }
      return null;
    };
    return search(this.menuConfig);
  }

  /**
   * Obtiene todos los items del menú de forma plana
   */
  getFlatMenuItems(): MenuItem[] {
    const flatten = (items: MenuItem[]): MenuItem[] => {
      return items.reduce((acc, item) => {
        acc.push(item);
        if (item.submenu) {
          acc.push(...flatten(item.submenu));
        }
        return acc;
      }, [] as MenuItem[]);
    };
    return flatten(this.menuConfig);
  }
}

