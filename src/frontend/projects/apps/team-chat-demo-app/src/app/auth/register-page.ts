import { Component, inject } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService, FormSubmission } from '@team-chat-kit/shared/core';
import { FieldErrors } from '@team-chat-kit/shared/ui';

@Component({
  selector: 'tck-register-page',
  imports: [ReactiveFormsModule, RouterLink, FieldErrors],
  template: `
    <form class="form-card" [formGroup]="form" (ngSubmit)="register()" novalidate>
      <h1 class="text-2xl font-bold tracking-tight">Create an account</h1>

      @if (submission.errors().form; as message) {
        <p class="alert-error" role="alert">{{ message }}</p>
      }

      <p class="text-sm text-slate-600">
        You join the organization with this name, or create it if nobody uses it yet.
      </p>

      <div>
        <label class="form-label" for="organizationName">Organization</label>
        <input
          id="organizationName"
          class="form-input"
          formControlName="organizationName"
          autocomplete="organization"
        />
        <tck-field-errors [messages]="submission.messagesFor('organizationName')" />
      </div>

      <div>
        <label class="form-label" for="displayName">Your name</label>
        <input
          id="displayName"
          class="form-input"
          formControlName="displayName"
          autocomplete="name"
        />
        <tck-field-errors [messages]="submission.messagesFor('displayName')" />
      </div>

      <div>
        <label class="form-label" for="email">Email</label>
        <input
          id="email"
          class="form-input"
          type="email"
          formControlName="email"
          autocomplete="email"
        />
        <tck-field-errors [messages]="submission.messagesFor('email')" />
      </div>

      <div>
        <label class="form-label" for="password">Password</label>
        <input
          id="password"
          class="form-input"
          type="password"
          formControlName="password"
          autocomplete="new-password"
        />
        <p class="mt-1 text-xs text-slate-500">At least 8 characters, with a letter and a digit.</p>
        <tck-field-errors [messages]="submission.messagesFor('password')" />
      </div>

      <div>
        <label class="form-label" for="confirmPassword">Confirm password</label>
        <input
          id="confirmPassword"
          class="form-input"
          type="password"
          formControlName="confirmPassword"
          autocomplete="new-password"
        />
        <tck-field-errors [messages]="submission.messagesFor('confirmPassword')" />
      </div>

      <button class="button-primary" type="submit" [disabled]="submission.pending()">
        Create my account
      </button>

      <p class="text-center text-sm text-slate-600">
        Already have an account?
        <a routerLink="/login" class="link">Log in</a>
      </p>
    </form>
  `,
})
export class RegisterPage {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  protected readonly form = inject(NonNullableFormBuilder).group({
    organizationName: ['', Validators.required],
    displayName: ['', Validators.required],
    email: ['', [Validators.required, Validators.email]],
    password: ['', Validators.required],
    confirmPassword: ['', Validators.required],
  });
  protected readonly submission = new FormSubmission(this.form, {
    organizationName: 'Enter your organization.',
    displayName: 'Enter your name.',
    email: 'Enter a valid email address.',
    password: 'Choose a password.',
    confirmPassword: 'Confirm your password.',
  });

  protected register(): void {
    this.submission.submit(
      () => this.auth.register(this.form.getRawValue()),
      () => void this.router.navigateByUrl('/'),
    );
  }
}
