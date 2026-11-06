import { TestBed } from '@angular/core/testing';
import { CHAT_CONFIG, provideChat } from './chat-config';

describe('provideChat', () => {
  it('uses the default route prefix when nothing is provided', () => {
    expect(TestBed.inject(CHAT_CONFIG)).toEqual({ apiBaseUrl: '/api/chat' });
  });

  it('keeps the default route prefix when the host gives no options', () => {
    TestBed.configureTestingModule({ providers: [provideChat()] });

    expect(TestBed.inject(CHAT_CONFIG)).toEqual({ apiBaseUrl: '/api/chat' });
  });

  it('uses the route prefix chosen by the host', () => {
    TestBed.configureTestingModule({ providers: [provideChat({ apiBaseUrl: '/team/chat' })] });

    expect(TestBed.inject(CHAT_CONFIG)).toEqual({ apiBaseUrl: '/team/chat' });
  });
});
