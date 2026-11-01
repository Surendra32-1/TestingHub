import { HttpClient } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { Component } from '@angular/core';
import { of, throwError } from 'rxjs';

import { AuthService } from '../../../core/auth/auth.service';
import { AdminNavbarComponent } from './admin-navbar.component';

@Component({ selector: 'app-blank-page', standalone: true, template: '' })
class BlankPageComponent {}

describe('AdminNavbarComponent (Navbar 2 top bar)', () => {
  let fixture: ComponentFixture<AdminNavbarComponent>;
  let authService: AuthService;
  let router: Router;

  const host = (): HTMLElement => fixture.nativeElement as HTMLElement;

  const signIn = (permissions: string[]) =>
    authService.storeSession({ accessToken: 'token', expiresIn: 900, roles: ['Admin'], permissions });

  beforeEach(async () => {
    sessionStorage.clear();

    await TestBed.configureTestingModule({
      imports: [AdminNavbarComponent],
      providers: [
        provideRouter([
          { path: 'administration', component: BlankPageComponent },
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

  it('does not render the horizontal nav (navigation lives in the sidebar)', () => {
    signIn(['administration.view', 'organization.department.view']);
    fixture.detectChanges();

    expect(host().querySelector('.admin-nav')).toBeNull();
    expect(host().querySelector('.admin-title')).not.toBeNull();
  });

  it('shows the TeamHub brand only when the sidebar is not shown', () => {
    signIn(['administration.view']);

    fixture.componentRef.setInput('sidebarCollapsed', false);
    fixture.detectChanges();
    expect(host().querySelector('.admin-brand')).toBeNull();

    fixture.componentRef.setInput('sidebarCollapsed', true);
    fixture.detectChanges();
    expect(host().querySelector('.admin-brand')).not.toBeNull();
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
