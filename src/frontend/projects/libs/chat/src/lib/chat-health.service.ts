import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { catchError, map, Observable, of } from 'rxjs';
import { CHAT_CONFIG } from './chat-config';

export type ChatStatus = 'checking' | 'online' | 'offline';

@Injectable({ providedIn: 'root' })
export class ChatHealthService {
  private readonly http = inject(HttpClient);
  private readonly config = inject(CHAT_CONFIG);

  /** Emits `online` when the chat endpoints answer `ok`, `offline` otherwise. Never errors. */
  check(): Observable<ChatStatus> {
    return this.http.get<{ status?: string }>(`${this.config.apiBaseUrl}/health`).pipe(
      map((body): ChatStatus => (body?.status === 'ok' ? 'online' : 'offline')),
      catchError(() => of<ChatStatus>('offline')),
    );
  }
}
