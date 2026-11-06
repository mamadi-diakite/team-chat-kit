import { InjectionToken, Provider } from '@angular/core';

export interface ChatConfig {
  /** Base URL of the chat endpoints without a trailing slash, matching the host `RoutePrefix`. */
  readonly apiBaseUrl: string;
}

export const defaultChatConfig: ChatConfig = { apiBaseUrl: '/api/chat' };

export const CHAT_CONFIG = new InjectionToken<ChatConfig>('CHAT_CONFIG', {
  providedIn: 'root',
  factory: () => defaultChatConfig,
});

/** Points the chat at the host API. The host must also send its credentials with each request. */
export function provideChat(config: Partial<ChatConfig> = {}): Provider {
  return { provide: CHAT_CONFIG, useValue: { ...defaultChatConfig, ...config } };
}
