import { HttpClient } from '@angular/common/http';
import { Injectable, computed, signal } from '@angular/core';
import { Observable, finalize, shareReplay, tap } from 'rxjs';

import { AuthResponse, AuthSession, CurrentUser, DevSignInUser } from './auth.models';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly apiUrl = `${environment.apiUrl}api/auth`;
  private readonly accessTokenKey = 'teamhub.accessToken';
  private readonly rolesKey = 'teamhub.roles';
  private readonly permissionsKey = 'teamhub.permissions';
  private refreshInFlight$?: Observable<AuthResponse>;
  private readonly sessionRevision = signal(0);

  constructor(private readonly http: HttpClient) {}

  get accessToken(): string | null {
    return sessionStorage.getItem(this.accessTokenKey);
  }

  get isAuthenticated(): boolean {
    return this.accessToken !== null;
  }

  readonly sessionState = computed<AuthSession>(() => {
    this.sessionRevision();
    return {
      isAuthenticated: sessionStorage.getItem(this.accessTokenKey) !== null,
      permissions: new Set(
        (sessionStorage.getItem(this.permissionsKey) ?? '')
          .split('|')
          .map((storedPermission) => storedPermission.trim().toLowerCase())
          .filter((storedPermission) => storedPermission.length > 0)
      )
    };
  });

  hasRole(role: string): boolean {
    return (sessionStorage.getItem(this.rolesKey) ?? '')
      .split('|')
      .some((storedRole) => storedRole.toLowerCase() === role.toLowerCase());
  }

  hasPermission(permission: string): boolean {
    return (sessionStorage.getItem(this.permissionsKey) ?? '')
      .split('|')
      .some((storedPermission) => storedPermission.toLowerCase() === permission.toLowerCase());
  }

  getDevUsers(): Observable<DevSignInUser[]> {
    return this.http.get<DevSignInUser[]>(`${this.apiUrl}/dev-users`);
  }

  devSignIn(userId: number): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiUrl}/dev-sign-in`, { userId }, { withCredentials: true })
      .pipe(tap((response) => this.storeSession(response)));
  }

  refreshAccessToken(): Observable<AuthResponse> {
    if (!this.refreshInFlight$) {
      this.refreshInFlight$ = this.http
        .post<AuthResponse>(`${this.apiUrl}/refresh-token`, {}, { withCredentials: true })
        .pipe(
          tap((response) => this.storeSession(response)),
          finalize(() => {
            this.refreshInFlight$ = undefined;
          }),
          shareReplay({ bufferSize: 1, refCount: false })
        );
    }

    return this.refreshInFlight$;
  }

  restoreSession(): Observable<AuthResponse> {
    return this.refreshAccessToken();
  }

  getCurrentUser(): Observable<CurrentUser> {
    return this.http.get<CurrentUser>(`${this.apiUrl}/me`);
  }

  logout(): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/logout`, {}, { withCredentials: true })
      .pipe(finalize(() => this.clearSession()));
  }

  logoutAll(): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/logout-all`, {}, { withCredentials: true })
      .pipe(finalize(() => this.clearSession()));
  }

  clearSession(): void {
    sessionStorage.removeItem(this.accessTokenKey);
    sessionStorage.removeItem(this.rolesKey);
    sessionStorage.removeItem(this.permissionsKey);
    this.sessionRevision.update((revision) => revision + 1);
  }

  storeSession(response: AuthResponse): void {
    sessionStorage.setItem(this.accessTokenKey, response.accessToken);
    sessionStorage.setItem(this.rolesKey, response.roles.join('|'));
    sessionStorage.setItem(this.permissionsKey, (response.permissions ?? []).join('|'));
    this.sessionRevision.update((revision) => revision + 1);
  }
}
