import { Injectable, computed, inject } from '@angular/core';

import { AuthService } from '../auth/auth.service';
import { MenuService } from './menu.service';

/**
 * Chooses which navbar structure a signed-in user should see, purely from the
 * existing RolePermissions-driven menu data. No new permissions are introduced.
 *
 * - Navbar 1 (employee): People + Profile only.
 * - Navbar 2 (admin): any admin-module navigation is visible (Administration,
 *   Resignations, Organization Master, Cotivity) or the settings gear is visible.
 *
 * The People entry (`id: 'employee'`) is intentionally excluded from the admin
 * trigger so a user who has both People and admin permissions lands on Navbar 2.
 */
@Injectable({ providedIn: 'root' })
export class NavbarSelectionService {
  private readonly menu = inject(MenuService);
  private readonly auth = inject(AuthService);

  readonly useAdminNavbar = computed(() =>
    this.menu.visibleMenu().some((item) => item.id !== 'employee' && item.visibility !== 'anonymous')
    || this.gearVisible());

  gearVisible(): boolean {
    const has = (permission: string): boolean => this.auth.hasPermission(permission);
    return has('setting.gear.EmployeeRole')
      || has('setting.gear.RolePermission')
      || has('setting.gear.role.view')
      || has('setting.gear.manage.permissions.view');
  }
}
