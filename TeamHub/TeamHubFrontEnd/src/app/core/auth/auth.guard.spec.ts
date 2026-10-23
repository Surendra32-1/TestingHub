import { HttpClient } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import {
  ActivatedRouteSnapshot,
  CanActivateFn,
  Router,
  RouterStateSnapshot,
  UrlTree,
  convertToParamMap,
  provideRouter
} from '@angular/router';
import { Observable, isObservable, of, throwError } from 'rxjs';

import { AuthResponse } from './auth.models';
import { AuthService } from './auth.service';
import { authenticatedGuard, masterGuard, rolePermissionMatrixGuard, signedInLandingGuard, terminationAddGuard } from './auth.guard';

const routeSnapshot = (params: Record<string, string> = {}): ActivatedRouteSnapshot =>
  ({ paramMap: convertToParamMap(params) }) as unknown as ActivatedRouteSnapshot;

const stateSnapshot = (url: string): RouterStateSnapshot => ({ url }) as RouterStateSnapshot;

const guardResult = <T>(fn: () => T): T => TestBed.runInInjectionContext(fn);

type GuardOutcome = Awaited<ReturnType<CanActivateFn>>;

const resolveGuard = (fn: () => ReturnType<CanActivateFn>): Promise<GuardOutcome> =>
  new Promise((resolve) => {
    const result = guardResult(fn);
    if (isObservable(result)) {
      result.subscribe(resolve);
    } else {
      resolve(result);
    }
  });

describe('route guards', () => {
  let authService: AuthService;
  let router: Router;
  let refresh: Observable<AuthResponse> = throwError(() => new Error('no session'));

  beforeEach(() => {
    sessionStorage.clear();
    refresh = throwError(() => new Error('no session'));

    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        {
          provide: HttpClient,
          useValue: {
            get: () => throwError(() => new Error('not used')),
            post: () => refresh
          }
        }
      ]
    });

    authService = TestBed.inject(AuthService);
    router = TestBed.inject(Router);
  });

  afterEach(() => sessionStorage.clear());

  const signIn = (permissions: string[]) =>
    authService.storeSession({ accessToken: 'token', expiresIn: 900, roles: ['HR'], permissions });

  it('lets a user through when the permission code is granted', () => {
    signIn(['role-permissions.manage']);

    expect(guardResult(() => rolePermissionMatrixGuard(routeSnapshot(), stateSnapshot('/settings/role-permission-matrix')))).toBe(true);
  });

  it('redirects unauthorized direct navigation instead of opening the page', () => {
    signIn([]);

    const result = guardResult(() =>
      rolePermissionMatrixGuard(routeSnapshot(), stateSnapshot('/settings/role-permission-matrix'))
    );

    expect(result instanceof UrlTree).toBe(true);
    expect((result as UrlTree).toString()).toBe(router.createUrlTree(['/home']).toString());
  });

  it('re-checks the session on every navigation, so a logout cannot leave a window open', () => {
    signIn(['role-permissions.manage']);
    expect(guardResult(() => rolePermissionMatrixGuard(routeSnapshot(), stateSnapshot('/settings/role-permission-matrix')))).toBe(true);

    authService.clearSession();

    const result = guardResult(() =>
      rolePermissionMatrixGuard(routeSnapshot(), stateSnapshot('/settings/role-permission-matrix'))
    );
    expect(result instanceof UrlTree).toBe(true);
  });

  it('requires a session and sends anonymous visitors to the sign-in page', async () => {
    const result = await resolveGuard(() => authenticatedGuard(routeSnapshot(), stateSnapshot('/people')));

    expect(result instanceof UrlTree).toBe(true);
    expect((result as UrlTree).toString()).toContain('/login');
    expect((result as UrlTree).toString()).toContain('returnUrl');
  });

  it('restores an expired session before activating a guarded route', async () => {
    refresh = of({ accessToken: 'fresh-token', expiresIn: 900, roles: ['HR'], permissions: ['terminations.view'] });

    const result = await resolveGuard(() =>
      authenticatedGuard(routeSnapshot(), stateSnapshot('/employees/terminations'))
    );

    expect(result).toBe(true);
    expect(authService.hasPermission('terminations.view')).toBe(true);
  });

  it('keeps master routes behind their own module code', () => {
    signIn(['master.department']);

    expect(guardResult(() => masterGuard(routeSnapshot({ masterKey: 'department' }), stateSnapshot('/master/department')))).toBe(true);

    const blocked = guardResult(() => masterGuard(routeSnapshot({ masterKey: 'gender' }), stateSnapshot('/master/gender')));
    expect(blocked instanceof UrlTree).toBe(true);
  });

  it('keeps the termination create routes on terminations.add, not terminations.manage', () => {
    signIn(['terminations.manage']);

    const blocked = guardResult(() =>
      terminationAddGuard(routeSnapshot({ employeeId: '6' }), stateSnapshot('/employees/terminate/6'))
    );
    expect(blocked instanceof UrlTree).toBe(true);

    signIn(['terminations.manage', 'terminations.add']);

    expect(guardResult(() =>
      terminationAddGuard(routeSnapshot({ employeeId: '6' }), stateSnapshot('/employees/terminate/6'))
    )).toBe(true);
  });

  it('sends a signed-in visitor from the landing page to the directory', () => {
    signIn([]);

    const result = guardResult(() => signedInLandingGuard(routeSnapshot(), stateSnapshot('/')));

    expect(result instanceof UrlTree).toBe(true);
    expect((result as UrlTree).toString()).toBe('/people');
  });
});
