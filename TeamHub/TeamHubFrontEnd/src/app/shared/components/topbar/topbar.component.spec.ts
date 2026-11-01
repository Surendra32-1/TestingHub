import { HttpClient } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { Component } from '@angular/core';
import { of, throwError } from 'rxjs';

import { AuthService } from '../../../core/auth/auth.service';
import { TopbarComponent } from './topbar.component';

@Component({ selector: 'app-blank-page', standalone: true, template: '' })
class BlankPageComponent {}

describe('TopbarComponent (Navbar 1 - employee)', () => {
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
    authService.storeSession({ accessToken: 'token', expiresIn: 900, roles: ['NormalUser'], permissions });

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

  it('shows the TeamHub brand since this navbar has no sidebar', () => {
    const brand = host().querySelector('.topbar-brand');
    expect(brand).not.toBeNull();
    expect(brand?.textContent).toContain('TeamHub');
  });

  it('renders the employee navbar (People + Profile) when no admin module is granted', () => {
    signIn(['people.allemployee.search.view', 'profile.view', 'profile.edit']);
    fixture.detectChanges();

    expect(host().querySelector('app-admin-navbar')).toBeNull();
    expect(itemLabels()).toEqual(['People']);
    expect(host().querySelector('.profile-trigger')).not.toBeNull();
  });

  it('keeps the employee navbar for a profile-only signed-in user', () => {
    signIn(['profile.view', 'profile.edit']);
    fixture.detectChanges();

    expect(host().querySelector('app-admin-navbar')).toBeNull();
    expect(host().querySelector('.profile-trigger')).not.toBeNull();
  });

  it('opens the profile menu and closes it when clicking outside', () => {
    signIn(['people.allemployee.search.view', 'profile.view', 'profile.edit']);
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
    signIn(['people.allemployee.search.view', 'profile.view']);
    fixture.detectChanges();
    expect(itemLabels()).toContain('People');

    host().querySelector<HTMLButtonElement>('.profile-trigger')!.click();
    fixture.detectChanges();
    host().querySelector<HTMLButtonElement>('.profile-dropdown button')!.click();
    await fixture.whenStable();
    await new Promise<void>((resolve) => setTimeout(resolve, 0));
    fixture.detectChanges();

    expect(itemLabels()).toEqual(['Home', 'Login']);
    expect(router.url).toBe('/login');
  });
});
