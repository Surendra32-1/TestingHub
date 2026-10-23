import { Injectable, inject } from '@angular/core';
import { MsalService } from '@azure/msal-angular';

import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class EntraAuthService {
  private readonly msal = inject(MsalService);

  signIn(): void {
    void this.msal.loginRedirect({ scopes: [environment.entra.apiScope] });
  }

  signOut(): void {
    void this.msal.logoutRedirect({ postLogoutRedirectUri: environment.entra.postLogoutRedirectUri });
  }
}
