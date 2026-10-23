import { Injectable, computed, inject } from '@angular/core';

import { AuthService } from '../auth/auth.service';
import { APP_MENU, MenuItem } from './menu.config';
import { MenuContext, filterMenu } from './menu-filter';

@Injectable({ providedIn: 'root' })
export class MenuService {
  private readonly authService = inject(AuthService);

  readonly visibleMenu = computed<MenuItem[]>(() => filterMenu(APP_MENU, this.context()));

  readonly fullMenu: readonly MenuItem[] = APP_MENU;

  private context(): MenuContext {
    const session = this.authService.sessionState();
    return {
      isAuthenticated: session.isAuthenticated,
      hasPermission: (permissionCode: string) => session.permissions.has(permissionCode.trim().toLowerCase())
    };
  }
}
