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
    expect(host().querySelector('.sign-out')).toBeNull();
  });

  it('renders only the entries the signed-in permissions allow', () => {
    signIn(['master.department', 'user-roles.assign']);
    fixture.detectChanges();

    expect(itemLabels()).toEqual(['People', 'Manage', 'Settings']);
    expect(host().querySelector('.sign-out')).not.toBeNull();
  });

  it('removes every protected entry when the user signs out', async () => {
    signIn(['master.department']);
    fixture.detectChanges();
    expect(itemLabels()).toContain('Manage');

    host().querySelector<HTMLButtonElement>('.sign-out')!.click();
    await fixture.whenStable();
    await new Promise<void>((resolve) => setTimeout(resolve, 0));
    fixture.detectChanges();

    expect(itemLabels()).toEqual(['Home', 'Login']);
    expect(router.url).toBe('/login');
  });

  it('re-renders the navigation when another account takes over the session', () => {
    signIn(['user-roles.assign']);
    fixture.detectChanges();
    expect(itemLabels()).toEqual(['People', 'Settings']);

    signIn(['terminations.view']);
    fixture.detectChanges();

    expect(itemLabels()).toEqual(['People', 'Terminations']);
  });
});
