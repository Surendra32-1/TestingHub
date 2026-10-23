import { ApplicationConfig, EnvironmentProviders, Provider, importProvidersFrom, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideHttpClient, withInterceptors, withInterceptorsFromDi } from '@angular/common/http';
import { provideRouter } from '@angular/router';
import { provideNativeDateAdapter } from '@angular/material/core';
import { MsalModule } from '@azure/msal-angular';

import { routes } from './app.routes';
import { authInterceptor } from './core/auth/auth.interceptor';
import { environment } from '../environments/environment';
import {
  msalGuardConfigFactory,
  msalInstanceFactory,
  msalInterceptorConfigFactory
} from './core/auth/msal.config';

const httpProviders: (Provider | EnvironmentProviders)[] = environment.enableEntra
  ? [
      provideHttpClient(withInterceptorsFromDi()),
      importProvidersFrom(
        MsalModule.forRoot(
          msalInstanceFactory(),
          msalGuardConfigFactory(),
          msalInterceptorConfigFactory()
        )
      )
    ]
  : [provideHttpClient(withInterceptors([authInterceptor]))];

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    ...httpProviders,
    provideRouter(routes),
    provideNativeDateAdapter()
  ]
};
