import { CanActivateFn, Router } from '@angular/router';
import { inject } from '@angular/core';
import { catchError, map, of } from 'rxjs';

import { AuthService } from './auth.service';

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

export const signedInLandingGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (authService.isAuthenticated) {
    return router.createUrlTree(['/people']);
  }

  return authService.restoreSession().pipe(
    map(() => router.createUrlTree(['/people'])),
    catchError(() => {
      authService.clearSession();
      return of(true);
    })
  );
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
    : router.createUrlTree(['/home']);
};

export const employeeRoleMatrixGuard = rbacGuard('user-roles.assign');

export const rolePermissionMatrixGuard = rbacGuard('role-permissions.manage');

export const resignationViewGuard = rbacGuard('resignations.view');

export const resignationAddGuard = rbacGuard('resignations.add');

export const peopleCreateGuard = rbacGuard('people.create');

export const peopleUpdateGuard = rbacGuard('people.update');

export const peopleReleaseGuard = rbacGuard('people.release');

export const terminationViewGuard = rbacGuard('terminations.view');

export const terminationAddGuard = rbacGuard('terminations.add');

export const masterGuard: CanActivateFn = (route) => {
  const authService = inject(AuthService);
  const router = inject(Router);
  const masterKey = route.paramMap.get('masterKey') ?? '';
  const permission = masterKey === 'permission' ? 'permissions.manage' : `master.${masterKey}`;

  return authService.hasPermission(permission)
    ? true
    : router.createUrlTree(['/home']);
};
