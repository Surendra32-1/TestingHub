import { HttpClient } from '@angular/common/http';
import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';

import { AuthService } from './core/auth/auth.service';
import { App } from './app';

@Component({ selector: 'app-blank-page', standalone: true, template: '' })
class BlankPageComponent {}

describe('App shell', () => {
  let authService: AuthService;

  const signIn = (roles: string[], permissions: string[]) =>
    authService.storeSession({ accessToken: 'token', expiresIn: 900, roles, permissions });

  beforeEach(async () => {
    sessionStorage.clear();

    await TestBed.configureTestingModule({
      imports: [App],
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
  });

  afterEach(() => sessionStorage.clear());

  it('should create the app', () => {
    const fixture = TestBed.createComponent(App);
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('renders the Navbar 1 top bar shell for non-admin users', async () => {
    signIn(['NormalUser'], ['people.allemployee.search.view', 'profile.view']);
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    await fixture.whenStable();

    const el = fixture.nativeElement as HTMLElement;
    expect(el.querySelector('app-topbar')).not.toBeNull();
    expect(el.querySelector('app-admin-layout')).toBeNull();
  });

  it('renders the admin layout shell for admin users', async () => {
    signIn(['Admin'], ['administration.view']);
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    await fixture.whenStable();

    const el = fixture.nativeElement as HTMLElement;
    expect(el.querySelector('app-admin-layout')).not.toBeNull();
    expect(el.querySelector('app-topbar')).toBeNull();
  });
});
