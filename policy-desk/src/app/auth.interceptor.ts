import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { API_BASE_URL } from './api-config';
import { AuthService } from './auth.service';

// Runs on every call to our API:
// 1. adds the login token when there is one
// 2. if the server says the login is no longer valid (401), logs the person out
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);

  if (!req.url.startsWith(API_BASE_URL)) {
    return next(req);
  }

  const token = auth.token();
  const request =
    token && !req.headers.has('Authorization')
      ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
      : req;

  return next(request).pipe(
    catchError((err: unknown) => {
      if (err instanceof HttpErrorResponse && err.status === 401 && token) {
        auth.logout();
      }
      return throwError(() => err);
    }),
  );
};