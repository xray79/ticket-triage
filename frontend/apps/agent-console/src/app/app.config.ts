import { provideHttpClient, withInterceptors, withXhr } from '@angular/common/http';
import { ApplicationConfig, provideZoneChangeDetection } from '@angular/core';
import { provideAnimations } from '@angular/platform-browser/animations';
import { provideRouter } from '@angular/router';
import { NG_EVENT_PLUGINS } from '@taiga-ui/event-plugins';

import { routes } from './app.routes';
import { authInterceptor } from './core/auth/auth.interceptor';
import { telemetryInterceptor } from './core/telemetry/telemetry.interceptor';
import { provideTelemetry } from './core/telemetry/telemetry.providers';

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes),
    provideHttpClient(withXhr(), withInterceptors([authInterceptor, telemetryInterceptor])),
    provideTelemetry(),
    provideAnimations(),
    NG_EVENT_PLUGINS
  ]
};
