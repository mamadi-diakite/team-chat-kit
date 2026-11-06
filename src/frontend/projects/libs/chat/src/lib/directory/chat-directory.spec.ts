import { ChatHarness } from '../../testing/chat-harness';
import { ChatDirectory } from './chat-directory';

const colleagues = [
  {
    id: 'user-3',
    displayName: 'Chloé Petit',
    email: 'chloe.petit@example.com',
    organizationName: 'Bluebird Design',
  },
  {
    id: 'user-2',
    displayName: 'Hugo Bernard',
    email: 'hugo.bernard@example.com',
    organizationName: 'Bluebird Design',
  },
];

describe('ChatDirectory', () => {
  let chat: ChatHarness<ChatDirectory>;

  beforeEach(async () => {
    chat = new ChatHarness(ChatDirectory);
    await chat.settle();
  });

  afterEach(() => chat.http.verify());

  it('lists the people of my organization', async () => {
    expect(chat.text('[role="status"]')).toBe('Loading the directory…');

    const request = chat.http.expectOne('/team/chat/directory');
    expect(request.request.params.keys()).toEqual([]);
    request.flush(colleagues);
    await chat.settle();

    expect(chat.texts('[data-testid="person"] p')).toEqual([
      'Chloé Petit',
      'chloe.petit@example.com',
      'Hugo Bernard',
      'hugo.bernard@example.com',
    ]);
  });

  it('says when nobody else is in my organization', async () => {
    chat.http.expectOne('/team/chat/directory').flush([]);
    await chat.settle();

    expect(chat.text('li')).toBe('Nobody else in your organization yet.');
  });

  it('searches by name', async () => {
    chat.http.expectOne('/team/chat/directory').flush(colleagues);
    await chat.type('directorySearch', '  hug ');

    await chat.submit();
    const request = chat.http.expectOne((r) => r.url === '/team/chat/directory');
    expect(request.request.params.get('search')).toBe('hug');
    request.flush([colleagues[1]]);
    await chat.settle();

    expect(chat.texts('[data-testid="person"] p')).toEqual([
      'Hugo Bernard',
      'hugo.bernard@example.com',
    ]);
  });

  it('says when nobody matches the search', async () => {
    chat.http.expectOne('/team/chat/directory').flush(colleagues);
    await chat.type('directorySearch', 'zoe');

    await chat.submit();
    chat.http.expectOne((r) => r.url === '/team/chat/directory').flush([]);
    await chat.settle();

    expect(chat.text('li')).toBe('Nobody matches this search.');
  });

  it('explains when the directory cannot be loaded', async () => {
    chat.http
      .expectOne('/team/chat/directory')
      .flush(null, { status: 500, statusText: 'Server Error' });
    await chat.settle();

    expect(chat.text('[role="alert"]')).toBe(
      'The directory could not be loaded. Please try again later.',
    );
  });
});
