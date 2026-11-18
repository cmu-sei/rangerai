import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';

/** One of the Ranger apps, as the API's appsettings.json lists them; unlinked without a URL. */
export interface RangerApp {
  id: string;
  name: string;
  url?: string | null;
}

/** The Ranger apps, for the top bar's switcher, read once; empty if the API can't say. */
@Injectable({ providedIn: 'root' })
export class AppsService {
  readonly apps = signal<RangerApp[]>([]);

  constructor() {
    inject(HttpClient).get<RangerApp[]>('/api/apps').subscribe({ next: apps => this.apps.set(apps), error: () => {} });
  }
}
