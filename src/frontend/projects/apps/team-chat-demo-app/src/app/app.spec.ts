import { TestBed } from '@angular/core/testing';
import { CHAT_CONFIG } from '@team-chat-kit/chat';
import { APP_ENVIRONMENT } from '@team-chat-kit/shared/environment';
import { AppHarness } from '../testing/app-harness';

describe('App', () => {
  let app: AppHarness;

  beforeEach(() => (app = new AppHarness()));

  afterEach(() => app.http.verify());

  it('uses the host API and the chat endpoints behind the same origin', () => {
    expect(TestBed.inject(APP_ENVIRONMENT).apiBaseUrl).toBe('/api');
    expect(TestBed.inject(CHAT_CONFIG).apiBaseUrl).toBe('/api/chat');
  });

  it('shows the home page, the API status, the chat status and the visitor links', async () => {
    await app.router.navigateByUrl('/');
    await app.fixture.whenStable();

    expect(app.text('h1')).toBe('Chat with your team and your partners');
    expect(app.text('[data-testid="chat-status"]')).toBe('Chat module: checking…');

    await app.settle();

    expect(app.text('[data-testid="api-status"]')).toBe('API online');
    expect(app.text('[data-testid="chat-status"]')).toBe('Chat module: online');
    expect(app.texts('header nav a')).toEqual(['Log in', 'Create an account']);
  });

  it('shows when the chat module does not answer', async () => {
    await app.router.navigateByUrl('/');
    await app.fixture.whenStable();

    app.http.expectOne('/api/chat/health').flush(null, { status: 404, statusText: 'Not Found' });
    await app.fixture.whenStable();

    expect(app.text('[data-testid="chat-status"]')).toBe('Chat module: offline');
  });

  it('shows the signed-in user with their organization and logs out to the login page', async () => {
    app.signIn();
    await app.open('/');

    expect(app.text('[data-testid="user-name"]')).toBe('Emma Laurent · Bluebird Design');
    expect(app.texts('header nav a')).toEqual([
      'Directory',
      'Invitations',
      'Contacts',
      'Change password',
    ]);

    await app.click('header nav button');

    expect(app.router.url).toBe('/login');
    expect(app.texts('header nav a')).toEqual(['Log in', 'Create an account']);
  });

  it.each(['/directory', '/invitations', '/contacts'])(
    'asks visitors to log in before opening %s',
    async (url) => {
      await app.open(url);

      expect(app.router.url).toBe(`/login?returnUrl=${encodeURIComponent(url)}`);
    },
  );

  it('shows the chat pages to signed-in users, with their token', async () => {
    app.signIn();

    await app.open('/directory');
    const directory = app.http.expectOne('/api/chat/directory');
    expect(directory.request.headers.get('Authorization')).toBe('Bearer token-1');
    directory.flush([]);
    await app.open('/invitations');
    app.http.expectOne('/api/chat/invitations').flush({ received: [], sent: [] });
    await app.open('/contacts');
    app.http.expectOne('/api/chat/contacts').flush([]);
    await app.settle();

    expect(app.text('h1')).toBe('Contacts');
  });

  it('sends unknown addresses back home', async () => {
    await app.open('/does-not-exist');

    expect(app.router.url).toBe('/');
  });
});
