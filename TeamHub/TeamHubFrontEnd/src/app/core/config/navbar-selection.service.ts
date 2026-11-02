import { Injectable, computed, inject } from '@angular/core';

import { AuthService } from '../auth/auth.service';
import { MenuService } from './menu.service';

@Injectable({ providedIn: 'root' })
export class NavbarSelectionService {
  private readonly menu = inject(MenuService);
  private readonly auth = inject(AuthService);

  readonly useAdminNavbar = computed(() =>
    this.menu.visibleMenu().some((item) => item.visibility !== 'anonymous')
    || this.gearVisible());

  gearVisible(): boolean {
    const has = (permission: string): boolean => this.auth.hasPermission(permission);
    return has('setting.gear.EmployeeRole')
      || has('setting.gear.RolePermission')
      || has('setting.gear.role.view')
      || has('setting.gear.manage.permissions.view');
  }
}
