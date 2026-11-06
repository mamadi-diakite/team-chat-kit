import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { Type } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideChat } from '../lib/chat-config';
import { ChatUser } from '../lib/contacts/contacts.models';

export const liam: ChatUser = {
  id: 'user-4',
  displayName: 'Liam Moreau',
  email: 'liam.moreau@example.com',
  organizationName: 'Harbor Logistics',
};

export const noah: ChatUser = {
  id: 'user-6',
  displayName: 'Noah Girard',
  email: 'noah.girard@example.com',
  organizationName: 'Pine Valley Clinic',
};

/** A chat component on a fake API under the `/team/chat` prefix, driven like a user. */
export class ChatHarness<T> {
  readonly http: HttpTestingController;
  readonly fixture: ComponentFixture<T>;

  constructor(component: Type<T>) {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideChat({ apiBaseUrl: '/team/chat' }),
      ],
    });
    this.http = TestBed.inject(HttpTestingController);
    this.fixture = TestBed.createComponent(component);
  }

  get element(): HTMLElement {
    return this.fixture.nativeElement as HTMLElement;
  }

  async settle(): Promise<void> {
    await this.fixture.whenStable();
  }

  async type(id: string, value: string): Promise<void> {
    const input = this.element.querySelector<HTMLInputElement | HTMLTextAreaElement>(`#${id}`)!;
    input.value = value;
    input.dispatchEvent(new Event('input'));
    input.dispatchEvent(new Event('blur'));
    await this.settle();
  }

  async submit(): Promise<void> {
    this.element.querySelector<HTMLButtonElement>('form button[type="submit"]')!.click();
    await this.settle();
  }

  /** Clicks the button with this text inside the first element matching `scope`. */
  async press(label: string, scope = 'section'): Promise<void> {
    const buttons = this.element
      .querySelector(scope)!
      .querySelectorAll<HTMLButtonElement>('button');
    [...buttons].find((button) => button.textContent!.trim() === label)!.click();
    await this.settle();
  }

  text(selector: string): string | undefined {
    return this.element.querySelector(selector)?.textContent?.trim().replace(/\s+/g, ' ');
  }

  texts(selector: string): string[] {
    return [...this.element.querySelectorAll(selector)].map((node) =>
      node.textContent!.trim().replace(/\s+/g, ' '),
    );
  }
}
