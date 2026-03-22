import { Component, OnInit, AfterViewInit, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router } from '@angular/router';
import { AppMenuService } from '../../../../service/app-menus.service';
import { AuthService } from '../../../../service';

declare var slideUp: any;
declare var slideToggle: any;

@Component({
  selector: 'top-menu-web',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, RouterModule],
  templateUrl: './top-menu-web.component.html',
})
export class TopMenuWebComponent implements OnInit, AfterViewInit {
  menus: any[] = [];

  constructor(
    private appMenuService: AppMenuService,
    private authService: AuthService,
    private router: Router,
  ) {}

  ngOnInit(): void {
    const userRoles = this.authService.currentUser()?.roles ?? [];
    this.menus = this.appMenuService.getMenuForRoles(userRoles);
  }

  ngAfterViewInit(): void {
    setTimeout(() => {
      this.handleTopMenuSubMenu();

      window.addEventListener('resize', () => {
        if (window.innerWidth >= 768) {
          (document.querySelector('.app-top-menu') as HTMLElement)?.removeAttribute('style');
          (document.querySelector('.app-top-menu .menu') as HTMLElement)?.removeAttribute('style');
          document.querySelectorAll('.app-top-menu .menu-submenu').forEach((elm) => {
            (elm as HTMLElement).removeAttribute('style');
          });
          this.handleTopMenuMenuFocus();
        }
        this.handleTopMenuDrag('.app-top-menu');
      });

      if (window.innerWidth >= 768) {
        this.handleTopMenuMenuFocus();
        this.handleTopMenuDrag('.app-top-menu');
      }
    }, 50);
  }

  isActive(path: string): boolean {
    return this.router.url === '/' + path;
  }

  isChildActive(menus: any[]): boolean {
    return menus.some((menu) => this.router.url === '/' + menu.url);
  }

  handleTopMenuMenuFocus(): void {
    const targetMenu = document.querySelector('.app-top-menu .menu') as HTMLElement | null;
    if (!targetMenu) return;

    const targetMenuStyle = window.getComputedStyle(targetMenu);
    const bodyStyle       = window.getComputedStyle(document.body);
    const targetCss       = bodyStyle.getPropertyValue('direction') === 'rtl' ? 'margin-right' : 'margin-left';
    const marginLeft      = parseInt(targetMenuStyle.getPropertyValue(targetCss));
    const viewNav         = document.querySelector('.app-top-menu') as HTMLElement | null;
    const viewWidth       = viewNav ? viewNav.clientWidth : 0;
    let prevWidth = 0;
    let fullWidth = 0;

    const controlPrevObj  = targetMenu.querySelector('.menu-control-start') as HTMLElement | null;
    const controlPrevWidth = controlPrevObj ? controlPrevObj.clientWidth : 0;
    const controlNextObj  = targetMenu.querySelector('.menu-control-end') as HTMLElement | null;
    const controlNextWidth = controlNextObj ? controlNextObj.clientWidth : 0;
    let controlWidth = 0;

    Array.from(document.querySelectorAll('.app-top-menu .menu > .menu-item') as NodeListOf<HTMLElement>).forEach((elm) => {
      if (!elm.classList.contains('menu-control')) {
        fullWidth += elm.clientWidth;
        if (!elm.classList.contains('active')) prevWidth += elm.clientWidth;
      }
    });

    let elm = targetMenu.querySelector('.menu-control.menu-control-end') as HTMLElement | null;
    if (elm && prevWidth !== fullWidth && fullWidth >= viewWidth) {
      elm.classList.add('show'); controlWidth += controlNextWidth;
    } else elm?.classList.remove('show');

    elm = targetMenu.querySelector('.menu-control.menu-control-start') as HTMLElement | null;
    if (elm && prevWidth >= viewWidth && fullWidth >= viewWidth) {
      elm.classList.add('show');
    } else elm?.classList.remove('show');

    if (prevWidth >= viewWidth) {
      const finalScrollWidth = prevWidth - viewWidth + controlWidth;
      if (bodyStyle.getPropertyValue('direction') !== 'rtl') {
        targetMenu.style.marginLeft = `-${finalScrollWidth}px`;
      } else {
        targetMenu.style.marginRight = `-${finalScrollWidth}px`;
      }
    }
  }

  handleTopMenuDrag(containerClassName: string): void {
    const container = document.querySelector(containerClassName) as HTMLElement | null;
    if (!container) return;

    const menu     = container.querySelector('.menu') as HTMLElement | null;
    const menuItem = menu ? Array.from(menu.querySelectorAll('.menu-item:not(.menu-control)') as NodeListOf<HTMLElement>) : [];
    let startX: number | null, scrollLeft: number, mouseDown = false;
    let menuWidth = 0, maxScroll = 0;
    menuItem.forEach((el) => { menuWidth += el.offsetWidth; });

    const updateScroll = (x: number) => {
      if (!startX || !mouseDown || !menu) return;
      const walkX          = (x - startX) * 1;
      let totalMarginLeft  = scrollLeft + walkX;
      const controlEnd     = menu.querySelector('.menu-control.menu-control-end') as HTMLElement | null;
      const controlStart   = menu.querySelector('.menu-control.menu-control-start') as HTMLElement | null;

      if (totalMarginLeft <= maxScroll) { totalMarginLeft = maxScroll; controlEnd?.classList.remove('show'); }
      else controlEnd?.classList.add('show');

      if (menuWidth < window.innerWidth) controlStart?.classList.remove('show');
      if (maxScroll > 0)                 controlEnd?.classList.remove('show');
      if (totalMarginLeft > 0)           { totalMarginLeft = 0; controlStart?.classList.remove('show'); }
      else                               controlStart?.classList.add('show');

      menu.style.marginLeft = totalMarginLeft + 'px';
    };

    container.addEventListener('mousedown',  (e) => { mouseDown = true; startX = e.pageX; scrollLeft = parseInt(menu?.style.marginLeft || '0', 10); maxScroll = window.innerWidth - menuWidth; });
    container.addEventListener('touchstart', (e) => { mouseDown = true; startX = e.targetTouches[0].pageX; scrollLeft = parseInt(menu?.style.marginLeft || '0', 10); maxScroll = window.innerWidth - menuWidth; });
    container.addEventListener('mouseup',    () => { mouseDown = false; });
    container.addEventListener('touchend',   () => { mouseDown = false; });
    container.addEventListener('mousemove',  (e) => { if (window.innerWidth >= 768) { e.preventDefault(); updateScroll(e.pageX); } });
    container.addEventListener('touchmove',  (e) => { if (window.innerWidth >= 768) { e.preventDefault(); updateScroll(e.targetTouches[0].pageX); } });
  }

