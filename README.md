# POC: Communication with Booking.com

Proof-of-concept repository for integrating with **Booking.com Messaging API** via the **Connectivity Notification Service (CNS)** — using the new webhook-driven flow, not the deprecated `GET /messages/latest` polling.

## Purpose

- Validate end-to-end flow: CNS webhooks → our webhook endpoint → idempotent processing.
- Prove identity mapping (property, reservation, conversation) and recovery via Messaging Search where needed.
- Inform a production implementation in the Communication platform.

## Documentation

| Document | Description |
|----------|-------------|
| [booking-cns-messaging-overview.md](booking-cns-messaging-overview.md) | Technical and business limitations, delivery model, retention, identity mapping, and authentication for CNS-based messaging. |

## Scope (POC)

- Receive `MESSAGING_API_NEW_MESSAGE` notifications (CNS → our endpoint).
- Validate and persist notifications; implement idempotency (e.g. `metadata.uuid`, `message_id`).
- Map Booking.com identifiers to internal concepts where applicable.
- Optional: call Booking Messaging API (e.g. send reply, search) using token-based auth.

## Solution layout

| Project | Purpose | Port (HTTP) |
|---------|---------|-------------|
| **PocBooking.Api** | Your POC: webhook receiver, persistence, idempotency. | 5154 |
| **PocBooking.BookingSimulator** | Simulates Booking.com CNS: sends notifications to the POC. | 5060 |

## Tech Stack

- **.NET 10** — Both projects are minimal Web APIs.
- **SQLite** — Used only by **PocBooking.Api** (EF Core 10; `pocbooking.db` created on first run).

### Run the POC (Api)

```bash
dotnet run --project src/PocBooking.Api
```

- Root: `GET /` — service info
- Health: `GET /api/health` — SQLite connectivity
- **Booking CNS webhook**: `POST /api/webhooks/booking/cns` — receives CNS payloads; idempotent by `metadata.uuid` and `payload.message_id`; stores raw JSON in `NotificationInbox`. For local testing, set `Booking:Cns:RequireBearer` to `false` in `appsettings.Development.json`.

### Run the Booking.com simulator

Start the POC first, then:

```bash
dotnet run --project src/PocBooking.BookingSimulator
```

Simulator endpoints:

- `GET /` — service info
- `GET /api/simulate/sample` — returns a sample `MESSAGING_API_NEW_MESSAGE` JSON (for copy/paste or inspection)
- `POST /api/simulate/deliver` — builds a notification and **POSTs it to the POC webhook** (simulating CNS). Optional JSON body: `{ "notificationUuid": "...", "messageId": "...", "content": "..." }`. Response includes the POC’s status code and response body.

Config (simulator): `BookingSimulator:PocWebhookBaseUrl` (default `http://localhost:5154`), optional `PocBearerToken`.

Example: trigger a simulated delivery (POC and Simulator both running):

```bash
curl -X POST http://localhost:5060/api/simulate/deliver
# or with overrides:
curl -X POST http://localhost:5060/api/simulate/deliver -H "Content-Type: application/json" -d '{"content":"Custom message"}'
```

The POC’s SQLite database is created automatically on startup. Schema includes `NotificationInbox` for idempotency (see `PocBooking.Api/Data/NotificationInbox.cs`).

## References

- [Booking.com Connectivity Notification Service](https://developers.booking.com/connectivity/docs/notification-service/notification-service-overview)
- [Booking.com Messaging API](https://developers.booking.com/connectivity/docs/messaging-api/understanding-the-messaging-api)
- [CNS Authentication](https://developers.booking.com/connectivity/docs/notification-service/authentication)
