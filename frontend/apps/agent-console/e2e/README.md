# E2E / accessibility tests

Needs the full stack running: the .NET API (with a seeded admin user) and this
app's dev server. Wired into CI's `e2e` job (Postgres service container + the
real Host + `ng serve`) — neither of these spec files needs LocalStack/SQS or
a live Ollama, so unlike the backend's Testcontainers integration tests, this
suite doesn't need anything CI's network policy might block.

- `accessibility.spec.ts` — WCAG 2.1 AA pass (axe-core) on every authenticated page.
- `critical-flows.spec.ts` — the plan's own list (§10): login (success and
  failure), viewing the queue, creating a ticket with an explicit provider
  choice, viewing a ticket's detail, resolving a ticket, and role-based access
  (an Agent is redirected away from admin-only routes).

For local dev:

```bash
# terminal 1 (from repo root)
docker compose up postgres localstack

# terminal 2
dotnet run --project src/Host

# terminal 3
cd frontend/apps/agent-console
npm start

# terminal 4
npx playwright test
```

Set `E2E_ADMIN_EMAIL` / `E2E_ADMIN_PASSWORD` to override the seeded admin
credentials (defaults match `appsettings.Development.json`), and
`E2E_API_BASE_URL` if the API isn't on `http://localhost:5000`.
