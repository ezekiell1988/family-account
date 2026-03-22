import { Component, Input, Output, EventEmitter, ElementRef, HostListener, ViewChild, OnInit, AfterViewChecked, AfterViewInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { NgScrollbarModule } from 'ngx-scrollbar';
import { slideUp } from '../../../../composables/slideUp.js';
import { slideToggle } from '../../../../composables/slideToggle.js';
import { AppMenuService } from '../../../../service/app-menus.service';
import { AppSettings } from '../../../../service/app-settings.service';
import { AuthService, LoggerService, Logger } from '../../../../service';
import { FloatSubMenuComponent } from '../../../float-sub-menu/float-sub-menu.component';
import { UserData } from '../../../../shared/models';
import { TranslatePipe } from '@ngx-translate/core';

@Component({
  selector: 'sidebar-web',
  templateUrl: './sidebar-web.component.html',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    NgScrollbarModule,
    FloatSubMenuComponent,
    TranslatePipe,
  ]
})
export class SidebarWebComponent implements OnInit, AfterViewChecked, AfterViewInit {
  private readonly logger: Logger;
  menus: any[] = [];
  currentUser: UserData | null = null;

  @ViewChild('sidebarScrollbar', { static: false }) private sidebarScrollbar: ElementRef;
  @Output() appSidebarMinifiedToggled = new EventEmitter<boolean>();
  @Output() hideMobileSidebar = new EventEmitter<boolean>();
  @Output() setPageFloatSubMenu = new EventEmitter();
  @Output() appSidebarMobileToggled = new EventEmitter<boolean>();

  @Input() appSidebarTransparent: any;
  @Input() appSidebarGrid: any;
  @Input() appSidebarFixed: any;
  @Input() appSidebarMinified: any;

  appSidebarFloatSubMenu: any;
  appSidebarFloatSubMenuHide: any;
  appSidebarFloatSubMenuHideTime = 250;
  appSidebarFloatSubMenuTop: any;
  appSidebarFloatSubMenuLeft = '60px';
  appSidebarFloatSubMenuRight: any;
  appSidebarFloatSubMenuBottom: any;
  appSidebarFloatSubMenuArrowTop: any;
  appSidebarFloatSubMenuArrowBottom: any;
  appSidebarFloatSubMenuLineTop: any;
  appSidebarFloatSubMenuLineBottom: any;
  appSidebarFloatSubMenuOffset: any;

  scrollTop: any;

  constructor(
    public appSettings: AppSettings,
    private appMenuService: AppMenuService,
    private authService: AuthService,
    loggerService: LoggerService
  ) {
    this.logger = loggerService.getLogger('SidebarWebComponent');
  }

  ngOnInit(): void {
    const userRoles = this.authService.currentUser()?.roles ?? [];
    this.menus = this.appMenuService.getMenuForRoles(userRoles);
    this.currentUser = this.authService.currentUser();
  }

  toggleNavProfile(e: any): void {
    e.preventDefault();
    var targetSidebar = <HTMLElement>document.querySelector('.app-sidebar:not(.app-sidebar-end)');
    var targetMenu = e.target.closest('.menu-profile');
    var targetProfile = <HTMLElement>document.querySelector('#appSidebarProfileMenu');
    var expandTime = (targetSidebar && targetSidebar.getAttribute('data-disable-slide-animation')) ? 0 : 250;

    if (targetProfile && targetProfile.style) {
      if (targetProfile.style.display == 'block') {
        targetMenu.classList.remove('active');
      } else {
        targetMenu.classList.add('active');
      }
      slideToggle(targetProfile, expandTime);
      targetProfile.classList.toggle('expand');
    }
  }

  toggleAppSidebarMinified(): void {
    this.appSidebarMinifiedToggled.emit(!this.appSettings.appSidebarMinified);
    this.scrollTop = 40;
  }

  toggleAppSidebarMobile(): void {
    this.appSidebarMobileToggled.emit(true);
  }

