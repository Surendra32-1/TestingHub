import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router, convertToParamMap, provideRouter } from '@angular/router';
import { of } from 'rxjs';

import { AuthService } from '../../../core/auth/auth.service';
import { LoginComponent } from './login.component';

describe('LoginComponent', () => {
  let fixture: ComponentFixture<LoginComponent>;
  let router: Router;
  let grantedPermissions: string[];
  let authService: {
    isAuthenticated: boolean;
    hasPermission: (permission: string) => boolean;
    getDevUsers: ReturnType<typeof vi.fn>;
    devSignIn: ReturnType<typeof vi.fn>;
  };

  beforeEach(async () => {
    grantedPermissions = ['people.allemployee.search.view'];
    authService = {
      isAuthenticated: false,
      hasPermission: (permission) => grantedPermissions.includes(permission),
      getDevUsers: vi.fn(() => of([])),
      devSignIn: vi.fn(() => of({
        accessToken: 'test-token',
        expiresIn: 900,
        roles: [],
        permissions: grantedPermissions
      }))
    };

    await TestBed.configureTestingModule({
      imports: [LoginComponent],
      providers: [
        provideRouter([]),
        { provide: AuthService, useValue: authService },
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              queryParamMap: convertToParamMap({ returnUrl: '/login' })
            }
          }
        }
      ]
    })
      .compileComponents();

    router = TestBed.inject(Router);
    vi.spyOn(router, 'navigateByUrl').mockResolvedValue(true);
    fixture = TestBed.createComponent(LoginComponent);
  });

  it('sends an already authenticated visitor away from the login page', () => {
    authService.isAuthenticated = true;

    fixture.detectChanges();

    expect(authService.getDevUsers).not.toHaveBeenCalled();
    expect(router.navigateByUrl).toHaveBeenCalledWith('/people');
  });

  it('sends an authenticated user with both page permissions to Administration', () => {
    grantedPermissions = ['people.allemployee.search.view', 'administration.view'];
    authService.isAuthenticated = true;

    fixture.detectChanges();

    expect(router.navigateByUrl).toHaveBeenCalledWith('/administration');
  });

  it('does not return to the login page after a successful sign-in', () => {
    fixture.detectChanges();
    fixture.componentInstance.signInAs({
      userId: 1,
      username: 'test.user',
      displayName: 'Test User',
      roles: []
    });

    expect(router.navigateByUrl).toHaveBeenCalledWith('/people');
  });
});
