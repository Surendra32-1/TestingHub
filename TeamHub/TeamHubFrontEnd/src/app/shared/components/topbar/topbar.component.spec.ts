import { HttpClient } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { Component } from '@angular/core';
import { of, throwError } from 'rxjs';

import { AuthService } from '../../../core/auth/auth.service';
import { TopbarComponent } from './topbar.component';

@Component({ selector: 'app-blank-page', standalone: true, template: '' })
class BlankPageComponent {}

describe('TopbarComponent', () => {
  let fixture: ComponentFixture<TopbarComponent>;
  let authService: AuthService;
  let router: Router;

  const host = (): HTMLElement => fixture.nativeElement as HTMLElement;
  const itemLabels = (): string[] =>
    Array.from(host().querySelectorAll(':scope > .topbar > .main-nav > app-nav-menu > .menu-level--root > *'))
      .map((element) => {
        const toggle = element.querySelector(':scope > .menu-toggle');
        return (toggle ?? element).textContent!.replace(/[▼›]/g, '').replace(/\s+/g, ' ').trim();
      });

  const signIn = (permissions: string[]) =>
    authService.storeSession({ accessToken: 'token', expiresIn: 900, roles: ['Admin'], permissions });

  beforeEach(async () => {
    sessionStorage.clear();

    await TestBed.configureTestingModule({
      imports: [TopbarComponent],
      providers: [
        provideRouter([{ path: 'login', component: BlankPageComponent }, { path: '**', component: BlankPageComponent }]),
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
    fixture = TestBed.createComponent(TopbarComponent);
    fixture.detectChanges();
  });

  afterEach(() => sessionStorage.clear());

  it('renders the signed-out entries and no management shortcuts', () => {
    expect(itemLabels()).toEqual(['Home', 'Login']);
    expect(host().querySelector('.profile-trigger')).toBeNull();
  });

  it('renders only the entries the signed-in permissions allow', () => {
    signIn(['people.view', 'master.department', 'user-roles.assign']);
    fixture.detectChanges();

    expect(itemLabels()).toEqual(['People', 'Manage']);
    expect(host().querySelector('.profile-trigger')).not.toBeNull();
  });

  it('shows only permitted matrix links in the gear dropdown', () => {
    signIn(['setting.gear', 'setting.gear.EmployeeRole', 'setting.gear.RolePermission']);
    fixture.detectChanges();

    host().querySelector<HTMLButtonElement>('.settings-trigger')!.click();
    fixture.detectChanges();

    const dropdown = host().querySelector('.settings-dropdown');
    expect(dropdown?.textContent).toContain('Employee Role');
    expect(dropdown?.textContent).toContain('Role Permission');
    expect(dropdown?.textContent).not.toContain('Matrix');
  });

  it('shows the gear only when the setting.gear permission is granted', () => {
    signIn(['setting.gear.EmployeeRole']);
    fixture.detectChanges();
    expect(host().querySelector('.settings-trigger')).toBeNull();

    signIn(['setting.gear']);
    fixture.detectChanges();
    expect(host().querySelector('.settings-trigger')).not.toBeNull();
  });

  it('navigates to the employee role matrix from the gear menu', async () => {
    signIn(['setting.gear', 'setting.gear.EmployeeRole']);
    fixture.detectChanges();

    host().querySelector<HTMLButtonElement>('.settings-trigger')!.click();
    fixture.detectChanges();
    host().querySelector<HTMLButtonElement>('.settings-dropdown button')!.click();
    await fixture.whenStable();

    expect(router.url).toBe('/settings/employee-role-matrix');
  });

  it('navigates to the role permission matrix from the gear menu', async () => {
    signIn(['setting.gear', 'setting.gear.RolePermission']);
    fixture.detectChanges();

    host().querySelector<HTMLButtonElement>('.settings-trigger')!.click();
    fixture.detectChanges();
    host().querySelector<HTMLButtonElement>('.settings-dropdown button')!.click();
    await fixture.whenStable();

    expect(router.url).toBe('/settings/role-permission-matrix');
  });

  it('opens the profile menu and closes it when clicking outside', () => {
    signIn(['master.department', 'profile.view', 'profile.edit']);
    fixture.detectChanges();

    host().querySelector<HTMLButtonElement>('.profile-trigger')!.click();
    fixture.detectChanges();
    expect(host().querySelector('.profile-dropdown')?.textContent).toContain('Update Profile');
    expect(host().querySelector('.profile-dropdown')?.textContent).not.toContain('View Profile');
    expect(host().querySelector('.profile-dropdown')?.textContent).toContain('Logout');

    document.body.click();
    fixture.detectChanges();
    expect(host().querySelector('.profile-dropdown')).toBeNull();
  });

  it('allows users with profile.edit to open the self-service editor', async () => {
    signIn(['profile.edit']);
    fixture.detectChanges();

    host().querySelector<HTMLButtonElement>('.profile-trigger')!.click();
    fixture.detectChanges();
    host().querySelector<HTMLButtonElement>('.profile-dropdown button')!.click();
    await fixture.whenStable();

    expect(router.url).toBe('/profile/edit');
  });

  it('removes every protected entry when the user signs out', async () => {
    signIn(['master.department']);
    fixture.detectChanges();
    expect(itemLabels()).toContain('Manage');

    host().querySelector<HTMLButtonElement>('.profile-trigger')!.click();
    fixture.detectChanges();
    host().querySelector<HTMLButtonElement>('.profile-dropdown button')!.click();
    await fixture.whenStable();
    await new Promise<void>((resolve) => setTimeout(resolve, 0));
    fixture.detectChanges();

    expect(itemLabels()).toEqual(['Home', 'Login']);
    expect(router.url).toBe('/login');
  });

  it('re-renders the navigation when another account takes over the session', () => {
    signIn(['people.view', 'setting.gear', 'setting.gear.EmployeeRole']);
    fixture.detectChanges();
    expect(itemLabels()).toEqual(['People']);
    expect(host().querySelector('.settings-trigger')).not.toBeNull();

    signIn(['people.view', 'terminations.view']);
    fixture.detectChanges();

    expect(itemLabels()).toEqual(['People', 'Terminations']);
    expect(host().querySelector('.settings-trigger')).toBeNull();
  });
});
