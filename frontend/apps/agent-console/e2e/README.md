# E2E / accessibility tests

Needs the full stack running: the .NET API (with a seeded admin user) and this
app's dev server. Not wired into CI (same reasoning as the backend's
Testcontainers integration tests — this sandbox's/CI's network policy may not
have everything the stack needs, e.g. a live Ollama).

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
credentials (defaults match `appsettings.Development.json`).
