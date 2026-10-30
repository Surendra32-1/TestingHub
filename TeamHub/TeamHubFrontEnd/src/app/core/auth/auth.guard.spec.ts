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
  employeeTerminationAddGuard,
  masterGuard,
  administrationViewGuard,
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
    const result = await resolveGuard(() => authenticatedGuard(routeSnapshot(), stateSnapshot('/administration')));

    expect(result instanceof UrlTree).toBe(true);
    expect((result as UrlTree).toString()).toContain('/login');
    expect((result as UrlTree).toString()).toContain('returnUrl');
  });

  it('allows a signed-out visitor to remain on the public Home landing page', async () => {
    const result = await resolveGuard(() => signedInLandingGuard(routeSnapshot(), stateSnapshot('/')));

    expect(result).toBe(true);
  });

  it('requires administration.view to open the employee management page', () => {
    signIn(['administration.view']);
    expect(guardResult(() => administrationViewGuard(routeSnapshot(), stateSnapshot('/administration')))).toBe(true);

    signIn([]);
    const result = guardResult(() => administrationViewGuard(routeSnapshot(), stateSnapshot('/administration')));
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
    signIn(['organization.department.view']);

    expect(guardResult(() => masterGuard(routeSnapshot({ masterKey: 'department' }), stateSnapshot('/master/department')))).toBe(true);

    const blocked = guardResult(() => masterGuard(routeSnapshot({ masterKey: 'gender' }), stateSnapshot('/master/gender')));
    expect(blocked instanceof UrlTree).toBe(true);

    signIn(['organization.gender.create']);
    const createOnly = guardResult(() => masterGuard(routeSnapshot({ masterKey: 'gender' }), stateSnapshot('/master/gender')));
    expect(createOnly).toBe(true);

    signIn(['organization.bloodgroup.view']);
    expect(guardResult(() => masterGuard(
      routeSnapshot({ masterKey: 'blood-group' }),
      stateSnapshot('/master/blood-group')
    ))).toBe(true);
  });

  it('uses the dedicated gear View grants for Role and Manage Permissions pages', () => {
    signIn(['setting.gear.role.view']);
    expect(guardResult(() => masterGuard(
      routeSnapshot({ masterKey: 'role' }),
      stateSnapshot('/master/role')
    ))).toBe(true);
    expect(guardResult(() => masterGuard(
      routeSnapshot({ masterKey: 'permission' }),
      stateSnapshot('/master/permission')
    )) instanceof UrlTree).toBe(true);

    signIn(['setting.gear.manage.permissions.view']);
    expect(guardResult(() => masterGuard(
      routeSnapshot({ masterKey: 'permission' }),
      stateSnapshot('/master/permission')
    ))).toBe(true);
  });

  it('allows Cotivity master routes for view or CRUD grants', () => {
    signIn(['cotivity.department.view']);
    expect(guardResult(() => masterGuard(
      routeSnapshot({ masterKey: 'cotivity-department' }),
      stateSnapshot('/master/cotivity-department')
    ))).toBe(true);
    expect(guardResult(() => masterGuard(
      routeSnapshot({ masterKey: 'cotivity-employee' }),
      stateSnapshot('/master/cotivity-employee')
    )) instanceof UrlTree).toBe(true);

    signIn(['cotivity.employee.create']);
    expect(guardResult(() => masterGuard(
      routeSnapshot({ masterKey: 'cotivity-employee' }),
      stateSnapshot('/master/cotivity-employee')
    ))).toBe(true);
  });

  it('allows employee termination with administration.terminate or terminations.add, but not manage alone', () => {
    signIn(['terminations.manage']);

    const blocked = guardResult(() =>
      employeeTerminationAddGuard(routeSnapshot({ employeeId: '6' }), stateSnapshot('/employees/terminate/6'))
    );
    expect(blocked instanceof UrlTree).toBe(true);

    signIn(['administration.terminate']);

    expect(guardResult(() =>
      employeeTerminationAddGuard(routeSnapshot({ employeeId: '6' }), stateSnapshot('/employees/terminate/6'))
    )).toBe(true);

    signIn(['terminations.manage', 'terminations.add']);

    expect(guardResult(() =>
      employeeTerminationAddGuard(routeSnapshot({ employeeId: '6' }), stateSnapshot('/employees/terminate/6'))
    )).toBe(true);

    signIn(['terminations.add']);
    expect(guardResult(() =>
      terminationAddGuard(routeSnapshot(), stateSnapshot('/employees/terminations/new'))
    )).toBe(true);
  });

  it('prioritizes Administration for signed-in visitors who may open it', () => {
    signIn(['people.allemployee.search.view', 'administration.view']);

    const result = guardResult(() => signedInLandingGuard(routeSnapshot(), stateSnapshot('/')));

    expect(result instanceof UrlTree).toBe(true);
    expect((result as UrlTree).toString()).toBe('/administration');
  });

  it('uses the assigned landing permission and falls back to Home without either', () => {
    signIn(['people.allemployee.search.view']);
    expect((guardResult(() => signedInLandingGuard(routeSnapshot(), stateSnapshot('/'))) as UrlTree).toString())
      .toBe('/people');

    signIn(['administration.view']);
    expect((guardResult(() => signedInLandingGuard(routeSnapshot(), stateSnapshot('/'))) as UrlTree).toString())
      .toBe('/administration');

    signIn([]);
    expect((guardResult(() => signedInLandingGuard(routeSnapshot(), stateSnapshot('/'))) as UrlTree).toString())
      .toBe('/home');
  });

  it('keeps a signed-in account out of the public Home directory', () => {
    signIn(['people.allemployee.search.view']);

    const result = guardResult(() => anonymousHomeGuard(routeSnapshot(), stateSnapshot('/home')));

    expect(result instanceof UrlTree).toBe(true);
    expect((result as UrlTree).toString()).toBe('/people');

    authService.clearSession();
    expect(guardResult(() => anonymousHomeGuard(routeSnapshot(), stateSnapshot('/home')))).toBe(true);
  });

  it('sends a denied Administration visit to People when that is the only page allowed', () => {
    signIn(['people.allemployee.search.view']);

    const result = guardResult(() => administrationViewGuard(routeSnapshot(), stateSnapshot('/administration')));

    expect(result instanceof UrlTree).toBe(true);
    expect((result as UrlTree).toString()).toBe('/people');
  });
});
