import AxeBuilder from '@axe-core/playwright';
import { expect, test } from '@playwright/test';

const ADMIN_EMAIL = process.env['E2E_ADMIN_EMAIL'] ?? 'admin@ticket-triage.local';
const ADMIN_PASSWORD = process.env['E2E_ADMIN_PASSWORD'] ?? 'ChangeMe123!';

async function login(page: import('@playwright/test').Page) {
  await page.goto('/login');
  await page.fill('#email', ADMIN_EMAIL);
  await page.fill('#password', ADMIN_PASSWORD);
  await page.click('button[type=submit]');
  await page.waitForURL('**/tickets');
}

async function expectNoSeriousViolations(page: import('@playwright/test').Page) {
  const results = await new AxeBuilder({ page }).withTags(['wcag2a', 'wcag2aa']).analyze();
  const serious = results.violations.filter((v) => v.impact === 'serious' || v.impact === 'critical');
  expect(serious, JSON.stringify(serious, null, 2)).toEqual([]);
}

test.describe('WCAG 2.1 AA — no serious/critical violations', () => {
  test('login page', async ({ page }) => {
    await page.goto('/login');
    await expectNoSeriousViolations(page);
  });

  test('ticket queue', async ({ page }) => {
    await login(page);
    await expectNoSeriousViolations(page);
  });

  test('provider settings', async ({ page }) => {
    await login(page);
    await page.goto('/settings/provider');
    await expectNoSeriousViolations(page);
  });

  test('org policy (admin)', async ({ page }) => {
    await login(page);
    await page.goto('/admin/org-settings');
    await expectNoSeriousViolations(page);
  });

  test('user management (admin)', async ({ page }) => {
    await login(page);
    await page.goto('/admin/users');
    await expectNoSeriousViolations(page);
  });

  test('reporting dashboard (admin)', async ({ page }) => {
    await login(page);
    await page.goto('/reporting');
    await expectNoSeriousViolations(page);
  });

  test('ticket detail', async ({ page }) => {
    await login(page);
    await page.click('button:has-text("New ticket")');
    await page.fill('#subject', 'Accessibility scan ticket');
    await page.fill('#customerEmail', 'a11y@example.com');
    await page.fill('#body', 'Body text for the accessibility scan.');
    await page.click('button:has-text("Create ticket")');
    await page.waitForTimeout(500);
    await page.click('a:has-text("Accessibility scan ticket")');
    await page.waitForURL('**/tickets/*');
    await expectNoSeriousViolations(page);
  });
});
