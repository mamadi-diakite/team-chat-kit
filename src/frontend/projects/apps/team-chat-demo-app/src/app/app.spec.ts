import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { CHAT_CONFIG } from '@team-chat-kit/chat';
import { APP_ENVIRONMENT } from '@team-chat-kit/shared/environment';
import { App } from './app';
import { appConfig } from './app.config';

describe('App', () => {
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [...appConfig.providers, provideHttpClientTesting()],
    });
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('uses the host API and the chat endpoints behind the same origin', () => {
    expect(TestBed.inject(APP_ENVIRONMENT).apiBaseUrl).toBe('/api');
    expect(TestBed.inject(CHAT_CONFIG).apiBaseUrl).toBe('/api/chat');
  });

  it('shows the home page, the API status and the chat status once checked', async () => {
    const fixture = TestBed.createComponent(App);
    await TestBed.inject(Router).navigateByUrl('/');
    await fixture.whenStable();
    const element = fixture.nativeElement as HTMLElement;

    expect(element.querySelector('h1')?.textContent).toBe('Chat with your team and your partners');
    expect(element.querySelector('[data-testid="api-status"]')?.textContent).toBe('API checking…');
    expect(element.querySelector('[data-testid="chat-status"]')?.textContent).toBe(
      'Chat module: checking…',
    );

    http.expectOne('/api/health').flush({ status: 'ok' });
    http.expectOne('/api/chat/health').flush({ status: 'ok' });
    await fixture.whenStable();

    expect(element.querySelector('[data-testid="api-status"]')?.textContent).toBe('API online');
    expect(element.querySelector('[data-testid="chat-status"]')?.textContent).toBe(
      'Chat module: online',
    );
  });

  it('shows when the chat module does not answer', async () => {
    const fixture = TestBed.createComponent(App);
    await TestBed.inject(Router).navigateByUrl('/');
    await fixture.whenStable();

    http.expectOne('/api/health').flush({ status: 'ok' });
    http.expectOne('/api/chat/health').flush(null, { status: 404, statusText: 'Not Found' });
    await fixture.whenStable();

    expect(
      (fixture.nativeElement as HTMLElement).querySelector('[data-testid="chat-status"]')
        ?.textContent,
    ).toBe('Chat module: offline');
  });

  it('sends unknown addresses back home', async () => {
    const router = TestBed.inject(Router);

    await router.navigateByUrl('/does-not-exist');

    expect(router.url).toBe('/');
  });
});
