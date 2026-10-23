import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, switchMap, throwError } from 'rxjs';

import { AuthService } from './auth.service';

export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const authService = inject(AuthService);
  const router = inject(Router);
  const isAuthEndpoint = request.url.includes('/api/auth/');
  const isLoginOrRefresh = /\/api\/auth\/(dev-sign-in|refresh-token)$/.test(request.url);
  const token = authService.accessToken;
  const authenticatedRequest = request.clone({
    ...(token ? { setHeaders: { Authorization: `Bearer ${token}` } } : {}),
    ...(isAuthEndpoint ? { withCredentials: true } : {})
  });

  return next(authenticatedRequest).pipe(
    catchError((error: unknown) => {
      if (!(error instanceof HttpErrorResponse) || error.status !== 401 || isLoginOrRefresh) {
        return throwError(() => error);
      }

      return authService.refreshAccessToken().pipe(
        switchMap((response) => next(request.clone({
          setHeaders: { Authorization: `Bearer ${response.accessToken}` }
        }))),
        catchError((refreshError: unknown) => {
          authService.clearSession();
          void router.navigate(['/login']);
          return throwError(() => refreshError);
        })
      );
    })
  );
};
