import { defineConfig, devices } from '@playwright/test';

/**
 * Needs the full stack running (API + Postgres + this app's dev server) — run against a
 * locally-started stack for local dev (see e2e/README.md), and against a real Postgres +
 * Host + ng serve in CI's `e2e` job.
 */
export default defineConfig({
  testDir: './e2e',
  timeout: 30_000,
  use: {
    baseURL: process.env['E2E_BASE_URL'] ?? 'http://localhost:4200',
    screenshot: 'only-on-failure'
  },
  reporter: [['list']],
  projects: [
    {
      name: 'chromium',
      use: {
        ...devices['Desktop Chrome'],
        // Use the environment's pre-installed browser instead of a Playwright-managed
        // download (see CLAUDE session environment notes); harmless to leave in for other
        // environments too since PLAYWRIGHT_BROWSERS_PATH governs the download path anyway.
        launchOptions: process.env['PLAYWRIGHT_CHROMIUM_PATH']
          ? { executablePath: process.env['PLAYWRIGHT_CHROMIUM_PATH'] }
          : {}
      }
    }
  ]
});
