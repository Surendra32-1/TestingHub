import { HttpClient } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { Component } from '@angular/core';
import { of, throwError } from 'rxjs';

import { AuthService } from '../../../core/auth/auth.service';
import { AdminSidebarComponent } from './admin-sidebar.component';

@Component({ selector: 'app-blank-page', standalone: true, template: '' })
class BlankPageComponent {}

describe('AdminSidebarComponent (Navbar 2 left nav)', () => {
  let fixture: ComponentFixture<AdminSidebarComponent>;
  let authService: AuthService;
  let router: Router;

  const host = (): HTMLElement => fixture.nativeElement as HTMLElement;
  const topLevelLabels = (): string[] =>
    Array.from(host().querySelectorAll('.sidebar-link > .sidebar-label, .sidebar-branch > .sidebar-toggle > .sidebar-label'))
      .map((element) => element.textContent!.trim());

  const signIn = (permissions: string[]) =>
    authService.storeSession({ accessToken: 'token', expiresIn: 900, roles: ['Admin'], permissions });

  beforeEach(async () => {
    sessionStorage.clear();

    await TestBed.configureTestingModule({
      imports: [AdminSidebarComponent],
      providers: [
        provideRouter([{ path: '**', component: BlankPageComponent }]),
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
    fixture = TestBed.createComponent(AdminSidebarComponent);
  });

  afterEach(() => sessionStorage.clear());

  it('renders admin navigation from permissions and includes People', () => {
    signIn(['people.allemployee.search.view', 'administration.view', 'organization.department.view']);
    fixture.detectChanges();

    const labels = topLevelLabels();
    expect(labels).toContain('Administration');
    expect(labels).toContain('Organization Master');
    expect(labels).toContain('People');
  });

  it('expands a branch on toggle and keeps a single branch open', () => {
    signIn(['organization.department.view', 'cotivity.employee.view']);
    fixture.detectChanges();

    const toggles = Array.from(host().querySelectorAll<HTMLButtonElement>('.sidebar-toggle'));
    const orgMaster = toggles.find((t) => t.textContent?.includes('Organization Master'))!;
    const cotivity = toggles.find((t) => t.textContent?.includes('Cotivity'))!;

    orgMaster.click();
    fixture.detectChanges();
    expect(host().querySelector('.sidebar-submenu a')?.textContent).toContain('Department');

    cotivity.click();
    fixture.detectChanges();
    const submenuText = Array.from(host().querySelectorAll('.sidebar-submenu a')).map((a) => a.textContent!.trim());
    expect(submenuText).toContain('Cotivity Employee');
    expect(submenuText).not.toContain('Department');
  });

  it('marks the active section for the current route', async () => {
    signIn(['administration.view']);
    await router.navigateByUrl('/administration');
    fixture.detectChanges();

    const active = host().querySelector('.sidebar-link.active > .sidebar-label');
    expect(active?.textContent?.trim()).toBe('Administration');
  });

  it('auto-expands the active branch and lets the toggle collapse and reopen it', async () => {
    signIn(['organization.department.view']);
    await router.navigateByUrl('/master/department');
    fixture.detectChanges();

    const toggles = Array.from(host().querySelectorAll<HTMLButtonElement>('.sidebar-toggle'));
    const orgMaster = toggles.find((t) => t.textContent?.includes('Organization Master'))!;

    expect(host().querySelector('.sidebar-submenu a')?.textContent).toContain('Department');

    orgMaster.click();
    fixture.detectChanges();
    expect(orgMaster.getAttribute('aria-expanded')).toBe('false');
    expect(host().querySelector('.sidebar-submenu')).toBeNull();

    orgMaster.click();
    fixture.detectChanges();
    expect(orgMaster.getAttribute('aria-expanded')).toBe('true');
    expect(host().querySelector('.sidebar-submenu a')?.textContent).toContain('Department');
  });
});
