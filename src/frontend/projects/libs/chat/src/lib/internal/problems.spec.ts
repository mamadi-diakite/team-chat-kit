import { HttpErrorResponse } from '@angular/common/http';
import { toProblem } from './problems';

const unexpected = { fields: {}, message: 'Something went wrong. Please try again.' };

describe('toProblem', () => {
  it('reads the field errors of a validation problem', () => {
    const error = new HttpErrorResponse({
      status: 400,
      error: { errors: { email: ['No account uses this email.'] } },
    });

    expect(toProblem(error)).toEqual({
      fields: { email: ['No account uses this email.'] },
      message: null,
    });
  });

  it('uses the problem title for other client errors', () => {
    const error = new HttpErrorResponse({
      status: 409,
      error: { title: 'This invitation is no longer pending.' },
    });

    expect(toProblem(error)).toEqual({
      fields: {},
      message: 'This invitation is no longer pending.',
    });
  });

  it('uses the given message when the item is not found', () => {
    expect(toProblem(new HttpErrorResponse({ status: 404 }), 'Gone.')).toEqual({
      fields: {},
      message: 'Gone.',
    });
  });

  it.each([
    ['a client error without body', new HttpErrorResponse({ status: 404 })],
    ['a server error', new HttpErrorResponse({ status: 500, error: { title: 'Boom' } })],
    ['something else', new Error('boom')],
  ])('shows a generic message for %s', (_case, error) => {
    expect(toProblem(error)).toEqual(unexpected);
  });
});
