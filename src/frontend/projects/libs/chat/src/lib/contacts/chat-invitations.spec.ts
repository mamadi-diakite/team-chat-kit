import { ChatHarness, liam, noah } from '../../testing/chat-harness';
import { ChatInvitations } from './chat-invitations';
import { Invitation, Invitations } from './contacts.models';

const fromLiam: Invitation = {
  id: 'inv-1',
  user: liam,
  message: 'Hello Emma,\n<b>let us talk</b> about the shipments.',
  createdAtUtc: '2026-11-05T10:00:00Z',
};

const toNoah: Invitation = {
  id: 'inv-2',
  user: noah,
  message: null,
  createdAtUtc: '2026-11-06T10:00:00Z',
};

const pending: Invitations = { received: [fromLiam], sent: [toNoah] };

describe('ChatInvitations', () => {
  let chat: ChatHarness<ChatInvitations>;

  beforeEach(async () => {
    chat = new ChatHarness(ChatInvitations);
    await chat.settle();
  });

  afterEach(() => chat.http.verify());

  async function load(invitations: Invitations = pending): Promise<void> {
    chat.http.expectOne('/team/chat/invitations').flush(invitations);
    await chat.settle();
  }

  describe('lists', () => {
    it('shows received invitations with their message as plain text, and sent ones', async () => {
      expect(chat.text('[role="status"]')).toBe('Loading your invitations…');

      await load();

      expect(chat.text('[data-testid="received"] p')).toBe(
        'Liam Moreau · Harbor Logistics · Nov 5, 2026',
      );
      const message = chat.element.querySelector('[data-testid="message"]')!;
      expect(message.textContent!.trim()).toBe(fromLiam.message);
      expect(message.querySelector('b')).toBeNull();
      expect(chat.text('[data-testid="sent"]')).toBe(
        'Noah Girard · Pine Valley Clinic · Nov 6, 2026 Cancel',
      );
    });

    it('says when there is nothing pending', async () => {
      await load({ received: [{ ...fromLiam, message: null }], sent: [] });

      expect(chat.element.querySelector('[data-testid="message"]')).toBeNull();
      expect(chat.text('[aria-labelledby="sent-title"] li')).toBe('No pending invitations sent.');

      chat.fixture.componentInstance['refresh'].update((count) => count + 1);
      await chat.settle();
      await load({ received: [], sent: [] });

      expect(chat.text('[aria-labelledby="received-title"] li')).toBe(
        'No invitations waiting for your answer.',
      );
    });

    it('explains when the invitations cannot be loaded', async () => {
      chat.http
        .expectOne('/team/chat/invitations')
        .flush(null, { status: 500, statusText: 'Server Error' });
      await chat.settle();

      expect(chat.text('[role="alert"]')).toBe(
        'Your invitations could not be loaded. Please try again later.',
      );
    });
  });

  describe('sending', () => {
    beforeEach(() => load({ received: [], sent: [] }));

    it('asks for a valid email without calling the API', async () => {
      await chat.submit();

      expect(chat.texts('[data-testid="field-error"]')).toEqual(['Enter a valid email address.']);
    });

    it('refuses a message longer than 500 characters', async () => {
      await chat.type('invitationEmail', 'liam.moreau@example.com');
      await chat.type('invitationMessage', 'a'.repeat(501));

      await chat.submit();

      expect(chat.texts('[data-testid="field-error"]')).toEqual(['Use at most 500 characters.']);
    });

    it('sends the invitation, clears the form and reloads the lists', async () => {
      await chat.type('invitationEmail', ' liam.moreau@example.com ');
      await chat.type('invitationMessage', '   ');

      await chat.submit();
      const request = chat.http.expectOne({ method: 'POST', url: '/team/chat/invitations' });
      expect(request.request.body).toEqual({ email: 'liam.moreau@example.com', message: null });
      expect(chat.element.querySelector<HTMLButtonElement>('form button')!.disabled).toBe(true);
      request.flush({ ...toNoah, user: liam }, { status: 201, statusText: 'Created' });
      await chat.settle();
      await load({ received: [], sent: [{ ...toNoah, user: liam }] });

      expect(chat.text('[role="status"]')).toBe('Invitation sent to Liam Moreau.');
      expect(chat.element.querySelector<HTMLInputElement>('#invitationEmail')!.value).toBe('');
      expect(chat.element.querySelector<HTMLButtonElement>('form button')!.disabled).toBe(false);
      expect(chat.texts('[data-testid="sent"] .font-medium')).toEqual(['Liam Moreau']);
    });

    it('sends the trimmed message', async () => {
      await chat.type('invitationEmail', 'noah.girard@example.com');
      await chat.type('invitationMessage', '  Hello Noah.  ');

      await chat.submit();
      const request = chat.http.expectOne({ method: 'POST', url: '/team/chat/invitations' });
      expect(request.request.body).toEqual({
        email: 'noah.girard@example.com',
        message: 'Hello Noah.',
      });
      request.flush(toNoah, { status: 201, statusText: 'Created' });
      await chat.settle();
      await load();
    });

    it('shows why the API refuses the email next to the field', async () => {
      await chat.type('invitationEmail', 'chloe.petit@example.com');

      await chat.submit();
      chat.http.expectOne({ method: 'POST', url: '/team/chat/invitations' }).flush(
        {
          errors: { email: ['This person is in your organization: find them in the directory.'] },
        },
        { status: 400, statusText: 'Bad Request' },
      );
      await chat.settle();

      expect(chat.texts('[data-testid="field-error"]')).toEqual([
        'This person is in your organization: find them in the directory.',
      ]);
      expect(chat.element.querySelector('[role="alert"]')).toBeNull();
    });

    it('shows a general message when the API fails', async () => {
      await chat.type('invitationEmail', 'liam.moreau@example.com');

      await chat.submit();
      chat.http
        .expectOne({ method: 'POST', url: '/team/chat/invitations' })
        .flush(null, { status: 500, statusText: 'Server Error' });
      await chat.settle();

      expect(chat.text('[role="alert"]')).toBe('Something went wrong. Please try again.');
    });
  });

  describe('answering', () => {
    beforeEach(() => load());

    it.each([
      ['Accept', 'received', 'inv-1/accept', 'You and Liam Moreau are now contacts.'],
      ['Decline', 'received', 'inv-1/decline', 'Invitation from Liam Moreau declined.'],
      ['Cancel', 'sent', 'inv-2/cancel', 'Invitation to Noah Girard cancelled.'],
    ])('%s an invitation and reloads the lists', async (label, list, path, notice) => {
      await chat.press(label, `[data-testid="${list}"]`);
      const request = chat.http.expectOne(`/team/chat/invitations/${path}`);
      expect(request.request.method).toBe('POST');
      request.flush(null, { status: 204, statusText: 'No Content' });
      await chat.settle();
      await load({ received: [], sent: [] });

      expect(chat.text('[role="status"]')).toBe(notice);
    });

    it('explains when the invitation is no longer pending', async () => {
      await chat.press('Accept', '[data-testid="received"]');
      chat.http
        .expectOne('/team/chat/invitations/inv-1/accept')
        .flush(
          { title: 'This invitation is no longer pending.' },
          { status: 409, statusText: 'Conflict' },
        );
      await chat.settle();
      await load({ received: [], sent: [toNoah] });

      expect(chat.text('[role="alert"]')).toBe('This invitation is no longer pending.');
    });

    it('explains when the invitation no longer exists', async () => {
      await chat.press('Cancel', '[data-testid="sent"]');
      chat.http
        .expectOne('/team/chat/invitations/inv-2/cancel')
        .flush(null, { status: 404, statusText: 'Not Found' });
      await chat.settle();
      await load({ received: [fromLiam], sent: [] });

      expect(chat.text('[role="alert"]')).toBe('This invitation no longer exists.');
    });
  });
});
