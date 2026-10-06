# Hitachi Energy — Stock Replenishment Request System

A reviewer-ready implementation of the **System Development Specialist — Stock Replenishment Request System** assignment.

## What is included

- ASP.NET Core **.NET 10** controller-based REST API
- Entity Framework Core with **InMemory** database — no SQL Server setup required
- Blazor Server UI with **MudBlazor**
- NUnit + NSubstitute unit/service tests
- Seed data available on first startup
- Workflow enforcement:
  - `Draft → Submitted → Approved → Fulfilled`
  - `Submitted → Rejected`
- Priority: Low / Normal / Urgent
- Filtering by status, priority and location
- Server-side pagination
- Validation of request/item data and workflow transitions
- Slow external stock availability simulation using `Task.Delay`
- Background `Channel<Guid>` queue + hosted worker so submission returns immediately with **202 Accepted**
- UI polling of `/stock-validation` so the user sees the eventual external validation result
- Swagger/OpenAPI in Development

## Architecture

```text
src/StockReplenishment.Api
├── Background       # async stock-validation queue + worker
├── Components       # Blazor + MudBlazor UI
├── Controllers      # REST API endpoints
├── Domain           # entities, enums, workflow rules
├── Application      # DTOs, interfaces, application service
├── Infrastructure   # EF Core, seed data, simulated external service
└── Web              # API client used by the Blazor UI

tests/StockReplenishment.Tests
├── ReplenishmentRequestTests.cs
└── ReplenishmentRequestServiceTests.cs
```

The domain owns state-transition rules so controllers cannot accidentally bypass workflow constraints. The background worker owns the slow external operation; the HTTP request never waits for the external service.

## Run locally

### Prerequisites

- .NET 10 SDK
- Visual Studio 2026 / VS Code / Rider (optional)

### Start

```bash
dotnet restore
dotnet build
dotnet run --project src/StockReplenishment.Api
```

Open the URL printed by `dotnet run` (the included launch profile uses `https://localhost:7050`).

Swagger is available at `/swagger` in Development.

### Tests

```bash
dotnet test
```

## Main API endpoints

| Method | Endpoint | Purpose |
|---|---|---|
| GET | `/api/replenishment-requests` | Filter + paginate requests |
| GET | `/api/replenishment-requests/{id}` | Request details |
| POST | `/api/replenishment-requests` | Create draft |
| POST | `/api/replenishment-requests/{id}/submit` | Submit; queues stock validation and returns 202 |
| GET | `/api/replenishment-requests/{id}/stock-validation` | Poll validation status |
| POST | `/api/replenishment-requests/{id}/approve` | Approve after validation passes |
| POST | `/api/replenishment-requests/{id}/reject` | Reject with mandatory reason |
| POST | `/api/replenishment-requests/{id}/fulfill` | Fulfill with quantities per item |

### Example submit response

```http
POST /api/replenishment-requests/{id}/submit
HTTP/1.1 202 Accepted
```

The client can then poll:

```http
GET /api/replenishment-requests/{id}/stock-validation
```

Possible states: `NotStarted`, `Pending`, `Passed`, `Failed`.

## Design decisions worth reviewing

1. **202 Accepted instead of blocking submission** — the assignment explicitly states the external stock check takes several seconds. A background queue avoids tying up an API request/thread and gives the client a clear asynchronous contract.
2. **Domain-level workflow guards** — invalid transitions such as approving a draft or fulfilling an unapproved request are rejected consistently regardless of caller.
3. **Approval requires successful stock validation** — this prevents a reviewer from approving while the external check is still pending or has failed.
4. **Rejection requires a reason** — enforced in both API validation and the domain.
5. **Fulfillment quantities are validated** — every requested item must receive a fulfillment quantity, and no quantity may exceed the requested quantity.
6. **Pagination is server-side** — avoids loading an unbounded request list into the UI.
7. **Failure is observable** — if the background validation throws unexpectedly, the request is marked as failed rather than remaining indefinitely in `Pending`.

## Reviewer walkthrough

1. Start the application — seeded requests appear immediately.
2. Open the seeded Draft request and submit it.
3. Notice that submission returns immediately and the UI shows `Pending`.
4. Wait a few seconds; the UI polls the validation endpoint and changes to `Passed`.
5. Approve it and then enter fulfillment quantities.
6. Try invalid workflow actions to see the API return `409 Conflict`.
7. Create another request with a quantity above `500` to exercise the simulated failed stock-validation path.
8. Use the filters and pagination on the Requests screen.

## GitHub delivery

After reviewing locally:

```bash
git init
git add .
git commit -m "Implement stock replenishment request system"
git branch -M main
git remote add origin <YOUR_GITHUB_REPOSITORY_URL>
git push -u origin main
```

Do not commit secrets, local HTTPS certificates, `bin/`, or `obj/` directories.
