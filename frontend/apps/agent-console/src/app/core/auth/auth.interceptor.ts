import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, from, switchMap, throwError } from 'rxjs';
import { AuthService } from './auth.service';

function correlationId(): string {
  return crypto.randomUUID();
}

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const router = inject(Router);

  const token = auth.accessToken;
  const withCorrelation = req.clone({
    setHeaders: {
      'X-Correlation-Id': correlationId(),
      ...(token ? { Authorization: `Bearer ${token}` } : {})
    }
  });

  return next(withCorrelation).pipe(
    catchError((error: HttpErrorResponse) => {
      const isAuthEndpoint = req.url.includes('/api/auth/');
      if (error.status === 401 && auth.refreshToken && !isAuthEndpoint) {
        return from(auth.refresh()).pipe(
          switchMap((result) =>
            next(
              withCorrelation.clone({
                setHeaders: { Authorization: `Bearer ${result.accessToken}` }
              })
            )
          ),
          catchError((refreshError) => {
            auth.logout();
            router.navigate(['/login']);
            return throwError(() => refreshError);
          })
        );
      }

      if (error.status === 401) {
        auth.logout();
        router.navigate(['/login']);
      }

      return throwError(() => error);
    })
  );
};
