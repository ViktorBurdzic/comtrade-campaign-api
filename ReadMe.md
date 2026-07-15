# Customer Service Campaign API

Recruitment task solution for **Comtrade System Integration** — Task #1: *Customer service campaign*.

A telecom runs a one-week campaign where agents reward loyal customers with discounts
(**max 5 customers per agent per day**, mistakes must be correctable). A month later a
**.csv purchase report** arrives and must be **merged** with the campaign data and exposed
through **secure, easy-to-integrate APIs** consumable by multiple CRM systems.

## Tech stack

| Concern | Choice |
|---|---|
| Runtime / language | .NET 8, C# 12 |
| Web framework | ASP.NET Core Web API (controllers) |
| Persistence | EF Core 8 + SQL Server (code-first migrations) |
| Legacy integration | SOAP 1.1 client (HttpClient + LINQ to XML) for `FindPerson` |
| CSV | CsvHelper |
| Auth | JWT bearer, client-credentials style token endpoint |
| API docs | Swagger / OpenAPI with Authorize support |
| Tests | xUnit + EF Core SQLite in-memory |

## Architecture

Layered (Clean Architecture-style) with the dependency rule pointing inward:

```
Campaign.Api            -> HTTP concerns only: controllers, auth, middleware, Swagger
Campaign.Infrastructure -> EF Core (SQL Server), SOAP adapter, CsvHelper parser
Campaign.Application    -> business rules, use-case services, ports (interfaces), DTOs
Campaign.Domain         -> entities, zero dependencies
```

Why it matters for this task:
- **"Reuse in different CRM solutions"** → consumers see one clean REST contract
  (JWT + JSON + ProblemDetails errors), never SOAP or SQL.
- **"Easy maintenance / future needs"** → the SOAP directory sits behind the
  `ICustomerDirectory` port; swapping it for REST/gRPC later touches one adapter class.
- **Testability** → business rules are tested against a real EF provider (SQLite
  in-memory) without any web server or SQL Server instance.

## Key business rules & where they live

| Rule | Implementation |
|---|---|
| Max 5 rewards / agent / day | `RewardService.CreateAsync` count check (soft-deleted rows excluded via EF global query filter) |
| Customer must exist | Lookup against the SOAP directory before saving |
| No duplicate customer per agent per day | Service check **plus** a filtered unique index in the DB (race-condition safety net) |
| "Mistakes are possible" | `PUT` (fix customer/discount) and `DELETE` (soft delete frees the daily slot, keeps audit trail) |
| Bad rows in the .csv | Row-level errors are reported; good rows import anyway; re-uploads are idempotent |

## Endpoints

| Method | Route | Purpose |
|---|---|---|
| POST | `/api/v1/auth/token` | Exchange client credentials for a JWT |
| GET | `/api/v1/customers/{id}` | Customer lookup (proxies SOAP `FindPerson`) |
| POST | `/api/v1/rewards` | Create reward entry (agent form) |
| GET | `/api/v1/rewards` | List entries (filters: agent, date range) |
| GET | `/api/v1/rewards/{id}` | Get one entry |
| PUT | `/api/v1/rewards/{id}` | Correct a mistake |
| DELETE | `/api/v1/rewards/{id}` | Remove a mistaken entry (soft delete) |
| POST | `/api/v1/purchases/import` | Upload the monthly .csv report |
| GET | `/api/v1/reports/campaign-results` | Merged results + conversion rate |

## Running locally

**Prerequisites:** .NET 8 SDK, and SQL Server reachable at `localhost` with Windows Authentication.
If your instance differs, adjust `ConnectionStrings:Default` in `src/Campaign.Api/appsettings.json`.

The database is created automatically from EF Core migrations on first run in Development —
no manual database setup and no scripts to run.

```bash
dotnet build
dotnet run --project src/Campaign.Api
```

Open `https://localhost:7227/swagger`, call **POST /api/v1/auth/token** with:

```json
{ "clientId": "crm-demo", "clientSecret": "crm-demo-secret" }
```

Click **Authorize**, paste the token, and exercise the API. A sample purchase report
with intentionally broken rows is in `samples/purchases-sample.csv`.
Ready-made requests: `api.http`.

## Tests

```bash
dotnet test
```

Covers: the 5/day limit, soft delete freeing the quota, duplicate prevention,
unknown-customer rejection, discount validation, per-agent limit independence,
and CSV parsing (good rows, bad rows, missing header, currency normalization).

## Deliberate trade-offs

- **Secrets in appsettings.json** — fine for a demo; production: environment variables /
  Azure Key Vault, hashed client secrets, or a real IdP (Entra ID).
- **Daily-limit race window** — the count check plus the filtered unique index covers
  duplicates; a fully concurrency-proof limit would need a serializable transaction or a
  conditional insert. Documented rather than over-engineered.
- **Caching / retries on the SOAP directory** (`IMemoryCache` + Polly) — a straightforward
  next step behind the existing `ICustomerDirectory` port.
- **Pagination** on list endpoints once data volume justifies it.
- **SQLite for unit tests** — a real EF provider so query filters actually execute, but
  provider-specific behaviour (e.g. the filtered index) would need integration tests
  against SQL Server.