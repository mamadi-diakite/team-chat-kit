import { firstValueFrom, of, throwError, toArray } from 'rxjs';
import { withLoadState } from './load-state';

describe('withLoadState', () => {
  it('starts loading, then holds the value', async () => {
    const states = await firstValueFrom(withLoadState(of(3)).pipe(toArray()));

    expect(states).toEqual([{ status: 'loading' }, { status: 'ready', value: 3 }]);
  });

  it('ends in error when the request fails', async () => {
    const states = await firstValueFrom(
      withLoadState(throwError(() => new Error('boom'))).pipe(toArray()),
    );

    expect(states).toEqual([{ status: 'loading' }, { status: 'error' }]);
  });
});
