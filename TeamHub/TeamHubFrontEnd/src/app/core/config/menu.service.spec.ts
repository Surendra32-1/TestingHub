import { HttpClient } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';

import { AuthResponse } from '../auth/auth.models';
import { AuthService } from '../auth/auth.service';
import { MenuItem } from './menu.config';
import { MenuService } from './menu.service';

const FULL_CATALOG = [
  'people.view', 'people.create', 'people.update', 'people.release', 'people.terminate', 'people.hierarchy.view',
  'allemployee.search.view',
  'resignations.button', 'resignations.view', 'resignations.manage', 'resignations.add',
  'resignations.action.submit', 'resignations.action.sendforapproval', 'resignations.action.approve',
  'resignations.action.reject', 'resignations.action.cancel', 'resignations.action.exitchecklist',
  'resignations.action.release',
  'terminations.view', 'terminations.add', 'terminations.manage',
  'master.view', 'master.manage', 'permissions.manage', 'user-roles.assign', 'role-permissions.manage',
  'setting.gear', 'setting.gear.EmployeeRole', 'setting.gear.RolePermission',
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
    expect(labels(visible)).toEqual(['People', 'Employee', 'Resignations', 'Terminations', 'Manage']);

    const manage = visible.find((item) => item.id === 'manage')!;
    expect(labels(manage.children!)).toEqual([
      'Department', 'Designation', 'Business Unit', 'Dress Size', 'Cotivity Department',
      'Cotivity Designation', 'Cotivity Employee', 'Role', 'Manage Permissions', 'Address Type',
      'Blood Group', 'Skills', 'Job Type', 'Team', 'Gender'
    ]);

  });

  it('does not show a hardcoded self-service resignation navigation entry', () => {
    signIn(['people.view', 'resignations.add', 'resignations.button', 'resignations.view']);

    expect(labels(menuService.visibleMenu())).toEqual(['People', 'Resignations']);
  });

  it('shows Resignations only when the navigation permission is granted', () => {
    signIn(['people.view', 'resignations.view']);
    expect(labels(menuService.visibleMenu())).toEqual(['People']);

    signIn(['people.view', 'resignations.button']);
    expect(labels(menuService.visibleMenu())).toEqual(['People', 'Resignations']);
  });

  it('does not show an Apply Resignation entry to employees with only the add permission', () => {
    signIn(['people.view', 'resignations.add']);

    expect(labels(menuService.visibleMenu())).toEqual(['People']);
  });

  it('shows the People entry only to accounts with people.view', () => {
    signIn([]);

    expect(labels(menuService.visibleMenu())).toEqual([]);

    signIn(['people.view']);
    expect(labels(menuService.visibleMenu())).toEqual(['People']);
  });

  it('shows the Employee page only to accounts with allemployee.search.view', () => {
    signIn(['people.view']);
    expect(labels(menuService.visibleMenu())).toEqual(['People']);

    signIn(['allemployee.search.view']);
    expect(labels(menuService.visibleMenu())).toEqual(['Employee']);
  });

  it('merges the permissions coming from several roles into one menu', () => {
    signIn(['people.view', 'master.department', 'setting.gear.RolePermission']);

    const visible = menuService.visibleMenu();
    expect(labels(visible)).toEqual(['People', 'Manage']);
    expect(labels(visible[1].children!)).toEqual(['Department']);
  });

  it('matches permission codes case-insensitively', () => {
    signIn(['people.view', 'Master.Department']);

    expect(labels(menuService.visibleMenu()[1].children!)).toEqual(['Department']);
  });

  it('drops stale items on logout', () => {
    signIn(['people.view', 'master.department']);
    expect(labels(menuService.visibleMenu())).toEqual(['People', 'Manage']);

    authService.logout().subscribe();

    expect(labels(menuService.visibleMenu())).toEqual(['Home', 'Login']);
  });

  it('rebuilds the menu when the token refresh returns new permissions', () => {
    signIn(['master.department'], 'expired-token');
    refreshResponses = [{
      accessToken: 'new-token',
      expiresIn: 900,
      roles: ['HR'],
      permissions: ['people.view', 'terminations.view']
    }];

    authService.refreshAccessToken().subscribe();

    expect(labels(menuService.visibleMenu())).toEqual(['People', 'Terminations']);
  });

  it('switches menus cleanly when another account signs in', () => {
    signIn(['people.view', 'setting.gear.EmployeeRole']);
    expect(labels(menuService.visibleMenu())).toEqual(['People']);

    signIn(['people.view', 'master.gender']);
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
