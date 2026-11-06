import { DatePipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { switchMap } from 'rxjs';
import { LoadState, withLoadState } from '../internal/load-state';
import { toProblem } from '../internal/problems';
import { Contact } from './contacts.models';
import { ContactsService } from './contacts.service';

/** My contacts in other organizations. */
@Component({
  selector: 'tck-chat-contacts',
  imports: [DatePipe],
  template: `
    <section class="space-y-6">
      <div class="space-y-1">
        <h1 class="text-2xl font-bold tracking-tight">Contacts</h1>
        <p class="text-slate-600">People from other organizations who accepted an invitation.</p>
      </div>

      @if (notice(); as message) {
        <p class="rounded-md bg-emerald-50 px-4 py-3 text-sm text-emerald-800" role="status">
          {{ message }}
        </p>
      }
      @if (error(); as message) {
        <p class="rounded-md bg-rose-50 px-4 py-3 text-sm text-rose-700" role="alert">
          {{ message }}
        </p>
      }

      @let current = state();
      @if (current.status === 'loading') {
        <p class="text-slate-600" role="status">Loading your contacts…</p>
      } @else if (current.status === 'ready') {
        <ul class="divide-y divide-slate-200 rounded-xl border border-slate-200 bg-white">
          @for (contact of current.value; track contact.user.id) {
            <li class="flex items-center justify-between gap-4 px-4 py-3" data-testid="contact">
              <div>
                <p class="font-medium">{{ contact.user.displayName }}</p>
                <p class="text-sm text-slate-500">
                  {{ contact.user.organizationName }} · {{ contact.user.email }} · since
                  {{ contact.sinceUtc | date: 'mediumDate' }}
                </p>
              </div>
              <button
                type="button"
                class="rounded-md border border-slate-300 px-3 py-1 text-sm hover:bg-slate-50 disabled:opacity-60"
                [disabled]="busy()"
                (click)="remove(contact)"
              >
                Remove
              </button>
            </li>
          } @empty {
            <li class="px-4 py-3 text-slate-600">
              No contacts yet. Invite someone from another organization by email.
            </li>
          }
        </ul>
      } @else {
        <p class="rounded-md bg-rose-50 px-4 py-3 text-sm text-rose-700" role="alert">
          Your contacts could not be loaded. Please try again later.
        </p>
      }
    </section>
  `,
})
export class ChatContacts {
  private readonly contacts = inject(ContactsService);
  private readonly refresh = signal(0);

  protected readonly busy = signal(false);
  protected readonly notice = signal<string | null>(null);
  protected readonly error = signal<string | null>(null);
  protected readonly state = toSignal(
    toObservable(this.refresh).pipe(switchMap(() => withLoadState(this.contacts.contacts()))),
    { initialValue: { status: 'loading' } as LoadState<Contact[]> },
  );

  protected remove(contact: Contact): void {
    const name = contact.user.displayName;
    this.busy.set(true);
    this.notice.set(null);
    this.error.set(null);
    this.contacts.removeContact(contact.user.id).subscribe({
      next: () => this.done(() => this.notice.set(`${name} was removed from your contacts.`)),
      error: (error: unknown) =>
        this.done(() =>
          this.error.set(toProblem(error, `${name} is no longer one of your contacts.`).message),
        ),
    });
  }

  private done(show: () => void): void {
    show();
    this.busy.set(false);
    this.refresh.update((count) => count + 1);
  }
}
