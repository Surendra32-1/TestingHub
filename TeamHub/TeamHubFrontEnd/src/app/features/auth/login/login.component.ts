import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';

import { AuthService } from '../../../core/auth/auth.service';
import { signedInLandingPath } from '../../../core/auth/auth.guard';
import { EntraAuthService } from '../../../core/auth/entra-auth.service';
import { DevSignInUser } from '../../../core/auth/auth.models';
import { TopbarComponent } from '../../../shared/components/topbar/topbar.component';
import { environment } from '../../../../environments/environment';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [TopbarComponent],
  templateUrl: './login.component.html',
  styleUrl: './login.component.css'
})
export class LoginComponent implements OnInit {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly changeDetectorRef = inject(ChangeDetectorRef);

  private readonly entraService = environment.enableEntra ? inject(EntraAuthService) : null;

  readonly enableEntra = environment.enableEntra;
  devUsers: DevSignInUser[] = [];
  errorMessage = '';
  isSubmitting = false;

  ngOnInit(): void {
    if (this.authService.isAuthenticated) {
      this.navigateAfterSignIn();
      return;
    }

    if (this.enableEntra) {
      return;
    }

    this.authService.getDevUsers().subscribe({
      next: (users) => {
        this.devUsers = users;
        this.changeDetectorRef.markForCheck();
      },
      error: () => {
        this.errorMessage = 'Unable to load development accounts. Is the API running?';
        this.changeDetectorRef.markForCheck();
      }
    });
  }

  signInWithMicrosoft(): void {
    this.entraService?.signIn();
  }

  signInAs(user: DevSignInUser): void {
    if (this.isSubmitting) {
      return;
    }

    this.isSubmitting = true;
    this.errorMessage = '';
    this.authService.devSignIn(user.userId).subscribe({
      next: () => this.navigateAfterSignIn(),
      error: (error: unknown) => {
        this.isSubmitting = false;
        this.errorMessage = error instanceof HttpErrorResponse && error.status === 403
          ? 'This account cannot use development sign-in.'
          : 'Unable to sign in right now. Please try again.';
        this.changeDetectorRef.markForCheck();
      },
      complete: () => {
        this.isSubmitting = false;
        this.changeDetectorRef.markForCheck();
      }
    });
  }

  private navigateAfterSignIn(): void {
    const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl');
    const returnPath = returnUrl?.split(/[?#]/, 1)[0].replace(/\/+$/, '') || '';
    const safeReturnUrl = returnUrl?.startsWith('/')
      && !returnUrl.startsWith('//')
      && returnPath !== '/login'
      ? returnUrl
      : signedInLandingPath(this.authService);
    void this.router.navigateByUrl(safeReturnUrl);
  }
}
