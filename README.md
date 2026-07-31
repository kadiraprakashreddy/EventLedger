Event Ledger
==============

Two microservices for ingesting financial transaction events: an `EventGateway` (public) and an `AccountService` (internal), talking over synchronous REST.

Architecture
============


Client ──> EventGateway.Api ──> AccountService.Api
           (public)              (internal)


- **EventGateway** — validates and stores incoming events, enforces idempotency on `eventId`, forwards each transaction to AccountService, and serves event lookups. Own SQLite DB (`eventgateway.db`).
- **AccountService** — owns account balances and the transaction ledger. Only called by the Gateway. Own SQLite DB (`accountservice.db`).

Both are Clean Architecture (Domain → Application → Infrastructure → Api) with MediatR. No shared DB or in-process state — the only contract is HTTP + an `X-Trace-Id` header.

Prerequisites
============

- .NET 9 SDK
- Docker Desktop (optional, for Docker Compose)

Running
=======

**Docker Compose:**

docker compose up --build

- EventGateway: `http://localhost:5006`
- AccountService: `http://localhost:5005`

**Manual** (two terminals, from repo root):
```
dotnet run --project src/AccountService/AccountService.Api
dotnet run --project src/EventGateway/EventGateway.Api
```
Defaults to AccountService on `:5005`, EventGateway on `:5154`. Gateway's `appsettings.json` already points at `:5005`, so no config change needed with default ports.

## API

EventGateway API
Base URL: http://localhost:5154 (local)

Endpoints
===============
POST /events
=============
Submit a new transaction event to the system.

Request body: JSON event (e.g. eventId, accountId, type, amount, currency, eventTimestamp).

Behavior:

Saves the event locally.

Calls AccountService to apply the transaction.

If AccountService is down, returns 503 but still persists the event (graceful degradation).

Idempotent by eventId: duplicate posts return the original result with 200 OK and no state change.

GET /events/{id}
================
Retrieve a single event by its eventId.

Returns the stored event details.

Reads only from Gateway’s local store; unaffected by AccountService availability.

GET /events?account={accountId}
==================================
List all events for a given account in chronological order.

Query param: account = accountId.

Sorted by eventTimestamp, not by insertion order (out-of-order tolerance).

GET /health
=============
Health check endpoint.

Verifies database connectivity.

Used by load balancers / orchestration platforms.

GET /metrics
=================
Basic metrics endpoint.

Returns request count and average latency per endpoint.

Useful for simple observability without a full metrics stack.

Example Request
===============
bash
curl -X POST "http://localhost:5006/events" ^
  -H "Content-Type: application/json" ^
  -d "{\"eventId\":\"evt-001\",\"accountId\":\"acct-123\",\"type\":\"CREDIT\",\"amount\":150.00,\"currency\":\"USD\",\"eventTimestamp\":\"2026-05-15T14:02:11Z\"}"
(On Linux/macOS, use single quotes and \ line continuations as in your original example.)

AccountService API
=================
Base URL: e.g. http://localhost:5007

Endpoints
==================
POST /accounts/{accountId}/transactions
Apply a transaction to an account.

Called by EventGateway when an event is submitted.

Idempotent by eventId (unique index + in-memory check).

Duplicate transaction with same eventId → 200 OK with original result, no balance change.

GET /accounts/{accountId}/balance
==================================
Get the current balance for an account.

Balance = sum of all credits − sum of all debits.

Commutative by design, so out-of-order event arrival doesn’t affect correctness.

GET /accounts/{accountId}
=========================
Get account details plus recent transactions.

Returns account metadata and a list of recent events/transactions.

GET /health
=============
Health check endpoint.

Checks DB connectivity.

GET /metrics
=================   
Metrics endpoint.

Returns request count and average latency per endpoint.

Running the Tests
From the solution root:

bash
dotnet test EventLedger.sln
Test projects
AccountService.UnitTests

Domain logic:
==============

Balance calculations (credits − debits).

Idempotency for duplicate eventId.

Handling out-of-order events.

Input validation.

EventGateway.UnitTests

Similar domain-level tests for Gateway’s logic (event handling, validation, idempotency).

AccountService.IntegrationTests

HTTP-level tests:

Idempotent behavior of POST /accounts/{id}/transactions.

Validation error responses.

EventGateway.IntegrationTests

End-to-end flow:

Gateway → AccountService integration.

Resiliency:
================

AccountService down → Gateway returns 503 on POST /events but still saves events.

