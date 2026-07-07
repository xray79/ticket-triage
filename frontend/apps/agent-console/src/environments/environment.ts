export const environment = {
  production: false,
  apiBaseUrl: 'http://localhost:5000',
  // Empty by default - no Sentry project exists for local dev, and provideTelemetry() no-ops
  // when this is blank rather than erroring.
  sentryDsn: ''
};
