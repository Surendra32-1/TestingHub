import { HttpClient } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { throwError } from 'rxjs';

import { AuthService } from '../auth/auth.service';
import { MenuService } from './menu.service';
import { NavbarSelectionService } from './navbar-selection.service';

describe('NavbarSelectionService', () => {
  let authService: AuthService;
  let selection: NavbarSelectionService;

  const signIn = (permissions: string[]): void =>
    authService.storeSession({ accessToken: 'token', expiresIn: 900, roles: ['Admin'], permissions });

  beforeEach(() => {
    sessionStorage.clear();

    TestBed.configureTestingModule({
      providers: [
        MenuService,
        NavbarSelectionService,
        {
          provide: HttpClient,
          useValue: {
            get: () => throwError(() => new Error('not used')),
            post: () => throwError(() => new Error('not used'))
          }
        }
      ]
    });

    authService = TestBed.inject(AuthService);
    selection = TestBed.inject(NavbarSelectionService);
  });

  afterEach(() => sessionStorage.clear());

  it('uses the admin sidebar layout when only People and Profile are granted', () => {
    signIn(['people.allemployee.search.view', 'profile.view', 'profile.edit']);
    expect(selection.useAdminNavbar()).toBe(true);
  });

  it('uses the admin navbar when Administration is granted', () => {
    signIn(['administration.view']);
    expect(selection.useAdminNavbar()).toBe(true);
  });

  it('uses the admin navbar when a user has both People and admin permissions', () => {
    signIn(['people.allemployee.search.view', 'administration.view']);
    expect(selection.useAdminNavbar()).toBe(true);
  });

  it('keeps the employee navbar for self-service resignations.add only', () => {
    signIn(['profile.view', 'resignations.add']);
    expect(selection.useAdminNavbar()).toBe(false);
  });

  it('uses the admin navbar when only the settings gear is authorized', () => {
    signIn(['setting.gear.EmployeeRole']);
    expect(selection.gearVisible()).toBe(true);
    expect(selection.useAdminNavbar()).toBe(true);
  });

  it('uses the admin navbar when Organization Master or Cotivity permissions are present', () => {
    signIn(['organization.department.view']);
    expect(selection.useAdminNavbar()).toBe(true);

    signIn(['cotivity.employee.view']);
    expect(selection.useAdminNavbar()).toBe(true);
  });

  it('uses the employee navbar when signed out', () => {
    expect(selection.useAdminNavbar()).toBe(false);
  });
});
