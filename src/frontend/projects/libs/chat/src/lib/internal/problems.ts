import { HttpErrorResponse } from '@angular/common/http';

export interface Problem {
  /** Messages by request field name. */
  readonly fields: Readonly<Record<string, readonly string[]>>;
  /** Message about the whole request. */
  readonly message: string | null;
}

export const NO_PROBLEM: Problem = { fields: {}, message: null };

const unexpectedError = 'Something went wrong. Please try again.';

/** Reads the problem details of the chat endpoints: field errors for 400, the title for other 4xx. */
export function toProblem(error: unknown, notFoundMessage?: string): Problem {
  if (error instanceof HttpErrorResponse && error.status === 404 && notFoundMessage) {
    return { fields: {}, message: notFoundMessage };
  }

  if (error instanceof HttpErrorResponse && error.status >= 400 && error.status < 500) {
    const body = error.error as { title?: string; errors?: Record<string, string[]> } | null;
    if (body?.errors) {
      return { fields: body.errors, message: null };
    }

    return { fields: {}, message: body?.title ?? unexpectedError };
  }

  return { fields: {}, message: unexpectedError };
}
