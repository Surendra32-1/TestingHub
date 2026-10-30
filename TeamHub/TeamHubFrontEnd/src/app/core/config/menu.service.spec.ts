import { HttpClient } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';

import { AuthResponse } from '../auth/auth.models';
import { AuthService } from '../auth/auth.service';
import { MenuItem } from './menu.config';
import { MenuService } from './menu.service';

const MASTER_KEYS = [
  'department', 'designation', 'business-unit', 'dress-size', 'cotivity-department',
  'cotivity-designation', 'cotivity-employee', 'role', 'address-type', 'blood-group',
  'skills', 'job-type', 'team', 'gender', 'recruitment'
];

const FULL_CATALOG = [
  'administration.view', 'administration.create', 'administration.update', 'administration.release', 'administration.terminate', 'administration.hierarchy.view',
  'administration.delete', 'administration.cardview',
  'people.allemployee.search.view',
  'resignations.button', 'resignations.view', 'resignations.manage', 'resignations.add',
  'resignations.action.submit', 'resignations.action.sendforapproval', 'resignations.action.approve',
  'resignations.action.reject', 'resignations.action.cancel', 'resignations.action.exitchecklist',
  'resignations.action.release',
  'terminations.view', 'terminations.add', 'terminations.manage',
  'master.view', 'master.manage', 'permissions.manage', 'user-roles.assign', 'role-permissions.manage',
  'setting.gear', 'setting.gear.EmployeeRole', 'setting.gear.RolePermission',
  ...MASTER_KEYS
    .filter((key) => !key.startsWith('cotivity-'))
    .flatMap((key) => ['view', 'create', 'update', 'delete'].map((action) => `master.${key}.${action}`)),
  ...(['department', 'designation', 'businessunit', 'dresssize', 'addresstype', 'bloodgroup', 'skills', 'jobtype', 'team', 'gender'] as const)
    .flatMap((key) => ['view', 'create', 'edit', 'delete'].map((action) => `organization.${key}.${action}`)),
  ...(['department', 'designation', 'employee'] as const)
    .flatMap((key) => ['view', 'create', 'edit', 'delete'].map((action) => `cotivity.${key}.${action}`))
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
    expect(labels(visible)).toEqual([
      'People', 'Administration', 'Resignations', 'Terminations', 'Organization Master', 'Cotivity'
    ]);

    const organizationMaster = visible.find((item) => item.id === 'organization-master')!;
    const cotivity = visible.find((item) => item.id === 'cotivity')!;
    expect(labels(organizationMaster.children!)).toEqual([
      'Department', 'Designation', 'Business Unit', 'Dress Size', 'Address Type',
      'Blood Group', 'Skills', 'Job Type', 'Team', 'Gender'
    ]);
    expect(labels(cotivity.children!)).toEqual(['Cotivity Department', 'Cotivity Designation', 'Cotivity Employee']);

  });

  it('does not show a hardcoded self-service resignation navigation entry', () => {
    signIn(['administration.view', 'resignations.add', 'resignations.button', 'resignations.view']);

    expect(labels(menuService.visibleMenu())).toEqual(['Administration', 'Resignations']);
  });

  it('shows Resignations only when the navigation permission is granted', () => {
    signIn(['administration.view', 'resignations.view']);
    expect(labels(menuService.visibleMenu())).toEqual(['Administration']);

    signIn(['administration.view', 'resignations.button']);
    expect(labels(menuService.visibleMenu())).toEqual(['Administration', 'Resignations']);
  });

  it('does not show an Apply Resignation entry to employees with only the add permission', () => {
    signIn(['administration.view', 'resignations.add']);

    expect(labels(menuService.visibleMenu())).toEqual(['Administration']);
  });

  it('shows the Administration entry only to accounts with administration.view', () => {
    signIn([]);

    expect(labels(menuService.visibleMenu())).toEqual([]);

    signIn(['administration.view']);
    expect(labels(menuService.visibleMenu())).toEqual(['Administration']);
  });

  it('shows the People page only to accounts with people.allemployee.search.view', () => {
    signIn(['administration.view']);
    expect(labels(menuService.visibleMenu())).toEqual(['Administration']);

    signIn(['people.allemployee.search.view']);
    expect(labels(menuService.visibleMenu())).toEqual(['People']);
  });

  it('places People before Administration when both are available', () => {
    signIn(['administration.view', 'people.allemployee.search.view']);

    const visible = menuService.visibleMenu();
    expect(labels(visible)).toEqual(['People', 'Administration']);
    expect(visible.map(({ route }) => route)).toEqual(['/people', '/administration']);
  });

  it('merges the permissions coming from several roles into one menu', () => {
    signIn(['administration.view', 'organization.department.view', 'setting.gear.RolePermission']);

    const visible = menuService.visibleMenu();
    expect(labels(visible)).toEqual(['Administration', 'Organization Master']);
    expect(labels(visible[1].children!)).toEqual(['Department']);
  });

  it('uses the View grant for master dropdown visibility', () => {
    signIn(['organization.department.create', 'organization.department.edit', 'organization.department.delete']);
    expect(labels(menuService.visibleMenu())).toEqual([]);

    signIn(['organization.department.view']);
    const visible = menuService.visibleMenu();
    expect(labels(visible)).toEqual(['Organization Master']);
    expect(labels(visible[0].children!)).toEqual(['Department']);
  });

  it('uses the Organization Blood Group View grant', () => {
    signIn(['organization.bloodgroup.view']);

    const organization = menuService.visibleMenu().find((item) => item.id === 'organization-master');
    expect(labels(organization?.children ?? [])).toEqual(['Blood Group']);
  });

  it('uses Cotivity View grants to show Cotivity master links', () => {
    signIn(['cotivity.department.create', 'cotivity.employee.edit']);
    expect(labels(menuService.visibleMenu())).toEqual([]);

    signIn(['cotivity.department.view', 'cotivity.employee.view']);
    const cotivity = menuService.visibleMenu().find((item) => item.id === 'cotivity');
    expect(labels(cotivity?.children ?? [])).toEqual(['Cotivity Department', 'Cotivity Employee']);
  });

  it('matches permission codes case-insensitively', () => {
    signIn(['administration.view', 'Organization.Department.View']);

    expect(labels(menuService.visibleMenu()[1].children!)).toEqual(['Department']);
  });

  it('drops stale items on logout', () => {
    signIn(['administration.view', 'organization.department.view']);
    expect(labels(menuService.visibleMenu())).toEqual(['Administration', 'Organization Master']);

    authService.logout().subscribe();

    expect(labels(menuService.visibleMenu())).toEqual(['Home', 'Login']);
  });

  it('rebuilds the menu when the token refresh returns new permissions', () => {
    signIn(['organization.department.view'], 'expired-token');
    refreshResponses = [{
      accessToken: 'new-token',
      expiresIn: 900,
      roles: ['HR'],
      permissions: ['administration.view', 'terminations.view']
    }];

    authService.refreshAccessToken().subscribe();

    expect(labels(menuService.visibleMenu())).toEqual(['Administration', 'Terminations']);
  });

  it('switches menus cleanly when another account signs in', () => {
    signIn(['administration.view', 'setting.gear.EmployeeRole']);
    expect(labels(menuService.visibleMenu())).toEqual(['Administration']);

    signIn(['administration.view', 'organization.gender.view']);
    const visible = menuService.visibleMenu();
    expect(labels(visible)).toEqual(['Administration', 'Organization Master']);
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
