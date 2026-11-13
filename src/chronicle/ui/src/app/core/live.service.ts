import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Audience } from './chronicle.model';
import { LiveMessage, LiveState } from './live.model';

/** What a live connection yields: its state as it changes, and each message. */
export type LiveEvent = { type: 'state'; state: LiveState } | { type: 'message'; message: LiveMessage };

/** The live side of an event: the WebSocket feed, and starting a subscriber's agent. */
@Injectable({ providedIn: 'root' })
export class LiveService {
  private readonly http = inject(HttpClient);

  /**
   * Opens /api/chronicles/{id}/live as an audience, and reopens it with backoff (1 s, doubling to 15 s) when it drops.
   * Each open starts with a hello, so a reconnect replaces what the page holds rather than adding to it.
   */
  connect(id: string, audience: Audience): Observable<LiveEvent> {
    return new Observable<LiveEvent>(subscriber => {
      if (typeof WebSocket === 'undefined') return;
      let socket: WebSocket | null = null;
      let delay = 1000;
      let timer: ReturnType<typeof setTimeout> | undefined;
      let stopped = false;
      const open = (first: boolean) => {
        subscriber.next({ type: 'state', state: first ? 'connecting' : 'reconnecting' });
        const scheme = location.protocol === 'https:' ? 'wss' : 'ws';
        socket = new WebSocket(`${scheme}://${location.host}/api/chronicles/${encodeURIComponent(id)}/live?audience=${audience}`);
        socket.onopen = () => {
          delay = 1000;
          subscriber.next({ type: 'state', state: 'live' });
        };
        socket.onmessage = event => {
          try {
            subscriber.next({ type: 'message', message: JSON.parse(event.data as string) as LiveMessage });
          } catch {
            // A message that isn't JSON is skipped.
          }
        };
        socket.onclose = () => {
          if (stopped) return;
          subscriber.next({ type: 'state', state: 'reconnecting' });
          timer = setTimeout(() => open(false), delay);
          delay = Math.min(delay * 2, 15000);
        };
      };
      open(true);
      return () => {
        stopped = true;
        clearTimeout(timer);
        socket?.close();
      };
    });
  }

  /** Starts a subscriber's agent; follow it on the live feed. */
  run(id: string, subscriberId: string): Observable<{ runId: string; subscriberId: string }> {
    return this.http.post<{ runId: string; subscriberId: string }>(
      `/api/chronicles/${encodeURIComponent(id)}/subscribers/${encodeURIComponent(subscriberId)}/run`, {});
  }
}
