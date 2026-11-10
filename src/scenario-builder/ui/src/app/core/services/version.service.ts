import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { ApiVersion } from '../models/builder.model';

/** The API's release number, read once. Null until it answers, or if it can't. */
@Injectable({ providedIn: 'root' })
export class VersionService {
  readonly version = signal<string | null>(null);

  constructor() {
    inject(HttpClient).get<ApiVersion>('/api/version').subscribe({
      next: v => this.version.set(v.version),
      error: () => this.version.set(null)
    });
  }
}
