export const environment = {
  production: true,
  apiBaseUrl: '/api-base-url-set-at-build-time',
  // Set at build/deploy time (e.g. via a build-time replacement or a config endpoint) once a
  // real Sentry project exists; blank here means provideTelemetry() no-ops, same as this repo's
  // other optional-infrastructure fallbacks (Redis, SMTP, SQS - see ADR 005).
  sentryDsn: ''
};
