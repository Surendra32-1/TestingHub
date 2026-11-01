import { HttpClient } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { Component } from '@angular/core';
import { of, throwError } from 'rxjs';

import { AuthService } from '../../../core/auth/auth.service';
import { AdminNavbarComponent } from './admin-navbar.component';

@Component({ selector: 'app-blank-page', standalone: true, template: '' })
class BlankPageComponent {}

describe('AdminNavbarComponent (Navbar 2 - administration)', () => {
  let fixture: ComponentFixture<AdminNavbarComponent>;
  let authService: AuthService;
  let router: Router;

  const host = (): HTMLElement => fixture.nativeElement as HTMLElement;
  const topLevelLabels = (): string[] =>
    Array.from(
      host().querySelectorAll('.admin-nav > .admin-link, .admin-nav > .admin-branch > .admin-toggle')
    ).map((element) => element.textContent!.replace(/▾/g, '').replace(/\s+/g, ' ').trim());

  const signIn = (permissions: string[]) =>
    authService.storeSession({ accessToken: 'token', expiresIn: 900, roles: ['Admin'], permissions });

  beforeEach(async () => {
    sessionStorage.clear();

    await TestBed.configureTestingModule({
      imports: [AdminNavbarComponent],
      providers: [
        provideRouter([
          { path: 'login', component: BlankPageComponent },
          { path: 'master/:masterKey', component: BlankPageComponent },
          { path: 'settings/employee-role-matrix', component: BlankPageComponent },
          { path: 'settings/role-permission-matrix', component: BlankPageComponent },
          { path: 'profile/edit', component: BlankPageComponent },
          { path: '**', component: BlankPageComponent }
        ]),
        {
          provide: HttpClient,
          useValue: {
            get: () => throwError(() => new Error('not used')),
            post: () => of(undefined)
          }
        }
      ]
    }).compileComponents();

    authService = TestBed.inject(AuthService);
    router = TestBed.inject(Router);
    fixture = TestBed.createComponent(AdminNavbarComponent);
  });

  afterEach(() => sessionStorage.clear());

  it('renders admin navigation from permissions and never shows People', () => {
    signIn(['people.allemployee.search.view', 'administration.view', 'organization.department.view']);
    fixture.detectChanges();

    const labels = topLevelLabels();
    expect(labels).toContain('Administration');
    expect(labels).toContain('Organization Master');
    expect(labels).not.toContain('People');
  });

  it('shows the settings gear and its permitted links', () => {
    signIn(['administration.view', 'setting.gear.role.view', 'setting.gear.manage.permissions.view']);
    fixture.detectChanges();

    host().querySelector<HTMLButtonElement>('.gear-trigger')!.click();
    fixture.detectChanges();

    const links = Array.from(host().querySelectorAll<HTMLButtonElement>('.admin-menu button'))
      .map((button) => button.textContent?.trim());
    expect(links).toEqual(['Role', 'Manage Permissions']);
  });

  it('hides the gear when no settings entry is authorized', () => {
    signIn(['administration.view']);
    fixture.detectChanges();

    expect(host().querySelector('.gear-trigger')).toBeNull();
  });

  it('navigates to the employee role matrix from the gear menu', async () => {
    signIn(['administration.view', 'setting.gear.EmployeeRole']);
    fixture.detectChanges();

    host().querySelector<HTMLButtonElement>('.gear-trigger')!.click();
    fixture.detectChanges();
    host().querySelector<HTMLButtonElement>('.admin-menu button')!.click();
    await fixture.whenStable();

    expect(router.url).toBe('/settings/employee-role-matrix');
  });

  it('opens the profile menu and logs out', async () => {
    signIn(['administration.view', 'profile.edit']);
    fixture.detectChanges();

    host().querySelector<HTMLButtonElement>('.profile-trigger')!.click();
    fixture.detectChanges();
    const menu = host().querySelector('.admin-menu');
    expect(menu?.textContent).toContain('Update Profile');
    expect(menu?.textContent).toContain('Logout');

    const buttons = Array.from(host().querySelectorAll<HTMLButtonElement>('.admin-menu button'));
    buttons[buttons.length - 1].click();
    await fixture.whenStable();
    await new Promise<void>((resolve) => setTimeout(resolve, 0));

    expect(router.url).toBe('/login');
  });
});
