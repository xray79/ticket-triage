import { ErrorHandler, EnvironmentProviders, makeEnvironmentProviders } from '@angular/core';
import * as Sentry from '@sentry/angular';
import { environment } from '../../../environments/environment';

// Same graceful-fallback shape as the backend's Redis/SMTP/SQS config (ADR 005): when no DSN is
// configured, this is a no-op and Angular's own default ErrorHandler (console.error) still runs -
// there's no separate "telemetry disabled" code path to maintain.
export function provideTelemetry(): EnvironmentProviders {
  if (!environment.sentryDsn) {
    return makeEnvironmentProviders([]);
  }

  Sentry.init({
    dsn: environment.sentryDsn,
    environment: environment.production ? 'production' : 'development'
  });

  return makeEnvironmentProviders([{ provide: ErrorHandler, useValue: Sentry.createErrorHandler() }]);
}
