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
import {
  anonymousHomeGuard,
  authenticatedGuard,
  masterGuard,
  peopleViewGuard,
  rolePermissionMatrixGuard,
  signedInLandingGuard,
  terminationAddGuard
} from './auth.guard';

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
    signIn(['setting.gear.RolePermission']);

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
    signIn(['setting.gear.RolePermission']);
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

  it('allows a signed-out visitor to remain on the public Home landing page', async () => {
    const result = await resolveGuard(() => signedInLandingGuard(routeSnapshot(), stateSnapshot('/')));

    expect(result).toBe(true);
  });

  it('requires people.view to open the employee management page', () => {
    signIn(['people.view']);
    expect(guardResult(() => peopleViewGuard(routeSnapshot(), stateSnapshot('/people')))).toBe(true);

    signIn([]);
    const result = guardResult(() => peopleViewGuard(routeSnapshot(), stateSnapshot('/people')));
    expect(result instanceof UrlTree).toBe(true);
    expect((result as UrlTree).toString()).toBe('/home');
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

  it('sends a signed-in visitor to the Employee page when they may open it', () => {
    signIn(['allemployee.search.view', 'people.view']);

    const result = guardResult(() => signedInLandingGuard(routeSnapshot(), stateSnapshot('/')));

    expect(result instanceof UrlTree).toBe(true);
    expect((result as UrlTree).toString()).toBe('/employee');
  });

  it('falls back to People, then to Home, when the Employee page is not granted', () => {
    signIn(['people.view']);
    expect((guardResult(() => signedInLandingGuard(routeSnapshot(), stateSnapshot('/'))) as UrlTree).toString())
      .toBe('/people');

    signIn([]);
    expect((guardResult(() => signedInLandingGuard(routeSnapshot(), stateSnapshot('/'))) as UrlTree).toString())
      .toBe('/home');
  });

  it('keeps a signed-in account out of the public Home directory', () => {
    signIn(['allemployee.search.view']);

    const result = guardResult(() => anonymousHomeGuard(routeSnapshot(), stateSnapshot('/home')));

    expect(result instanceof UrlTree).toBe(true);
    expect((result as UrlTree).toString()).toBe('/employee');

    authService.clearSession();
    expect(guardResult(() => anonymousHomeGuard(routeSnapshot(), stateSnapshot('/home')))).toBe(true);
  });

  it('sends a denied People visit to the Employee page when that is the only page allowed', () => {
    signIn(['allemployee.search.view']);

    const result = guardResult(() => peopleViewGuard(routeSnapshot(), stateSnapshot('/people')));

    expect(result instanceof UrlTree).toBe(true);
    expect((result as UrlTree).toString()).toBe('/employee');
  });
});
