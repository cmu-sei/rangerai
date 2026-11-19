import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { InjectionToken, inject } from '@angular/core';
import { tap } from 'rxjs';

/** Where the browser goes to sign in with Keycloak, coming back to returnUrl. */
export function signInUrl(returnUrl = location.pathname + location.search): string {
  return `/api/auth/login?returnUrl=${encodeURIComponent(returnUrl)}`;
}

/** Leaves the app for sign-in. Several calls fail at once on the way, so it leaves once. */
export const SIGN_IN = new InjectionToken<() => void>('SIGN_IN', {
  providedIn: 'root',
  factory: () => {
    let leaving = false;
    return () => {
      if (leaving) return;
      leaving = true;
      location.assign(signInUrl());
    };
  }
});

/** The API answers 401 when there's no session (or it ran out): send the person to sign in and back to the page they were on. */
export const signInInterceptor: HttpInterceptorFn = (req, next) => {
  const signIn = inject(SIGN_IN);
  return next(req).pipe(
    tap({
      error: (error: unknown) => {
        if (error instanceof HttpErrorResponse && error.status === 401 && req.url.startsWith('/api/')) signIn();
      }
    })
  );
};
