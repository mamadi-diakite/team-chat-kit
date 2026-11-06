import { ChatHarness, liam, noah } from '../../testing/chat-harness';
import { ChatContacts } from './chat-contacts';
import { Contact } from './contacts.models';

const contacts: Contact[] = [
  { user: liam, sinceUtc: '2026-11-02T10:00:00Z' },
  { user: noah, sinceUtc: '2026-11-04T10:00:00Z' },
];

describe('ChatContacts', () => {
  let chat: ChatHarness<ChatContacts>;

  beforeEach(async () => {
    chat = new ChatHarness(ChatContacts);
    await chat.settle();
  });

  afterEach(() => chat.http.verify());

  async function load(list: Contact[] = contacts): Promise<void> {
    chat.http.expectOne('/team/chat/contacts').flush(list);
    await chat.settle();
  }

  it('lists my contacts with their organization and since when', async () => {
    expect(chat.text('[role="status"]')).toBe('Loading your contacts…');

    await load();

    expect(chat.texts('[data-testid="contact"]')).toEqual([
      'Liam Moreau Harbor Logistics · liam.moreau@example.com · since Nov 2, 2026 Remove',
      'Noah Girard Pine Valley Clinic · noah.girard@example.com · since Nov 4, 2026 Remove',
    ]);
  });

  it('says when there are no contacts yet', async () => {
    await load([]);

    expect(chat.text('li')).toBe(
      'No contacts yet. Invite someone from another organization by email.',
    );
  });

  it('removes a contact and reloads the list', async () => {
    await load();

    await chat.press('Remove', '[data-testid="contact"]');
    const request = chat.http.expectOne('/team/chat/contacts/user-4');
    expect(request.request.method).toBe('DELETE');
    expect(chat.element.querySelector<HTMLButtonElement>('li button')!.disabled).toBe(true);
    request.flush(null, { status: 204, statusText: 'No Content' });
    await chat.settle();
    await load([contacts[1]]);

    expect(chat.text('[role="status"]')).toBe('Liam Moreau was removed from your contacts.');
    expect(chat.texts('[data-testid="contact"] p.font-medium')).toEqual(['Noah Girard']);
    expect(chat.element.querySelector<HTMLButtonElement>('li button')!.disabled).toBe(false);
  });

  it('explains when the contact was already removed', async () => {
    await load();

    await chat.press('Remove', '[data-testid="contact"]');
    chat.http
      .expectOne('/team/chat/contacts/user-4')
      .flush(null, { status: 404, statusText: 'Not Found' });
    await chat.settle();
    await load([contacts[1]]);

    expect(chat.text('[role="alert"]')).toBe('Liam Moreau is no longer one of your contacts.');
  });

  it('explains when the contacts cannot be loaded', async () => {
    chat.http
      .expectOne('/team/chat/contacts')
      .flush(null, { status: 500, statusText: 'Server Error' });
    await chat.settle();

    expect(chat.text('[role="alert"]')).toBe(
      'Your contacts could not be loaded. Please try again later.',
    );
  });
});
