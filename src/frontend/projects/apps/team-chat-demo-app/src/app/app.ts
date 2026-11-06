import { Component, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { Router, RouterLink, RouterOutlet } from '@angular/router';
import { ApiHealthService, AuthService, SessionStore } from '@team-chat-kit/shared/core';
import { Shell } from '@team-chat-kit/shared/ui';

@Component({
  selector: 'tck-root',
  imports: [RouterOutlet, RouterLink, Shell],
  template: `
    <tck-shell appName="Team Chat Demo" [apiStatus]="apiStatus()">
      <ng-container shellNav>
        @if (user(); as user) {
          <a routerLink="/directory" class="text-teal-700">Directory</a>
          <a routerLink="/invitations" class="text-teal-700">Invitations</a>
          <a routerLink="/contacts" class="text-teal-700">Contacts</a>
          <span class="text-slate-600" data-testid="user-name"
            >{{ user.displayName }} · {{ user.organizationName }}</span
          >
          <a routerLink="/account/password" class="text-teal-700">Change password</a>
          <button type="button" class="text-teal-700" (click)="logOut()">Log out</button>
        } @else {
          <a routerLink="/login" class="text-teal-700">Log in</a>
          <a routerLink="/register" class="text-teal-700">Create an account</a>
        }
      </ng-container>
      <router-outlet />
    </tck-shell>
  `,
})
export class App {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  protected readonly user = inject(SessionStore).user;
  protected readonly apiStatus = toSignal(inject(ApiHealthService).check(), {
    initialValue: 'checking',
  });

  protected logOut(): void {
    this.auth.logout();
    void this.router.navigateByUrl('/login');
  }
}
