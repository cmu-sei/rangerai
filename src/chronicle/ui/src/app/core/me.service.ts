import { Injectable, inject, signal } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';

/** Who is signed in. User is null without a login configured. */
export interface Me {
  user: string | null;
  /** Signed in with Keycloak, so there's a session to sign out of. */
  signedIn: boolean;
}

/** Who is at the keyboard, read once before the app renders. Null if the API can't say. */
@Injectable({ providedIn: 'root' })
export class MeService {
  private readonly http = inject(HttpClient);
  readonly me = signal<Me | null>(null);

  /**
   * Run by the app initializer, so nothing renders until it's known who is signed in. Without a session the sign-in
   * interceptor is already on its way to Keycloak, so this never settles and the page never shows; any other failure
   * lets the app start, and its pages say what's wrong.
   */
  load(): Promise<void> {
    return firstValueFrom(this.http.get<Me>('/api/me')).then(
      me => this.me.set(me),
      (error: unknown) => {
        if (error instanceof HttpErrorResponse && error.status === 401) return new Promise<void>(() => {});
        this.me.set(null);
        return undefined;
      }
    );
  }
}
