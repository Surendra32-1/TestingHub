import {
  Configuration,
  IPublicClientApplication,
  InteractionType,
  PublicClientApplication
} from '@azure/msal-browser';
import { MsalGuardConfiguration, MsalInterceptorConfiguration } from '@azure/msal-angular';

import { environment } from '../../../environments/environment';

export function msalInstanceFactory(): IPublicClientApplication {
  const config: Configuration = {
    auth: {
      clientId: environment.entra.clientId,
      authority: environment.entra.authority,
      redirectUri: environment.entra.redirectUri,
      postLogoutRedirectUri: environment.entra.postLogoutRedirectUri
    },
    cache: {
      cacheLocation: 'sessionStorage'
    }
  };
  return new PublicClientApplication(config);
}

export function msalGuardConfigFactory(): MsalGuardConfiguration {
  return {
    interactionType: InteractionType.Redirect,
    authRequest: { scopes: [environment.entra.apiScope] },
    loginFailedRoute: '/login'
  };
}

export function msalInterceptorConfigFactory(): MsalInterceptorConfiguration {
  const protectedResourceMap = new Map<string, Array<string>>();
  protectedResourceMap.set(environment.apiUrl, [environment.entra.apiScope]);
  return {
    interactionType: InteractionType.Redirect,
    protectedResourceMap
  };
}
