import { expect, test, Page, APIRequestContext } from '@playwright/test';

const ADMIN_EMAIL = process.env['E2E_ADMIN_EMAIL'] ?? 'admin@ticket-triage.local';
const ADMIN_PASSWORD = process.env['E2E_ADMIN_PASSWORD'] ?? 'ChangeMe123!';
const API_BASE_URL = process.env['E2E_API_BASE_URL'] ?? 'http://localhost:5000';

async function login(page: Page, email: string, password: string): Promise<void> {
  await page.goto('/login');
  await page.fill('#email', email);
  await page.fill('#password', password);
  await page.click('button[type=submit]');
  await page.waitForURL('**/tickets');
}

async function loginAsAdmin(page: Page): Promise<void> {
  await login(page, ADMIN_EMAIL, ADMIN_PASSWORD);
}

// Setup-only step, so it goes through the real API rather than the UI — same accepted pattern
// as seeding data in the backend's own integration tests.
async function createAgentUser(request: APIRequestContext): Promise<{ email: string; password: string }> {
  const loginResponse = await request.post(`${API_BASE_URL}/api/auth/login`, {
    data: { email: ADMIN_EMAIL, password: ADMIN_PASSWORD }
  });
  const { accessToken } = await loginResponse.json();

  const email = `e2e-agent-${Date.now()}@example.com`;
  const password = 'AgentPass123!';
  const createResponse = await request.post(`${API_BASE_URL}/api/users`, {
    headers: { Authorization: `Bearer ${accessToken}` },
    data: { email, password, displayName: 'E2E Agent', role: 'Agent' }
  });
  expect(createResponse.ok()).toBeTruthy();

  return { email, password };
}

test.describe('Critical flows', () => {
  test('rejects an invalid login and stays on the login page', async ({ page }) => {
    await page.goto('/login');
    await page.fill('#email', ADMIN_EMAIL);
    await page.fill('#password', 'definitely-wrong-password');
    await page.click('button[type=submit]');

    await expect(page.locator('[role=alert]')).toHaveText('Invalid email or password.');
    await expect(page).toHaveURL(/\/login$/);
  });

  test('logs in and sees the ticket queue', async ({ page }) => {
    await loginAsAdmin(page);
    await expect(page.locator('h1')).toHaveText('Ticket queue');
  });

  test('creates a ticket with an explicit provider choice, then views its detail', async ({ page }) => {
    await loginAsAdmin(page);

    const subject = `E2E critical-flow ticket ${Date.now()}`;
    await page.click('button:has-text("New ticket")');
    await page.fill('#subject', subject);
    await page.fill('#customerEmail', 'e2e-critical-flow@example.com');
    await page.fill('#body', 'Body text for the critical-flow E2E test.');
    await page.selectOption('#provider', 'anthropic');
    await page.click('button:has-text("Create ticket")');

    await expect(page.locator(`a:has-text("${subject}")`)).toBeVisible();
    await page.click(`a:has-text("${subject}")`);
    await page.waitForURL('**/tickets/*');

    await expect(page.locator('h1')).toHaveText(subject);
    await expect(page.locator('.detail__meta')).toContainText('Status: New');
  });

  test('resolves a ticket', async ({ page }) => {
    await loginAsAdmin(page);

    const subject = `E2E resolve ticket ${Date.now()}`;
    await page.click('button:has-text("New ticket")');
    await page.fill('#subject', subject);
    await page.fill('#customerEmail', 'e2e-resolve@example.com');
    await page.fill('#body', 'Body text for the resolve E2E test.');
    await page.click('button:has-text("Create ticket")');

    await expect(page.locator(`a:has-text("${subject}")`)).toBeVisible();
    await page.click(`a:has-text("${subject}")`);
    await page.waitForURL('**/tickets/*');

    await page.click('button:has-text("Mark resolved")');
    await expect(page.locator('.detail__meta')).toContainText('Status: Resolved');
    await expect(page.locator('button:has-text("Mark resolved")')).toHaveCount(0);
  });

  test('an Agent is blocked from admin-only routes and redirected back to the queue', async ({ page, request }) => {
    const agent = await createAgentUser(request);
    await login(page, agent.email, agent.password);

    await page.goto('/admin/users');
    await page.waitForURL('**/tickets');

    await page.goto('/admin/org-settings');
    await page.waitForURL('**/tickets');

    await page.goto('/reporting');
    await page.waitForURL('**/tickets');
  });
});