  calculateAppSidebarFloatSubMenuPosition(): void {
    var targetTop = this.appSidebarFloatSubMenuOffset.top;
    var direction = document.documentElement.getAttribute('dir');
    var windowHeight = window.innerHeight;

    setTimeout(() => {
      let targetElm = <HTMLElement>document.querySelector('.app-sidebar-float-submenu-container');
      let targetSidebar = <HTMLElement>document.getElementById('sidebar');
      var targetHeight = targetElm.offsetHeight;

      this.appSidebarFloatSubMenuLeft = 'auto';
      this.appSidebarFloatSubMenuRight = 'auto';

      if (direction === 'rtl') {
        this.appSidebarFloatSubMenuRight = (window.innerWidth - targetSidebar.offsetLeft) + 'px';
        this.logger.debug('appSidebarFloatSubMenuRight:', this.appSidebarFloatSubMenuRight);
      } else {
        this.appSidebarFloatSubMenuLeft = (this.appSidebarFloatSubMenuOffset.width + targetSidebar.offsetLeft) + 'px';
      }

      if ((windowHeight - targetTop) > targetHeight) {
        this.appSidebarFloatSubMenuTop = this.appSidebarFloatSubMenuOffset.top + 'px';
        this.appSidebarFloatSubMenuBottom = 'auto';
        this.appSidebarFloatSubMenuArrowTop = '20px';
        this.appSidebarFloatSubMenuArrowBottom = 'auto';
        this.appSidebarFloatSubMenuLineTop = '20px';
        this.appSidebarFloatSubMenuLineBottom = 'auto';
      } else {
        this.appSidebarFloatSubMenuTop = 'auto';
        this.appSidebarFloatSubMenuBottom = '0';
        var arrowBottom = (windowHeight - targetTop) - 21;
        this.appSidebarFloatSubMenuArrowTop = 'auto';
        this.appSidebarFloatSubMenuArrowBottom = arrowBottom + 'px';
        this.appSidebarFloatSubMenuLineTop = '20px';
        this.appSidebarFloatSubMenuLineBottom = arrowBottom + 'px';
      }
    }, 0);
  }

  showAppSidebarFloatSubMenu(menu: any, e: any): void {
    if (this.appSettings.appSidebarMinified) {
      clearTimeout(this.appSidebarFloatSubMenuHide);
      this.appSidebarFloatSubMenu = menu;
      this.appSidebarFloatSubMenuOffset = e.target.getBoundingClientRect();
      this.calculateAppSidebarFloatSubMenuPosition();
    }
  }

  hideAppSidebarFloatSubMenu(): void {
    this.appSidebarFloatSubMenuHide = setTimeout(() => {
      this.appSidebarFloatSubMenu = '';
    }, this.appSidebarFloatSubMenuHideTime);
  }

  remainAppSidebarFloatSubMenu(): void {
    clearTimeout(this.appSidebarFloatSubMenuHide);
  }

  appSidebarSearch(e: any): void {
    var targetValue = e.target.value;
    targetValue = targetValue.toLowerCase();

    if (targetValue) {
      var elms = [].slice.call(document.querySelectorAll('.app-sidebar:not(.app-sidebar-end) .menu > .menu-item:not(.menu-profile):not(.menu-header):not(.menu-search), .app-sidebar:not(.app-sidebar-end) .menu-submenu > .menu-item'));
      if (elms) { elms.map(function(elm: any) { elm.classList.add('d-none'); }); }

      var elms2 = [].slice.call(document.querySelectorAll('.app-sidebar:not(.app-sidebar-end) .has-text'));
      if (elms2) { elms2.map(function(elm: any) { elm.classList.remove('has-text'); }); }

      var elms3 = [].slice.call(document.querySelectorAll('.app-sidebar:not(.app-sidebar-end) .expand'));
      if (elms3) { elms3.map(function(elm: any) { elm.classList.remove('expand'); }); }

      var elms4 = [].slice.call(document.querySelectorAll('.app-sidebar:not(.app-sidebar-end) .menu > .menu-item:not(.menu-profile):not(.menu-header):not(.menu-search) > .menu-link, .app-sidebar .menu-submenu > .menu-item > .menu-link'));
      if (elms4) {
        elms4.map(function(elm: any) {
          var targetText = elm.textContent;
          targetText = targetText.toLowerCase();
          if (targetText.search(targetValue) > -1) {
            var targetElm = elm.closest('.menu-item');
            if (targetElm) { targetElm.classList.remove('d-none'); targetElm.classList.add('has-text'); }

            var targetElmSub = elm.closest('.menu-item.has-sub');
            if (targetElmSub) {
              var targetElmSubHidden = targetElmSub.querySelector('.menu-submenu .menu-item.d-none');
              if (targetElmSubHidden) { targetElmSubHidden.classList.remove('d-none'); }
            }

            var targetElmContainer = elm.closest('.menu-submenu');
            if (targetElmContainer) {
              targetElmContainer.style.display = 'block';
              var targetElmNoText = targetElmContainer.querySelector('.menu-item:not(.has-text)');
              if (targetElmNoText) { targetElmNoText.classList.add('d-none'); }
              var targetElmHasSub = elm.closest('.has-sub:not(.has-text)');
              if (targetElmHasSub) {
                targetElmHasSub.classList.remove('d-none');
                targetElmHasSub.classList.add('expand');
                var targetElmHasSubParent = targetElmHasSub.closest('.has-sub:not(.has-text)');
                if (targetElmHasSubParent) {
                  targetElmHasSubParent.classList.remove('d-none');
                  targetElmHasSubParent.classList.add('expand');
                }
              }
            }
          }
        });
      }
    } else {
      var elms5 = [].slice.call(document.querySelectorAll('.app-sidebar:not(.app-sidebar-end) .menu > .menu-item:not(.menu-profile):not(.menu-header):not(.menu-search).has-sub .menu-submenu'));
      if (elms5) { elms5.map(function(elm: any) { elm.removeAttribute('style'); }); }

      var elms6 = [].slice.call(document.querySelectorAll('.app-sidebar:not(.app-sidebar-end) .menu > .menu-item:not(.menu-profile):not(.menu-header):not(.menu-search)'));
      if (elms6) { elms6.map(function(elm: any) { elm.classList.remove('d-none'); }); }

      var elms7 = [].slice.call(document.querySelectorAll('.app-sidebar:not(.app-sidebar-end) .menu-submenu > .menu-item'));
      if (elms7) { elms7.map(function(elm: any) { elm.classList.remove('d-none'); }); }

      var elms8 = [].slice.call(document.querySelectorAll('.app-sidebar:not(.app-sidebar-end) .expand'));
      if (elms8) { elms8.map(function(elm: any) { elm.classList.remove('expand'); }); }
    }
  }

