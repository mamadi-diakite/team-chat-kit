import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { CHAT_CONFIG } from '../chat-config';
import {
  ChatUser,
  Contact,
  Invitation,
  InvitationAction,
  Invitations,
  SendInvitationRequest,
} from './contacts.models';

/** The directory, invitations and contacts endpoints of the chat. */
@Injectable({ providedIn: 'root' })
export class ContactsService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(CHAT_CONFIG).apiBaseUrl;

  /** People of my organization, except me, whose name contains `search`. */
  directory(search: string): Observable<ChatUser[]> {
    const term = search.trim();
    const params = term ? new HttpParams().set('search', term) : undefined;
    return this.http.get<ChatUser[]>(`${this.baseUrl}/directory`, { params });
  }

  invitations(): Observable<Invitations> {
    return this.http.get<Invitations>(`${this.baseUrl}/invitations`);
  }

  sendInvitation(request: SendInvitationRequest): Observable<Invitation> {
    return this.http.post<Invitation>(`${this.baseUrl}/invitations`, request);
  }

  /** Accept and decline are for received invitations, cancel for sent ones. */
  answer(invitationId: string, action: InvitationAction): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/invitations/${invitationId}/${action}`, null);
  }

  contacts(): Observable<Contact[]> {
    return this.http.get<Contact[]>(`${this.baseUrl}/contacts`);
  }

  removeContact(userId: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/contacts/${encodeURIComponent(userId)}`);
  }
}
