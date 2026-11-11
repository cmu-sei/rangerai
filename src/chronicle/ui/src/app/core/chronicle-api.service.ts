import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Audience, ChronicleRecord, ChronicleSummary } from './chronicle.model';

@Injectable({ providedIn: 'root' })
export class ChronicleApiService {
  private readonly http = inject(HttpClient);

  getChronicles(): Observable<ChronicleSummary[]> {
    return this.http.get<ChronicleSummary[]>('/api/chronicles');
  }

  /** One event, as an audience may see it. */
  getChronicle(id: string, audience: Audience): Observable<ChronicleRecord> {
    return this.http.get<ChronicleRecord>(`/api/chronicles/${encodeURIComponent(id)}`, { params: { audience } });
  }
}

/** Words for a failed call. */
export function errorMessage(error: unknown): string {
  if (error instanceof HttpErrorResponse) {
    if (error.status === 0) return 'The Chronicle API is not reachable.';
    if (error.status === 404) return 'There is no such event.';
    return `The API answered HTTP ${error.status}.`;
  }
  return String(error);
}
