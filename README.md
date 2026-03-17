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

## Tech Stack

- **.NET 10** — Web API (minimal API)
- **SQLite** — Persistence via Entity Framework Core 10 (single file `pocbooking.db`, created on first run)

### Run the API

```bash
cd src/PocBooking.Api
dotnet run
```

Or from repo root:

```bash
dotnet run --project src/PocBooking.Api
```

- Root: `GET /` — service info
- Health: `GET /api/health` — checks SQLite connectivity
- **Booking CNS webhook**: `POST /api/webhooks/booking/cns` — receives Booking.com Connectivity Notification Service payloads (e.g. `MESSAGING_API_NEW_MESSAGE`). Idempotent by `metadata.uuid` and `payload.message_id`; stores raw JSON in `NotificationInbox`. In production, configure CNS to call this URL and send `Authorization: Bearer <token>`. For local testing, set `Booking:Cns:RequireBearer` to `false` in `appsettings.Development.json`.

Example (local, no auth):

```bash
curl -X POST http://localhost:5xxx/api/webhooks/booking/cns \
  -H "Content-Type: application/json" \
  -d '{"metadata":{"uuid":"550e8400-e29b-41d4-a716-446655440000","type":"MESSAGING_API_NEW_MESSAGE","payloadVersion":"1.0"},"payload":{"message_id":"4ad42260-e0aa-11ea-b1cb-0975761ce091","message_type":"free_text","timestamp":"2020-08-17T16:54:19.270Z","content":"Hello","sender":{"participant_id":"9f6be5fd-b3a8-5691-9cf9-9ab6c6217327","metadata":{"name":"Test Property","participant_type":"hotel"}},"conversation":{"property_id":"1383087","conversation_id":"f3a9c29d-480d-5f5b-a6c0-65451e335353","conversation_reference":"3812391309","conversation_type":"reservation"}}}'
```

The SQLite database is created automatically on startup (`EnsureCreated`). Schema includes a placeholder table for notification idempotency (see `Data/NotificationInbox.cs`).

## References

- [Booking.com Connectivity Notification Service](https://developers.booking.com/connectivity/docs/notification-service/notification-service-overview)
- [Booking.com Messaging API](https://developers.booking.com/connectivity/docs/messaging-api/understanding-the-messaging-api)
- [CNS Authentication](https://developers.booking.com/connectivity/docs/notification-service/authentication)
