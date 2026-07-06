# Load test — concurrent ticket ingestion

Stretch stage S3 from the delivery plan: drive concurrent load at `POST /api/tickets`
and find where something actually breaks, rather than asserting it "should" hold up.
See [`docs/load-test-report.md`](../docs/load-test-report.md) for the write-up and
real numbers from a run against this sandbox's local stack.

## Why autocannon instead of k6

The plan names k6 but explicitly allows "k6 or similar." k6's own install path (the
GitHub release tarball, `dl.k6.io`, and the `grafana/k6` Docker image) is blocked by
this sandbox's egress policy the same way Docker Hub and the Terraform Registry are
documented as blocked elsewhere in this repo. The npm registry *is* reachable here,
so [autocannon](https://github.com/mcollina/autocannon) — a comparable HTTP load
generator (concurrent connections, requests/sec, latency percentiles, per-status-code
counts) — was used instead, which let this stage produce a real measured run rather
than an unrun script. `npm audit` flags a moderate transitive `uuid` advisory in
autocannon's own dependency tree; the only fix is downgrading to a much older
autocannon with a different result API, and this is a dev-only load-testing tool
never shipped in any deployed artifact, so the tradeoff wasn't taken.

## Running it

```bash
cd loadtest
npm install

# Requires the API running and reachable, and the seed admin account present
# (docker compose up, or dotnet run --project src/Host).
LOADTEST_BASE_URL="http://localhost:5000" npm run loadtest
```

Environment variables (all optional, defaults shown):

| Variable | Default | Meaning |
|---|---|---|
| `LOADTEST_BASE_URL` | `http://localhost:5000` | API base URL |
| `LOADTEST_EMAIL` / `LOADTEST_PASSWORD` | seed admin credentials | Login used to obtain a bearer token |
| `LOADTEST_DURATION_SECONDS` | `10` | Duration of each connection-level run |
| `LOADTEST_CONNECTIONS` | `5,20,50,100` | Comma-separated concurrency levels to run in sequence |

The script logs in once, then runs one autocannon pass per concurrency level against
`POST /api/tickets` with a fixed ticket body, printing a results table (req/s,
p50/p99 latency, 2xx/429/other status counts) at the end.
