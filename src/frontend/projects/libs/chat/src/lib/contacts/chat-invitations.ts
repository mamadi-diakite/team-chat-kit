import { DatePipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { finalize, switchMap } from 'rxjs';
import { LoadState, withLoadState } from '../internal/load-state';
import { NO_PROBLEM, toProblem } from '../internal/problems';
import { Invitation, InvitationAction, Invitations } from './contacts.models';
import { ContactsService } from './contacts.service';

export const INVITATION_MESSAGE_MAX_LENGTH = 500;

const clientMessages: Readonly<Record<'email' | 'message', string>> = {
  email: 'Enter a valid email address.',
  message: `Use at most ${INVITATION_MESSAGE_MAX_LENGTH} characters.`,
};

const notices: Readonly<Record<InvitationAction, (name: string) => string>> = {
  accept: (name) => `You and ${name} are now contacts.`,
  decline: (name) => `Invitation from ${name} declined.`,
  cancel: (name) => `Invitation to ${name} cancelled.`,
};

/** Sends invitations to people of other organizations, and answers the pending ones. */
@Component({
  selector: 'tck-chat-invitations',
  imports: [DatePipe, ReactiveFormsModule],
  template: `
    <section class="space-y-8">
      <div class="space-y-1">
        <h1 class="text-2xl font-bold tracking-tight">Invitations</h1>
        <p class="text-slate-600">
          Invite someone from another organization by email. Once they accept, you are contacts.
        </p>
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

      <form
        class="space-y-4 rounded-xl border border-slate-200 bg-white p-6 shadow-sm"
        [formGroup]="form"
        (ngSubmit)="send()"
        novalidate
      >
        <h2 class="text-lg font-semibold">Send an invitation</h2>
        <div>
          <label class="block text-sm font-medium text-slate-700" for="invitationEmail"
            >Email</label
          >
          <input
            id="invitationEmail"
            type="email"
            formControlName="email"
            autocomplete="off"
            class="mt-1 block w-full rounded-md border border-slate-300 px-3 py-2 shadow-sm"
          />
          @for (message of messagesFor('email'); track message) {
            <p class="mt-1 text-sm text-rose-600" data-testid="field-error">{{ message }}</p>
          }
        </div>
        <div>
          <label class="block text-sm font-medium text-slate-700" for="invitationMessage"
            >Message (optional)</label
          >
          <textarea
            id="invitationMessage"
            rows="3"
            formControlName="message"
            class="mt-1 block w-full rounded-md border border-slate-300 px-3 py-2 shadow-sm"
          ></textarea>
          @for (message of messagesFor('message'); track message) {
            <p class="mt-1 text-sm text-rose-600" data-testid="field-error">{{ message }}</p>
          }
        </div>
        <button
          type="submit"
          class="rounded-md bg-teal-600 px-4 py-2 font-medium text-white hover:bg-teal-700 disabled:opacity-60"
          [disabled]="sending()"
        >
          Send the invitation
        </button>
      </form>

      @let current = state();
      @if (current.status === 'loading') {
        <p class="text-slate-600" role="status">Loading your invitations…</p>
      } @else if (current.status === 'ready') {
        <section class="space-y-3" aria-labelledby="received-title">
          <h2 id="received-title" class="text-lg font-semibold">Received</h2>
          <ul class="divide-y divide-slate-200 rounded-xl border border-slate-200 bg-white">
            @for (invitation of current.value.received; track invitation.id) {
              <li class="space-y-2 px-4 py-3" data-testid="received">
                <p>
                  <span class="font-medium">{{ invitation.user.displayName }}</span>
                  <span class="text-sm text-slate-500">
                    · {{ invitation.user.organizationName }} ·
                    {{ invitation.createdAtUtc | date: 'mediumDate' }}</span
                  >
                </p>
                @if (invitation.message) {
                  <p class="whitespace-pre-line text-sm text-slate-700" data-testid="message">
                    {{ invitation.message }}
                  </p>
                }
                <div class="flex gap-2">
                  <button
                    type="button"
                    class="rounded-md bg-teal-600 px-3 py-1 text-sm text-white disabled:opacity-60"
                    [disabled]="busy()"
                    (click)="answer(invitation, 'accept')"
                  >
                    Accept
                  </button>
                  <button
                    type="button"
                    class="rounded-md border border-slate-300 px-3 py-1 text-sm disabled:opacity-60"
                    [disabled]="busy()"
                    (click)="answer(invitation, 'decline')"
                  >
                    Decline
                  </button>
                </div>
              </li>
            } @empty {
              <li class="px-4 py-3 text-slate-600">No invitations waiting for your answer.</li>
            }
          </ul>
        </section>

        <section class="space-y-3" aria-labelledby="sent-title">
          <h2 id="sent-title" class="text-lg font-semibold">Sent</h2>
          <ul class="divide-y divide-slate-200 rounded-xl border border-slate-200 bg-white">
            @for (invitation of current.value.sent; track invitation.id) {
              <li class="flex items-center justify-between gap-4 px-4 py-3" data-testid="sent">
                <p>
                  <span class="font-medium">{{ invitation.user.displayName }}</span>
                  <span class="text-sm text-slate-500">
                    · {{ invitation.user.organizationName }} ·
                    {{ invitation.createdAtUtc | date: 'mediumDate' }}</span
                  >
                </p>
                <button
                  type="button"
                  class="rounded-md border border-slate-300 px-3 py-1 text-sm disabled:opacity-60"
                  [disabled]="busy()"
                  (click)="answer(invitation, 'cancel')"
                >
                  Cancel
                </button>
              </li>
            } @empty {
              <li class="px-4 py-3 text-slate-600">No pending invitations sent.</li>
            }
          </ul>
        </section>
      } @else {
        <p class="rounded-md bg-rose-50 px-4 py-3 text-sm text-rose-700" role="alert">
          Your invitations could not be loaded. Please try again later.
        </p>
      }
    </section>
  `,
})
export class ChatInvitations {
  private readonly contacts = inject(ContactsService);
  private readonly refresh = signal(0);
  private readonly problem = signal(NO_PROBLEM);

  protected readonly form = inject(NonNullableFormBuilder).group({
    email: ['', [Validators.required, Validators.email]],
    message: ['', Validators.maxLength(INVITATION_MESSAGE_MAX_LENGTH)],
  });
  protected readonly sending = signal(false);
  protected readonly busy = signal(false);
  protected readonly notice = signal<string | null>(null);
  protected readonly error = signal<string | null>(null);
  protected readonly state = toSignal(
    toObservable(this.refresh).pipe(switchMap(() => withLoadState(this.contacts.invitations()))),
    { initialValue: { status: 'loading' } as LoadState<Invitations> },
  );

  /** Server messages for a field, or its client-side message once the user has left it invalid. */
  protected messagesFor(field: 'email' | 'message'): readonly string[] {
    const server = this.problem().fields[field];
    if (server) {
      return server;
    }

    const control = this.form.controls[field];
    return control.touched && control.invalid ? [clientMessages[field]] : [];
  }

  protected send(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const { email, message } = this.form.getRawValue();
    this.clearFeedback();
    this.sending.set(true);
    this.contacts
      .sendInvitation({ email: email.trim(), message: message.trim() || null })
      .pipe(finalize(() => this.sending.set(false)))
      .subscribe({
        next: (invitation) => {
          this.form.reset();
          this.notice.set(`Invitation sent to ${invitation.user.displayName}.`);
          this.refresh.update((count) => count + 1);
        },
        error: (error: unknown) => {
          const problem = toProblem(error);
          this.problem.set(problem);
          this.error.set(problem.message);
        },
      });
  }

  protected answer(invitation: Invitation, action: InvitationAction): void {
    const name = invitation.user.displayName;
    this.clearFeedback();
    this.busy.set(true);
    this.contacts
      .answer(invitation.id, action)
      .pipe(
        finalize(() => {
          this.busy.set(false);
          this.refresh.update((count) => count + 1);
        }),
      )
      .subscribe({
        next: () => this.notice.set(notices[action](name)),
        error: (error: unknown) =>
          this.error.set(toProblem(error, 'This invitation no longer exists.').message),
      });
  }

  private clearFeedback(): void {
    this.problem.set(NO_PROBLEM);
    this.notice.set(null);
    this.error.set(null);
  }
}
