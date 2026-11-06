import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { provideChat } from './chat-config';
import { ChatHealthService } from './chat-health.service';

describe('ChatHealthService', () => {
  let service: ChatHealthService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideChat({ apiBaseUrl: '/team/chat' }),
      ],
    });
    service = TestBed.inject(ChatHealthService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('reports online when the chat endpoints answer ok under the host prefix', async () => {
    const status = firstValueFrom(service.check());

    http.expectOne('/team/chat/health').flush({ status: 'ok' });

    expect(await status).toBe('online');
  });

  it.each([{ status: 'degraded' }, {}, null])(
    'reports offline for an unexpected body (%o)',
    async (body) => {
      const status = firstValueFrom(service.check());

      http.expectOne('/team/chat/health').flush(body);

      expect(await status).toBe('offline');
    },
  );

  it('reports offline when the request fails', async () => {
    const status = firstValueFrom(service.check());

    http.expectOne('/team/chat/health').flush(null, { status: 404, statusText: 'Not Found' });

    expect(await status).toBe('offline');
  });
});
