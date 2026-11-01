import { HttpClient } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { Component } from '@angular/core';
import { of, throwError } from 'rxjs';

import { AuthService } from '../../../core/auth/auth.service';
import { AdminLayoutComponent } from './admin-layout.component';

@Component({ selector: 'app-blank-page', standalone: true, template: '' })
class BlankPageComponent {}

describe('AdminLayoutComponent', () => {
  let fixture: ComponentFixture<AdminLayoutComponent>;
  let authService: AuthService;

  const host = (): HTMLElement => fixture.nativeElement as HTMLElement;

  beforeEach(async () => {
    sessionStorage.clear();

    await TestBed.configureTestingModule({
      imports: [AdminLayoutComponent],
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
    fixture = TestBed.createComponent(AdminLayoutComponent);
  });

  afterEach(() => sessionStorage.clear());

  it('renders the sidebar, top bar, and routed content outlet', () => {
    authService.storeSession({ accessToken: 'token', expiresIn: 900, roles: ['Admin'], permissions: ['administration.view'] });
    fixture.detectChanges();

    expect(host().querySelector('app-admin-sidebar')).not.toBeNull();
    expect(host().querySelector('app-admin-navbar')).not.toBeNull();
    expect(host().querySelector('router-outlet')).not.toBeNull();
  });
});