  @HostListener('scroll', ['$event'])
  onScroll(event: any): void {
    this.scrollTop = (this.appSettings.appSidebarMinified) ? event.srcElement.scrollTop + 40 : 0;
    if (typeof(Storage) !== 'undefined') {
      localStorage.setItem('sidebarScroll', event.srcElement.scrollTop);
    }
  }

  expandCollapseSubmenu(currentMenu: any, allMenus: any[], rla: any): void {
    allMenus.forEach(menu => {
      if (menu !== currentMenu && menu.state !== 'active') {
        menu.state = 'collapsed';
      }
    });
    if (currentMenu.state === 'expand' || (rla.isActive && currentMenu.state !== 'collapsed')) {
      currentMenu.state = 'collapsed';
    } else {
      currentMenu.state = 'expand';
    }
  }

  logout(): void {
    this.authService.logout().subscribe({
      next: () => {
        this.logger.success('Sesión cerrada exitosamente');
      },
      error: (error) => {
        this.logger.error('Error al cerrar sesión:', error);
        this.authService.clearSession();
      }
    });
  }

  ngAfterViewChecked(): void {
    if (typeof(Storage) !== 'undefined' && localStorage['sidebarScroll']) {
      if (this.sidebarScrollbar && this.sidebarScrollbar.nativeElement) {
        this.sidebarScrollbar.nativeElement.scrollTop = localStorage['sidebarScroll'];
      }
    }
  }

  ngAfterViewInit(): void {
    var handleSidebarMenuToggle = function(menus: any[], expandTime: number) {
      menus.map(function(menu: any) {
        menu.onclick = function(e: any) {
          e.preventDefault();
          var target = this.nextElementSibling;

          menus.map(function(m: any) {
            var otherTarget = m.nextElementSibling;
            if (otherTarget !== target) {
              slideUp(otherTarget, expandTime);
              otherTarget.closest('.menu-item').classList.remove('expand');
              otherTarget.closest('.menu-item').classList.add('closed');
            }
          });

          var targetItemElm = target.closest('.menu-item');
          if (targetItemElm.classList.contains('expand') || (targetItemElm.classList.contains('active') && !target.style.display)) {
            targetItemElm.classList.remove('expand');
            targetItemElm.classList.add('closed');
            slideToggle(target, expandTime);
          } else {
            targetItemElm.classList.add('expand');
            targetItemElm.classList.remove('closed');
            slideToggle(target, expandTime);
          }
        };
      });
    };

    var targetSidebar = document.querySelector('.app-sidebar:not(.app-sidebar-end)');
    var expandTime = (targetSidebar && targetSidebar.getAttribute('data-disable-slide-animation')) ? 0 : 300;

    var menuBaseSelector = '.app-sidebar .menu > .menu-item.has-sub';
    var submenuBaseSelector = ' > .menu-submenu > .menu-item.has-sub';

    var menuLinkSelector = menuBaseSelector + ' > .menu-link';
    var menus = [].slice.call(document.querySelectorAll(menuLinkSelector));
    handleSidebarMenuToggle(menus, expandTime);

    var submenuLvl1Selector = menuBaseSelector + submenuBaseSelector;
    var submenusLvl1 = [].slice.call(document.querySelectorAll(submenuLvl1Selector + ' > .menu-link'));
    handleSidebarMenuToggle(submenusLvl1, expandTime);

    var submenuLvl2Selector = menuBaseSelector + submenuBaseSelector + submenuBaseSelector;
    var submenusLvl2 = [].slice.call(document.querySelectorAll(submenuLvl2Selector + ' > .menu-link'));
    handleSidebarMenuToggle(submenusLvl2, expandTime);
  }
}
