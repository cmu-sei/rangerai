import { TestBed } from '@angular/core/testing';
import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { SIGN_IN, signInInterceptor, signInUrl } from './sign-in.interceptor';

describe('signInInterceptor', () => {
  function setup() {
    const signIn = vi.fn();
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([signInInterceptor])),
        provideHttpClientTesting(),
        { provide: SIGN_IN, useValue: signIn }
      ]
    });
    return {
      signIn,
      http: TestBed.inject(HttpClient),
      backend: TestBed.inject(HttpTestingController)
    };
  }

  it('sends the browser to sign in when the API says there is no session', () => {
    const { signIn, http, backend } = setup();
    http.get('/api/me').subscribe({ error: () => {} });
    backend.expectOne('/api/me').flush(null, { status: 401, statusText: 'Unauthorized' });
    expect(signIn).toHaveBeenCalledTimes(1);
  });

  it('leaves other errors to the page', () => {
    const { signIn, http, backend } = setup();
    http.get('/api/scenarios/9').subscribe({ error: () => {} });
    backend.expectOne('/api/scenarios/9').flush(null, { status: 403, statusText: 'Forbidden' });
    expect(signIn).not.toHaveBeenCalled();
  });

  it('comes back to the page it left', () => {
    expect(signInUrl('/scenarios/3?x=1')).toBe('/api/auth/login?returnUrl=%2Fscenarios%2F3%3Fx%3D1');
  });
});
