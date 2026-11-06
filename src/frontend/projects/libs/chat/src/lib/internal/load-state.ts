import { catchError, map, Observable, of, startWith } from 'rxjs';

export type LoadState<T> =
  | { readonly status: 'loading' }
  | { readonly status: 'ready'; readonly value: T }
  | { readonly status: 'error' };

/** Turns a one-shot request into loading, then ready or error. */
export function withLoadState<T>(source: Observable<T>): Observable<LoadState<T>> {
  return source.pipe(
    map((value): LoadState<T> => ({ status: 'ready', value })),
    catchError(() => of<LoadState<T>>({ status: 'error' })),
    startWith<LoadState<T>>({ status: 'loading' }),
  );
}
