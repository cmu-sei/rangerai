import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Audience, ChronicleRecord, ChronicleSummary, Entry, StreamEvent } from './chronicle.model';

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

  /**
   * Subscribes to an event's entries as an audience: server-sent `entry` events, the visible history first and then
   * each new entry as it is stored. The browser reconnects on its own after an error, resuming from the last id.
   */
  stream(id: string, audience: Audience): Observable<StreamEvent> {
    return new Observable<StreamEvent>(subscriber => {
      if (typeof EventSource === 'undefined') return;
      const source = new EventSource(`/api/chronicles/${encodeURIComponent(id)}/entries/stream?audience=${audience}`);
      source.onopen = () => subscriber.next({ type: 'open' });
      source.onerror = () => subscriber.next({ type: 'error' });
      source.addEventListener('entry', event => {
        try {
          subscriber.next({ type: 'entry', entry: JSON.parse((event as MessageEvent<string>).data) as Entry });
        } catch {
          // A malformed event is skipped; the next refresh reads the record whole.
        }
      });
      return () => source.close();
    });
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
