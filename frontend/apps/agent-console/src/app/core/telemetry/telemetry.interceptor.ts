import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import * as Sentry from '@sentry/angular';
import { catchError, tap, throwError } from 'rxjs';

const SLOW_REQUEST_THRESHOLD_MS = 3000;

// A no-op when Sentry isn't initialized (see provideTelemetry) - addBreadcrumb/captureMessage
// are safe to call unconditionally against an inactive client.
export const telemetryInterceptor: HttpInterceptorFn = (req, next) => {
  const startedAt = performance.now();

  return next(req).pipe(
    tap(() => {
      const durationMs = performance.now() - startedAt;
      if (durationMs > SLOW_REQUEST_THRESHOLD_MS) {
        Sentry.captureMessage(`Slow API call: ${req.method} ${req.urlWithParams} took ${Math.round(durationMs)}ms`, 'warning');
      }
    }),
    catchError((error: HttpErrorResponse) => {
      Sentry.addBreadcrumb({
        category: 'http',
        message: `${req.method} ${req.urlWithParams} -> ${error.status}`,
        level: 'error'
      });
      return throwError(() => error);
    })
  );
};
