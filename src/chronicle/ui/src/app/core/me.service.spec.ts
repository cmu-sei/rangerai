import { TestBed } from '@angular/core/testing';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { MeService } from './me.service';
import { SIGN_IN, signInInterceptor } from './sign-in.interceptor';

describe('MeService.load', () => {
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
      service: TestBed.inject(MeService),
      backend: TestBed.inject(HttpTestingController)
    };
  }

  it('lets the app start once it knows who is signed in', async () => {
    const { service, backend } = setup();
    const loaded = service.load();
    backend.expectOne('/api/me').flush({ user: 'lee', signedIn: true });
    await loaded;
    expect(service.me()).toEqual({ user: 'lee', signedIn: true });
  });

  it('never lets the app start without a session, so nothing shows before Keycloak', async () => {
    const { signIn, service, backend } = setup();
    let settled = false;
    service.load().then(() => (settled = true));
    backend.expectOne('/api/me').flush(null, { status: 401, statusText: 'Unauthorized' });
    await new Promise(resolve => setTimeout(resolve, 20));
    expect(signIn).toHaveBeenCalledTimes(1);
    expect(settled).toBe(false);
  });

  it('starts the app anyway when the API is down, for its pages to say so', async () => {
    const { signIn, service, backend } = setup();
    const loaded = service.load();
    backend.expectOne('/api/me').flush(null, { status: 502, statusText: 'Bad Gateway' });
    await loaded;
    expect(service.me()).toBeNull();
    expect(signIn).not.toHaveBeenCalled();
  });
});
