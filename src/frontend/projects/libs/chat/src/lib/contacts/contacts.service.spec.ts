import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { provideChat } from '../chat-config';
import { ContactsService } from './contacts.service';

describe('ContactsService', () => {
  let service: ContactsService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideChat({ apiBaseUrl: '/team/chat' }),
      ],
    });
    service = TestBed.inject(ContactsService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('searches the directory under the host route prefix', () => {
    service.directory('  chlo ').subscribe();

    const request = http.expectOne((r) => r.url === '/team/chat/directory');
    expect(request.request.params.get('search')).toBe('chlo');
  });

  it('lists the whole directory when the search is blank', () => {
    service.directory(' ').subscribe();

    expect(http.expectOne('/team/chat/directory').request.params.keys()).toEqual([]);
  });

  it('lists and sends invitations', () => {
    service.invitations().subscribe();
    service.sendInvitation({ email: 'liam.moreau@example.com', message: null }).subscribe();

    http.expectOne({ method: 'GET', url: '/team/chat/invitations' });
    const send = http.expectOne({ method: 'POST', url: '/team/chat/invitations' });
    expect(send.request.body).toEqual({ email: 'liam.moreau@example.com', message: null });
  });

  it.each(['accept', 'decline', 'cancel'] as const)('can %s an invitation', async (action) => {
    const done = firstValueFrom(service.answer('inv-1', action), { defaultValue: undefined });

    const request = http.expectOne(`/team/chat/invitations/inv-1/${action}`);
    expect(request.request.method).toBe('POST');
    request.flush(null, { status: 204, statusText: 'No Content' });

    await expect(done).resolves.toBeNull();
  });

  it('lists contacts and removes one by user id', () => {
    service.contacts().subscribe();
    service.removeContact('user/2').subscribe();

    expect(http.expectOne('/team/chat/contacts').request.method).toBe('GET');
    expect(http.expectOne('/team/chat/contacts/user%2F2').request.method).toBe('DELETE');
  });
});
