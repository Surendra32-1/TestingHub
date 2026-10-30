import { CanActivateFn, Router } from '@angular/router';
import { inject } from '@angular/core';
import { catchError, map, of } from 'rxjs';

import { AuthService } from './auth.service';
import { MASTER_MENU_PERMISSIONS } from '../config/menu.config';

export const authenticatedGuard: CanActivateFn = (_route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (authService.isAuthenticated) {
    return true;
  }

  return authService.restoreSession().pipe(
    map(() => true),
    catchError(() => {
      authService.clearSession();
      return of(router.createUrlTree(['/login'], { queryParams: { returnUrl: state.url } }));
    })
  );
};

export const authGuard = authenticatedGuard;

export const signedInLandingPath = (authService: AuthService): string => {
  if (authService.hasPermission('administration.view')) {
    return '/administration';
  }

  return authService.hasPermission('people.allemployee.search.view') ? '/people' : '/home';
};

export const signedInLandingGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (authService.isAuthenticated) {
    return router.createUrlTree([signedInLandingPath(authService)]);
  }

  return authService.restoreSession().pipe(
    map(() => router.createUrlTree([signedInLandingPath(authService)])),
    catchError(() => {
      authService.clearSession();
      return of(true);
    })
  );
};

export const anonymousHomeGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (!authService.isAuthenticated) {
    return true;
  }

  const landingPath = signedInLandingPath(authService);
  return landingPath === '/home' ? true : router.createUrlTree([landingPath]);
};

export const adminGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  return authService.hasRole('Admin') ? true : router.createUrlTree(['/home']);
};

const rbacGuard = (permission: string): CanActivateFn => () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  return authService.hasPermission(permission)
    ? true
    : router.createUrlTree([signedInLandingPath(authService)]);
};

export const employeeRoleMatrixGuard = rbacGuard('setting.gear.EmployeeRole');

export const rolePermissionMatrixGuard = rbacGuard('setting.gear.RolePermission');

export const resignationViewGuard = rbacGuard('resignations.view');

export const resignationAddGuard = rbacGuard('resignations.add');

export const administrationCreateGuard = rbacGuard('administration.create');

export const administrationUpdateGuard = rbacGuard('administration.update');

export const administrationReleaseGuard = rbacGuard('administration.release');

export const administrationViewGuard = rbacGuard('administration.view');

export const peopleSearchViewGuard = rbacGuard('people.allemployee.search.view');

export const profileEditGuard = rbacGuard('profile.edit');

export const terminationViewGuard = rbacGuard('terminations.view');

export const terminationAddGuard = rbacGuard('terminations.add');

export const employeeTerminationAddGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  return authService.hasPermission('administration.terminate') || authService.hasPermission('terminations.add')
    ? true
    : router.createUrlTree([signedInLandingPath(authService)]);
};

export const masterGuard: CanActivateFn = (route) => {
  const authService = inject(AuthService);
  const router = inject(Router);
  const masterKey = route.paramMap.get('masterKey') ?? '';
  const permissions = masterKey === 'permission'
    ? ['setting.gear.manage.permissions.view']
    : masterKey === 'role'
      ? ['setting.gear.role.view']
      : MASTER_MENU_PERMISSIONS[masterKey] ?? [`master.${masterKey}.view`];

  return permissions.some((permission) => authService.hasPermission(permission))
    ? true
    : router.createUrlTree(['/home']);
};
