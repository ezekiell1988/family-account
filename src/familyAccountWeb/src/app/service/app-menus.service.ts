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
    },
    {
      icon: "fa fa-wrench",
      iconMobile: "settings-outline",
      title: "Mantenimiento",
      url: "/maintenance",
      caret: "true",
      roles: [1, 2],
      submenu: [
        {
          icon: "fa fa-calculator",
          iconMobile: "calculator-outline",
          title: "Cuentas Contables",
          url: "/maintenance/accounts",
          roles: [1, 2],
        },
        {
          icon: "fa fa-book",
          iconMobile: "book-outline",
          title: "Asientos Contables",
          url: "/process/accounting-entries",
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