GET /events/* still works when AccountService is down.

Trace-ID propagation from Gateway to AccountService.

Each integration test uses its own temporary SQLite file to avoid interference between test runs.

Key Design Decisions (Developer Notes)
Idempotency
Both services use eventId as the idempotency key.

Implemented with:

A unique index on eventId in the database.

An in-memory check before writing.

Behavior:

First submission: processes normally.

Duplicate submission: returns 200 OK with the original result; no additional state change or balance update.

Out-of-Order Tolerance
Balance is defined as a simple commutative sum:

Balance
=

credits
==========

debits
=======
Balance=credits−debits
========================
This means the order in which events arrive does not affect the final balance.

Event listings are sorted by eventTimestamp, not by insertion time, so clients see a consistent chronological view even if events arrive out of order.

Resiliency: Circuit Breaker (Polly)
EventGateway calls AccountService through a Polly circuit breaker:

Configured to open after 3 consecutive failures.

Stays open for 30 seconds before allowing a trial request.

Each call has a 5-second timeout.

Rationale:

If AccountService is unreachable, it’s more likely a sustained outage than a momentary blip.

A circuit breaker fails fast instead of repeatedly retrying and adding load to a failing service.

Graceful Degradation
AccountService down:

POST /events on Gateway:

Saves event locally.

Returns 503 Service Unavailable.

GET /events/{id} and GET /events?account=...

Still work; they read from Gateway’s local store only.

AccountService reachable but rejecting (e.g. validation failure, internal error):

Gateway returns 502 Bad Gateway to indicate the downstream service responded with an error.

Observability
Logging:

Uses Serilog with JSON output.

Logs written to:

Console (for container logs).

Files: logs/<service>-<date>.json.

Every log line includes a trace ID.

Trace ID propagation:

Gateway generates an X-Trace-Id for each incoming request.

This header is propagated to AccountService on downstream calls.

Enables end-to-end correlation of logs across services.

Health checks:

/health on both services verifies database connectivity.

Suitable for Kubernetes/AKS health probes or load balancer checks.

Metrics:

/metrics exposes:

Request count per endpoint.

Average latency per endpoint.

Simple, lightweight observability without requiring Prometheus or similar.

If you want, I can next turn this into a polished Markdown README file (with sections like “Architecture Overview”, “Running Locally”, “Deployment Notes”, etc.) tailored for a senior .NET/Azure interview-style repo.


Test cases from Bash:
===================

 Health & Metrics
======================
curl http://localhost:5006/health
curl http://localhost:5005/health
curl http://localhost:5006/metrics
curl http://localhost:5005/metrics

Postive test cases:
==========================

curl -X POST "http://localhost:5006/events" -H "Content-Type: application/json" \
  -d '{"eventId":"t01","accountId":"acct-A","type":"CREDIT","amount":500,"currency":"USD","eventTimestamp":"2026-07-30T08:00:00Z"}'

curl -X POST "http://localhost:5006/events" -H "Content-Type: application/json" \
  -d '{"eventId":"t02","accountId":"acct-A","type":"DEBIT","amount":150,"currency":"USD","eventTimestamp":"2026-07-30T08:05:00Z"}'

curl http://localhost:5006/events/t01
curl "http://localhost:5006/events?account=acct-A"
curl http://localhost:5005/accounts/acct-A/balance
curl http://localhost:5005/accounts/acct-A

Validation & Error Handling
============================
# negative amount
curl -X POST "http://localhost:5006/events" -H "Content-Type: application/json" \
  -d '{"eventId":"t06","accountId":"acct-D","type":"CREDIT","amount":-10,"currency":"USD","eventTimestamp":"2026-07-30T09:00:00Z"}'

# zero amount
curl -X POST "http://localhost:5006/events" -H "Content-Type: application/json" \
  -d '{"eventId":"t07","accountId":"acct-D","type":"CREDIT","amount":0,"currency":"USD","eventTimestamp":"2026-07-30T09:00:00Z"}'

# missing required field (currency)
curl -X POST "http://localhost:5006/events" -H "Content-Type: application/json" \
  -d '{"eventId":"t08","accountId":"acct-D","type":"CREDIT","amount":10,"eventTimestamp":"2026-07-30T09:00:00Z"}'

# invalid type
curl -X POST "http://localhost:5006/events" -H "Content-Type: application/json" \
  -d '{"eventId":"t09","accountId":"acct-D","type":"TRANSFER","amount":10,"currency":"USD","eventTimestamp":"2026-07-30T09:00:00Z"}'

# malformed timestamp
curl -X POST "http://localhost:5006/events" -H "Content-Type: application/json" \
  -d '{"eventId":"t10","accountId":"acct-D","type":"CREDIT","amount":10,"currency":"USD","eventTimestamp":"not-a-date"}'


  trace
  ==========================

  curl -i -X POST "http://localhost:5006/events" -H "Content-Type: application/json" -H "X-Trace-Id: my-custom-trace-999" \
  -d '{"eventId":"t11","accountId":"acct-E","type":"CREDIT","amount":25,"currency":"USD","eventTimestamp":"2026-07-30T09:00:00Z"}'

  Circuit Breaker / Resiliency
  ==========================

  curl -w "\n%{time_total}s\n" -X POST "http://localhost:5006/events" -H "Content-Type: application/json" \
  -d '{"eventId":"t12","accountId":"acct-F","type":"CREDIT","amount":25,"currency":"USD","eventTimestamp":"2026-07-30T09:00:00Z"}'

curl -w "\n%{time_total}s\n" -X POST "http://localhost:5006/events" -H "Content-Type: application/json" \
  -d '{"eventId":"t13","accountId":"acct-F","type":"CREDIT","amount":25,"currency":"USD","eventTimestamp":"2026-07-30T09:01:00Z"}'

curl -w "\n%{time_total}s\n" -X POST "http://localhost:5006/events" -H "Content-Type: application/json" \
  -d '{"eventId":"t14","accountId":"acct-F","type":"CREDIT","amount":25,"currency":"USD","eventTimestamp":"2026-07-30T09:02:00Z"}'

curl -w "\n%{time_total}s\n" -X POST "http://localhost:5006/events" -H "Content-Type: application/json" \
  -d '{"eventId":"t15","accountId":"acct-F","type":"CREDIT","amount":25,"currency":"USD","eventTimestamp":"2026-07-30T09:03:00Z"}'
