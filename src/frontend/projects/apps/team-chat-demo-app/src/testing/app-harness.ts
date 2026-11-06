import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { LoginResponse, SessionStore } from '@team-chat-kit/shared/core';
import { App } from '../app/app';
import { appConfig } from '../app/app.config';

export const demoSession: LoginResponse = {
  accessToken: 'token-1',
  expiresAtUtc: '2999-01-01T00:00:00Z',
  user: {
    id: 'user-1',
    email: 'emma.laurent@example.com',
    displayName: 'Emma Laurent',
    organizationId: 'org-1',
    organizationName: 'Bluebird Design',
  },
};

/** The whole app on a fake API, with helpers to drive it like a user. */
export class AppHarness {
  readonly http: HttpTestingController;
  readonly router: Router;
  readonly fixture: ComponentFixture<App>;

  constructor() {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [...appConfig.providers, provideHttpClientTesting()],
    });
    this.http = TestBed.inject(HttpTestingController);
    this.router = TestBed.inject(Router);
    this.fixture = TestBed.createComponent(App);
    this.http.expectOne('/api/health').flush({ status: 'ok' });
  }

  get element(): HTMLElement {
    return this.fixture.nativeElement as HTMLElement;
  }

  signIn(): void {
    TestBed.inject(SessionStore).start(demoSession);
  }

  async open(url: string): Promise<void> {
    await this.router.navigateByUrl(url);
    await this.settle();
  }

  async type(id: string, value: string): Promise<void> {
    const input = this.element.querySelector<HTMLInputElement>(`#${id}`)!;
    input.value = value;
    input.dispatchEvent(new Event('input'));
    input.dispatchEvent(new Event('blur'));
    await this.fixture.whenStable();
  }

  async click(selector: string): Promise<void> {
    this.element.querySelector<HTMLElement>(selector)!.click();
    await this.fixture.whenStable();
  }

  async submit(): Promise<void> {
    this.element.querySelector<HTMLButtonElement>('form button[type="submit"]')!.click();
    await this.fixture.whenStable();
  }

  /** Waits for the page, answering the chat status check the home page makes. */
  async settle(): Promise<void> {
    await this.fixture.whenStable();
    for (const request of this.http.match('/api/chat/health')) {
      request.flush({ status: 'ok' });
    }
    await this.fixture.whenStable();
  }

  text(selector: string): string | undefined {
    return this.element.querySelector(selector)?.textContent?.trim();
  }

  texts(selector: string): string[] {
    return [...this.element.querySelectorAll(selector)].map((node) => node.textContent!.trim());
  }

  button(selector = 'form button[type="submit"]'): HTMLButtonElement {
    return this.element.querySelector<HTMLButtonElement>(selector)!;
  }
}