  handleTopMenuControlAction(event: MouseEvent, direction: string): void {
    const obj = (event.currentTarget as HTMLElement).closest('.menu') as HTMLElement | null;
    if (!obj) return;

    const objStyle    = window.getComputedStyle(obj);
    const bodyStyle   = window.getComputedStyle(document.body);
    const targetCss   = bodyStyle.getPropertyValue('direction') === 'rtl' ? 'margin-right' : 'margin-left';
    const marginLeft  = parseInt(objStyle.getPropertyValue(targetCss), 10);
    const containerWidth = (document.querySelector('.app-top-menu') as HTMLElement).clientWidth
      - (document.querySelector('.app-top-menu') as HTMLElement).clientHeight * 2;

    let totalWidth = 0, finalScrollWidth = 0;
    const controlPrevWidth = (obj.querySelector('.menu-control-start') as HTMLElement | null)?.clientWidth ?? 0;
    const controlNextWidth = (obj.querySelector('.menu-control-end') as HTMLElement | null)?.clientWidth ?? 0;
    const controlWidth = controlPrevWidth + controlNextWidth;

    Array.from(obj.querySelectorAll('.menu-item') as NodeListOf<HTMLElement>).forEach((elm) => {
      if (!elm.classList.contains('.menu-control')) totalWidth += elm.clientWidth;
    });

    if (direction === 'next') {
      const widthLeftNext = totalWidth + marginLeft - containerWidth;
      if (widthLeftNext <= containerWidth) {
        finalScrollWidth = widthLeftNext - marginLeft - controlWidth;
        setTimeout(() => (obj.querySelector('.menu-control.menu-control-end') as HTMLElement | null)?.classList.remove('show'), 300);
      } else {
        finalScrollWidth = containerWidth - marginLeft - controlWidth;
      }
      if (finalScrollWidth !== 0) {
        obj.style.transitionProperty = 'height, margin, padding';
        obj.style.transitionDuration = '300ms';
        bodyStyle.getPropertyValue('direction') !== 'rtl'
          ? (obj.style.marginLeft = `-${finalScrollWidth}px`)
          : (obj.style.marginRight = `-${finalScrollWidth}px`);
        setTimeout(() => {
          obj.style.transitionProperty = '';
          obj.style.transitionDuration = '';
          (obj.querySelector('.menu-control.menu-control-start') as HTMLElement | null)?.classList.add('show');
        }, 300);
      }
    } else {
      const widthLeftPrev = -marginLeft;
      if (widthLeftPrev <= containerWidth) {
        (obj.querySelector('.menu-control.menu-control-start') as HTMLElement | null)?.classList.remove('show');
        finalScrollWidth = 0;
      } else {
        finalScrollWidth = widthLeftPrev - containerWidth + controlWidth;
      }
      obj.style.transitionProperty = 'height, margin, padding';
      obj.style.transitionDuration  = '300ms';
      bodyStyle.getPropertyValue('direction') !== 'rtl'
        ? (obj.style.marginLeft  = `-${finalScrollWidth}px`)
        : (obj.style.marginRight = `-${finalScrollWidth}px`);
      setTimeout(() => {
        obj.style.transitionProperty = '';
        obj.style.transitionDuration  = '';
        (obj.querySelector('.menu-control.menu-control-end') as HTMLElement | null)?.classList.add('show');
      }, 300);
    }
  }

  handleTopMenuToggle(menus: HTMLElement[], forMobile = false): void {
    menus.forEach((menu) => {
      menu.onclick = (e: MouseEvent) => {
        e.preventDefault();
        if (!forMobile || (forMobile && document.body.clientWidth < 768)) {
          const target = menu.nextElementSibling as HTMLElement | null;
          menus.forEach((m) => {
            const other = m.nextElementSibling as HTMLElement | null;
            if (other && other !== target) {
              slideUp(other);
              other.closest('.menu-item')?.classList.remove('expand');
              other.closest('.menu-item')?.classList.add('closed');
            }
          });
          if (target) slideToggle(target);
        }
      };
    });
  }

  handleTopMenuSubMenu(): void {
    const base      = '.app-top-menu .menu > .menu-item.has-sub';
    const subBase   = ' > .menu-submenu > .menu-item.has-sub';
    this.handleTopMenuToggle(Array.from(document.querySelectorAll<HTMLElement>(base + ' > .menu-link')), true);
    this.handleTopMenuToggle(Array.from(document.querySelectorAll<HTMLElement>(base + subBase + ' > .menu-link')));
    this.handleTopMenuToggle(Array.from(document.querySelectorAll<HTMLElement>(base + subBase + subBase + ' > .menu-link')));
  }
}
