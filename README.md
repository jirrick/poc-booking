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

- **.NET 10** — POC Api is a minimal Web API; Simulator is a Web API with Razor Pages UI.
- **SQLite** — **PocBooking.Api**: `pocbooking.db` (EF Core 10). **PocBooking.BookingSimulator**: `booking-simulator.db` (EF Core 10); stores properties, conversations, messages, participants; seeded on first run.

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

- **Web UI (root)**: Open `http://localhost:5060/` in a browser to list properties and conversations, open a thread, and send messages (as guest or property). Sending a message persists it and triggers a CNS webhook to the POC (when `SendWebhookOnNewMessage` is true).
- **API info**: `GET /api` — service info.

**Booking-style API** (base path `/messaging`; optional `Accept-Version: 1.2`):

| Method | Path | Purpose |
|--------|------|---------|
| GET | `/messaging/properties/{propertyId}/conversations` | List conversations (optional `page_id`). |
| GET | `/messaging/properties/{propertyId}/conversations/{conversationId}` | Get one conversation with full message list. |
| POST | `/messaging/properties/{propertyId}/conversations/{conversationId}` | Create a message. Body: `{ "message": { "content": "...", "attachment_ids": [] } }`. Triggers webhook to POC. |
| GET | `/messaging/messages/search` | Create search job. Query: `after`, `before` (ISO8601), `property_id`, `order_by`. |
| GET | `/messaging/messages/search/result/{jobId}` | Get messages for a job (optional `page_id`). |

**Simulate endpoints** (one-off delivery, no DB):

- `GET /api/simulate/sample` — returns a sample `MESSAGING_API_NEW_MESSAGE` JSON.
- `POST /api/simulate/deliver` — builds a notification and POSTs it to the POC webhook via the shared webhook sender. Optional body: `{ "notificationUuid": "...", "messageId": "...", "content": "..." }`.

**Simulator configuration** (appsettings; no UI for settings):

| Key | Description |
|-----|-------------|
| `ConnectionStrings:DefaultConnection` | SQLite path (e.g. `Data Source=booking-simulator.db`). |
| `BookingSimulator:PocWebhookBaseUrl` | POC base URL (e.g. `http://localhost:5154`). |
| `BookingSimulator:PocBearerToken` | Optional Bearer token when calling the POC webhook. |
| `BookingSimulator:SendWebhookOnNewMessage` | If `true` (default), send webhook after each new message (API or UI). |
| `BookingSimulator:ApiKey` | Optional. If set, `/messaging/*` requests must send `Authorization: Bearer <ApiKey>`. |

Example: trigger a simulated delivery (POC and Simulator both running):

```bash
curl -X POST http://localhost:5060/api/simulate/deliver
curl -X POST http://localhost:5060/api/simulate/deliver -H "Content-Type: application/json" -d '{"content":"Custom message"}'
```

The POC’s SQLite database is created automatically on startup. Schema includes `NotificationInbox` for idempotency (see `PocBooking.Api/Data/NotificationInbox.cs`). The simulator’s database is created and seeded (one property, participants, one conversation with a welcome message) on first run.

## References

- [Booking.com Connectivity Notification Service](https://developers.booking.com/connectivity/docs/notification-service/notification-service-overview)
- [Booking.com Messaging API](https://developers.booking.com/connectivity/docs/messaging-api/understanding-the-messaging-api)
- [CNS Authentication](https://developers.booking.com/connectivity/docs/notification-service/authentication)
