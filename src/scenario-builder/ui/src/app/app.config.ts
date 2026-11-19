import { ApplicationConfig, inject, provideAppInitializer, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideHttpClient, withFetch, withInterceptors } from '@angular/common/http';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { routes } from './app.routes';
import { MeService } from './core/services/me.service';
import { signInInterceptor } from './core/services/sign-in.interceptor';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes, withComponentInputBinding()),
    provideHttpClient(withFetch(), withInterceptors([signInInterceptor])),
    // Who is signed in, before anything renders: without a session, straight to Keycloak with nothing shown.
    provideAppInitializer(() => inject(MeService).load())
  ]
};
