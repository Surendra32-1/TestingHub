import { HttpClient } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';

import { AuthResponse } from '../auth/auth.models';
import { AuthService } from '../auth/auth.service';
import { MenuItem } from './menu.config';
import { MenuService } from './menu.service';

const FULL_CATALOG = [
  'people.view', 'people.create', 'people.update', 'people.release', 'people.terminate', 'people.hierarchy.view',
  'resignations.view', 'resignations.manage', 'resignations.add',
  'resignations.action.submit', 'resignations.action.sendforapproval', 'resignations.action.approve',
  'resignations.action.reject', 'resignations.action.cancel', 'resignations.action.exitchecklist',
  'resignations.action.release',
  'terminations.view', 'terminations.add', 'terminations.manage',
  'master.view', 'master.manage', 'permissions.manage', 'user-roles.assign', 'role-permissions.manage',
  'master.department', 'master.designation', 'master.business-unit', 'master.dress-size',
  'master.cotivity-department', 'master.cotivity-designation', 'master.cotivity-employee', 'master.role',
  'master.address-type', 'master.blood-group', 'master.skills', 'master.job-type', 'master.team', 'master.gender'
];

const labels = (items: readonly MenuItem[]): string[] => items.map((item) => item.label);

describe('MenuService', () => {
  let authService: AuthService;
  let menuService: MenuService;
  let refreshResponses: AuthResponse[] = [];

  const signIn = (permissions: string[], accessToken = 'access-token') =>
    authService.storeSession({ accessToken, expiresIn: 900, roles: ['Admin'], permissions });

  beforeEach(() => {
    sessionStorage.clear();
    refreshResponses = [];

    TestBed.configureTestingModule({
      providers: [
        {
          provide: HttpClient,
          useValue: {
            get: () => throwError(() => new Error('not used')),
            post: (url: string) =>
              url.includes('refresh-token')
                ? of(refreshResponses.shift() ?? { accessToken: 'refreshed', expiresIn: 900, roles: [], permissions: [] })
                : of(undefined as unknown)
          }
        }
      ]
    });

    authService = TestBed.inject(AuthService);
    menuService = TestBed.inject(MenuService);
  });

  afterEach(() => sessionStorage.clear());

  it('shows only the signed-out entries before anyone signs in', () => {
    expect(labels(menuService.visibleMenu())).toEqual(['Home', 'Login']);
  });

  it('derives the tree from the effective permissions, not from role names', () => {
    signIn(FULL_CATALOG);

    const visible = menuService.visibleMenu();
    expect(labels(visible)).toEqual(['People', 'Resignations', 'Terminations', 'Manage', 'Settings']);

    const manage = visible.find((item) => item.id === 'manage')!;
    expect(labels(manage.children!)).toEqual([
      'Department', 'Designation', 'Business Unit', 'Dress Size', 'Cotivity Department',
      'Cotivity Designation', 'Cotivity Employee', 'Role', 'Manage Permissions', 'Address Type',
      'Blood Group', 'Skills', 'Job Type', 'Team', 'Gender'
    ]);

    const settings = visible.find((item) => item.id === 'settings')!;
    expect(labels(settings.children!)).toEqual(['Employee Role Matrix', 'Role Permission Matrix']);
  });

  it('hides the self-service entry from users who can already manage resignations', () => {
    signIn(['resignations.add', 'resignations.view']);

    expect(labels(menuService.visibleMenu())).toEqual(['People', 'Resignations']);
  });

  it('shows only the self-service entry to an employee who can file but not view', () => {
    signIn(['resignations.add']);

    expect(labels(menuService.visibleMenu())).toEqual(['People', 'Apply Resignation']);
  });

  it('leaves an account without granted permissions with just the directory entry', () => {
    signIn([]);

    expect(labels(menuService.visibleMenu())).toEqual(['People']);
  });

  it('merges the permissions coming from several roles into one menu', () => {
    signIn(['master.department', 'role-permissions.manage']);

    const visible = menuService.visibleMenu();
    expect(labels(visible)).toEqual(['People', 'Manage', 'Settings']);
    expect(labels(visible[1].children!)).toEqual(['Department']);
    expect(labels(visible[2].children!)).toEqual(['Role Permission Matrix']);
  });

  it('matches permission codes case-insensitively', () => {
    signIn(['Master.Department']);

    expect(labels(menuService.visibleMenu()[1].children!)).toEqual(['Department']);
  });

  it('drops stale items on logout', () => {
    signIn(['master.department']);
    expect(labels(menuService.visibleMenu())).toEqual(['People', 'Manage']);

    authService.logout().subscribe();

    expect(labels(menuService.visibleMenu())).toEqual(['Home', 'Login']);
  });

  it('rebuilds the menu when the token refresh returns new permissions', () => {
    signIn(['master.department'], 'expired-token');
    refreshResponses = [{ accessToken: 'new-token', expiresIn: 900, roles: ['HR'], permissions: ['terminations.view'] }];

    authService.refreshAccessToken().subscribe();

    expect(labels(menuService.visibleMenu())).toEqual(['People', 'Terminations']);
  });

  it('switches menus cleanly when another account signs in', () => {
    signIn(['user-roles.assign']);
    expect(labels(menuService.visibleMenu())).toEqual(['People', 'Settings']);

    signIn(['master.gender']);
    const visible = menuService.visibleMenu();
    expect(labels(visible)).toEqual(['People', 'Manage']);
    expect(labels(visible[1].children!)).toEqual(['Gender']);
  });

  it('never mutates the shared configuration while filtering', () => {
    const before = JSON.stringify(menuService.fullMenu);

    signIn(FULL_CATALOG);
    menuService.visibleMenu();
    authService.clearSession();
    menuService.visibleMenu();

    expect(JSON.stringify(menuService.fullMenu)).toBe(before);
  });
});
