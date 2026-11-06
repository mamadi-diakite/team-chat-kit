import { Component, inject, signal } from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { NonNullableFormBuilder, ReactiveFormsModule } from '@angular/forms';
import { switchMap } from 'rxjs';
import { ChatUser } from '../contacts/contacts.models';
import { ContactsService } from '../contacts/contacts.service';
import { LoadState, withLoadState } from '../internal/load-state';

/** The people of my organization, searched by name. */
@Component({
  selector: 'tck-chat-directory',
  imports: [ReactiveFormsModule],
  template: `
    <section class="space-y-6">
      <div class="space-y-1">
        <h1 class="text-2xl font-bold tracking-tight">Directory</h1>
        <p class="text-slate-600">The people of your organization.</p>
      </div>

      <form class="flex flex-wrap gap-3" [formGroup]="form" (ngSubmit)="search()" novalidate>
        <label class="sr-only" for="directorySearch">Search by name</label>
        <input
          id="directorySearch"
          type="search"
          formControlName="search"
          placeholder="Search by name"
          class="min-w-0 flex-1 rounded-md border border-slate-300 px-3 py-2 shadow-sm"
        />
        <button
          type="submit"
          class="rounded-md bg-teal-600 px-4 py-2 font-medium text-white hover:bg-teal-700"
        >
          Search
        </button>
      </form>

      @let current = state();
      @if (current.status === 'loading') {
        <p class="text-slate-600" role="status">Loading the directory…</p>
      } @else if (current.status === 'ready') {
        <ul class="divide-y divide-slate-200 rounded-xl border border-slate-200 bg-white">
          @for (person of current.value; track person.id) {
            <li class="px-4 py-3" data-testid="person">
              <p class="font-medium">{{ person.displayName }}</p>
              <p class="text-sm text-slate-500">{{ person.email }}</p>
            </li>
          } @empty {
            <li class="px-4 py-3 text-slate-600">
              {{
                query() ? 'Nobody matches this search.' : 'Nobody else in your organization yet.'
              }}
            </li>
          }
        </ul>
      } @else {
        <p class="rounded-md bg-rose-50 px-4 py-3 text-sm text-rose-700" role="alert">
          The directory could not be loaded. Please try again later.
        </p>
      }
    </section>
  `,
})
export class ChatDirectory {
  private readonly contacts = inject(ContactsService);

  protected readonly form = inject(NonNullableFormBuilder).group({ search: [''] });
  protected readonly query = signal('');
  protected readonly state = toSignal(
    toObservable(this.query).pipe(
      switchMap((query) => withLoadState(this.contacts.directory(query))),
    ),
    { initialValue: { status: 'loading' } as LoadState<ChatUser[]> },
  );

  protected search(): void {
    this.query.set(this.form.getRawValue().search.trim());
  }
}
