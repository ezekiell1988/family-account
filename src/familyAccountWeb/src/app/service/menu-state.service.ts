import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';

/**
 * Servicio para gestionar el estado del menú
 * Centraliza el control de apertura/cierre del menú lateral
 */
@Injectable({
  providedIn: 'root'
})
export class MenuStateService {
  private isMenuOpenSubject = new BehaviorSubject<boolean>(false);
  private isMenuCollapsedSubject = new BehaviorSubject<boolean>(false);
  private isMobileViewSubject = new BehaviorSubject<boolean>(false);

  constructor() {
    this.checkMobileView();
    window.addEventListener('resize', () => this.checkMobileView());
  }

  /**
   * Observable para el estado de apertura del menú
   */
  get isMenuOpen$(): Observable<boolean> {
    return this.isMenuOpenSubject.asObservable();
  }

  /**
   * Observable para el estado de colapso del menú
   */
  get isMenuCollapsed$(): Observable<boolean> {
    return this.isMenuCollapsedSubject.asObservable();
  }

  /**
   * Observable para detectar vista móvil
   */
  get isMobileView$(): Observable<boolean> {
    return this.isMobileViewSubject.asObservable();
  }

  /**
   * Estado actual del menú (abierto/cerrado)
   */
  get isMenuOpen(): boolean {
    return this.isMenuOpenSubject.value;
  }

  /**
   * Estado actual del menú (colapsado/expandido)
   */
  get isMenuCollapsed(): boolean {
    return this.isMenuCollapsedSubject.value;
  }

  /**
   * Estado actual de vista móvil
   */
  get isMobileView(): boolean {
    return this.isMobileViewSubject.value;
  }

  /**
   * Abrir el menú lateral
   */
  openMenu(): void {
    this.isMenuOpenSubject.next(true);
  }

  /**
   * Cerrar el menú lateral
   */
  closeMenu(): void {
    this.isMenuOpenSubject.next(false);
  }

  /**
   * Alternar estado del menú (abrir/cerrar)
   */
  toggleMenu(): void {
    this.isMenuOpenSubject.next(!this.isMenuOpenSubject.value);
  }

  /**
   * Colapsar el menú
   */
  collapseMenu(): void {
    this.isMenuCollapsedSubject.next(true);
  }

  /**
   * Expandir el menú
   */
  expandMenu(): void {
    this.isMenuCollapsedSubject.next(false);
  }

  /**
   * Alternar estado de colapso del menú
   */
  toggleCollapse(): void {
    this.isMenuCollapsedSubject.next(!this.isMenuCollapsedSubject.value);
  }

  /**
   * Verificar si estamos en vista móvil
   */
  private checkMobileView(): void {
    const isMobile = window.innerWidth <= 768;
    this.isMobileViewSubject.next(isMobile);
    
    // En vista móvil, cerrar el menú por defecto
    if (isMobile) {
      this.closeMenu();
    }
  }

  /**
   * Reiniciar estado del menú
   */
  resetMenuState(): void {
    this.isMenuOpenSubject.next(false);
    this.isMenuCollapsedSubject.next(false);
  }
}