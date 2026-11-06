import { Component, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { RouterOutlet } from '@angular/router';
import { ApiHealthService } from '@team-chat-kit/shared/core';
import { Shell } from '@team-chat-kit/shared/ui';

@Component({
  selector: 'tck-root',
  imports: [RouterOutlet, Shell],
  template: `
    <tck-shell appName="Team Chat Demo" [apiStatus]="apiStatus()">
      <router-outlet />
    </tck-shell>
  `,
})
export class App {
  protected readonly apiStatus = toSignal(inject(ApiHealthService).check(), {
    initialValue: 'checking',
  });
}
