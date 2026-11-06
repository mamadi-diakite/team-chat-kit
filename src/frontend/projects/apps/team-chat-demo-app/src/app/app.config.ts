import { provideHttpClient, withFetch } from '@angular/common/http';
import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideChat } from '@team-chat-kit/chat';
import { provideAppEnvironment } from '@team-chat-kit/shared/environment';
import { routes } from './app.routes';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes),
    provideHttpClient(withFetch()),
    provideAppEnvironment({ apiBaseUrl: '/api' }),
    provideChat({ apiBaseUrl: '/api/chat' }),
  ],
};
