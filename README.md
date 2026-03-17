# POC: Communication with Booking.com

Proof-of-concept for integrating with **Booking.com Messaging API** via the **Connectivity Notification Service (CNS)** — using the webhook-driven flow, not the deprecated `GET /messages/latest` polling.

## Purpose

- Validate the end-to-end flow: CNS webhooks → webhook endpoint → idempotent processing.
- Prove identity mapping (guest, reservation, conversation) and local enrichment.
- Demonstrate a realistic UI showing conversations and their local context.
- Inform a production implementation in the Communication platform.

## Documentation

| Document | Description |
|----------|-------------|
| [booking-cns-messaging-overview.md](booking-cns-messaging-overview.md) | Technical and business limitations, delivery model, retention, identity mapping, and authentication for CNS-based messaging. |

## Solution layout

| Project | Purpose | Port (HTTP) |
|---------|---------|-------------|
| **PocBooking.Api** | POC: webhook receiver, idempotency, enrichment, identity mapping, conversation UI. | 5154 |
| **PocBooking.BookingSimulator** | Simulates Booking.com: Messaging API + CNS webhooks; Razor Pages UI for driving conversations. | 5160 |
| **PocBooking.AppHost** | .NET Aspire host: runs both projects together. | — |

## Message flow

### Inbound — CNS push (new message notification)

A message sent from the Booking.com side triggers a webhook push to the POC API. This is the only way the POC learns about new messages.

```mermaid
sequenceDiagram
    participant Sim as Booking.com / Simulator
    participant Api as PocBooking.Api
    participant DB as SQLite (pocbooking.db)

    Note over Sim,DB: Triggered when a guest or hotel sends a message

    Sim->>Api: POST /api/webhooks/booking/cns
    Note right of Api: {metadata: {uuid, type}, payload: {message_id, sender, conversation, ...}}

    Api->>Api: Validate Bearer JWT
    Api->>DB: SELECT NotificationInbox WHERE uuid = ? OR message_id = ?
    Note right of Api: Idempotency check — silently accept duplicates

    Api->>DB: INSERT NotificationInbox (raw JSON)
    Api->>DB: UPSERT GuestMapping (booking_guest_id → InternalGuestId + GuestName)
    Note right of DB: GuestName generated on first encounter and stored permanently
    Api->>DB: UPSERT ReservationMapping (booking_reservation_id → InternalReservationId + ConfirmationNumber)
    Api->>DB: INSERT ProcessedMessage (links inbox → internal IDs)

    Api-->>Sim: 200 {received: true}
```

### Outbound — reading and replying to conversations

The POC UI fetches conversations from the Booking Messaging API and overlays local identity data from SQLite.

```mermaid
sequenceDiagram
    participant Browser
    participant Api as PocBooking.Api
    participant DB as SQLite (pocbooking.db)
    participant Booking as Booking.com / Simulator

    Note over Browser,Booking: Conversations list

    Browser->>Api: GET /Conversations?propertyId=...
    Api->>Booking: GET /messaging/properties/{id}/conversations
    Booking-->>Api: {data: {conversations: [{conversation_reference, messages, ...}], next_page_id}}
    Api->>DB: Batch lookup by conversation_reference → GuestMapping + ReservationMapping
    DB-->>Api: GuestName, ConfirmationNumber, InternalIds per conversation
    Api-->>Browser: Rendered list (guest name + confirmation # as primary identifiers)

    Note over Browser,Booking: Conversation thread

    Browser->>Api: GET /Conversation?propertyId=...&conversationId=...
    Api->>Booking: GET /messaging/properties/{id}/conversations/{convId}
    Booking-->>Api: {data: {messages, participants}}
    Api->>DB: Lookup by conversation_reference + guest participant IDs
    DB-->>Api: ConversationMapping (guest name, confirmation #, internal IDs)
    Api-->>Browser: Thread with chat bubbles + local mapping card

    Note over Browser,Booking: Sending a reply

    Browser->>Api: POST /Conversation (reply form)
    Api->>Booking: POST /messaging/properties/{id}/conversations/{convId}
    Note right of Booking: {"message": {"content": "..."}}
    Booking-->>Api: {message_id, ok: true}
    Booking-->>Api: POST /api/webhooks/booking/cns (CNS push for the hotel reply)
    Note right of Api: Inbound flow runs again — hotel reply recorded in inbox
    Api-->>Browser: Redirect → conversation page
```

## Tech Stack

- **.NET 10** — both projects are ASP.NET Core minimal Web APIs with Razor Pages.
- **SQLite + EF Core 10** — each project has its own database, migrated automatically on startup.
- **.NET Aspire** — local orchestration via `Aspire.AppHost.Sdk` (no workload install required).

---

## Running the projects

### With Aspire (recommended)

```bash
dotnet run --project src/PocBooking.AppHost
```

- **POC API**: http://localhost:5154
- **Simulator**: http://localhost:5160
- **Dashboard**: URL printed to console on startup (e.g. http://localhost:15300)

If you see *Address already in use* for 5154 or 5160, stop any previous instance of the AppHost, Api, or Simulator (`lsof -i :5154` / `lsof -i :5160`).

### POC API standalone

```bash
dotnet run --project src/PocBooking.Api
```

### Simulator standalone

```bash
dotnet run --project src/PocBooking.BookingSimulator
```

---

## PocBooking.Api

Receives CNS webhooks from Booking.com (or the simulator), enriches them with local identity data, and provides a read-only conversation UI backed by the Booking Messaging API.

### Endpoints

| Method | Path | Description |
|--------|------|-------------|
| `POST` | `/api/webhooks/booking/cns` | CNS webhook receiver. JWT validation when `Booking:Cns:JwtSigningKey` is configured. Idempotent by `metadata.uuid` and `payload.message_id`. |
| `GET` | `/api/health` | SQLite connectivity check. |
| `GET` | `/` | Redirects to `/Index`. |

### Razor Pages UI

| Page | Path | Description |
|------|------|-------------|
| Index | `/Index` | Landing page with links to Conversations and Inbox. |
| Conversations | `/Conversations?propertyId=...` | Lists all conversations for a property. Primary identifiers are the **guest name** and **confirmation number** from local mappings; falls back to Booking.com conversation ID. Paginated. |
| Conversation | `/Conversation?propertyId=...&conversationId=...` | Full conversation thread with chat bubbles. Shows a **local mapping card** (guest name, confirmation number, internal IDs) when the CNS enrichment has run. Reply form sends as property. |
| Inbox | `/Inbox` | Raw CNS webhook inbox: every received `MESSAGING_API_NEW_MESSAGE` notification with its guest name, confirmation number, and Booking.com IDs. |

### Enrichment and identity mapping

When a `MESSAGING_API_NEW_MESSAGE` CNS webhook arrives, `EnrichCnsMessageService` extracts the Booking.com guest and reservation identifiers from the payload and creates or retrieves local mappings:

- **`GuestMapping`** — maps `booking_guest_id` → `InternalGuestId` (UUID) + a generated `GuestName`.
- **`ReservationMapping`** — maps `booking_reservation_id` → `InternalReservationId` (UUID) + a generated `ConfirmationNumber`.
- **`ProcessedMessage`** — links a `NotificationInbox` record to its resolved internal IDs.

Guest names and confirmation numbers are randomly generated on first encounter and stored permanently, so the same guest/reservation always resolves to the same local identity.

### Mapping service

`IConversationMappingService` (in `Mapping/`) encapsulates all DB lookups:

- `GetMappingAsync(ref, guestParticipantIds, ct)` — resolves local identity for a single conversation thread.
- `GetMappingsByRefAsync(refs, ct)` — batch-resolves for the conversations list page.

### SQLite schema (`pocbooking.db`)

| Table | Contents |
|-------|----------|
| `NotificationInbox` | Every received CNS notification (raw JSON, uuid, message_id, type). |
| `GuestMapping` | Booking guest ID → internal UUID + generated guest name. |
| `ReservationMapping` | Booking reservation ID → internal UUID + generated confirmation number. |
| `ProcessedMessage` | Links inbox records to resolved InternalReservationId + InternalGuestId. |

Database is created and migrated automatically on startup.

### POC configuration

| Key | Description |
|-----|-------------|
| `ConnectionStrings:DefaultConnection` | SQLite path (default: `Data Source=pocbooking.db`). |
| `Booking:ApiBaseUrl` | Outbound Messaging API base URL. Use simulator (`http://localhost:5160`) or real Booking.com URL. |
| `Booking:ApiKey` | Optional Bearer token for the Messaging API. |
| `Booking:Cns:JwtSigningKey` | Symmetric key for validating incoming CNS webhook JWTs. |
| `Booking:Cns:JwtIssuer` | Expected `iss` claim in CNS webhook JWTs. |
| `Booking:Cns:JwtAudience` | Expected `aud` claim in CNS webhook JWTs. |
| `Booking:Cns:RequireSignatureValidation` | Set to `true` when a signing key is configured. |

---

## PocBooking.BookingSimulator

Simulates both sides of Booking.com: the Messaging API (REST) and the CNS push delivery. Fully interchangeable with the real Booking.com API by config change in PocBooking.Api.

### Booking-style Messaging API

All endpoints are under `/messaging`. Authentication is `Authorization: Bearer <ApiKey>` (optional; only enforced when `BookingSimulator:ApiKey` is set).

| Method | Path | Description |
|--------|------|-------------|
| `GET` | `/messaging/properties/{propertyId}/conversations` | List conversations (optional `?page_id`). Returns 50 per page. |
| `GET` | `/messaging/properties/{propertyId}/conversations/{conversationId}` | Full conversation thread: all messages + participants. |
| `POST` | `/messaging/properties/{propertyId}/conversations/{conversationId}` | Send a message as property. Body: `{ "message": { "content": "...", "attachment_ids": [] } }`. Fires a CNS webhook to the POC. |
| `GET` | `/messaging/messages/search` | Create a search job. Query: `after`, `before` (ISO 8601), `property_id`, `order_by`. |
| `GET` | `/messaging/messages/search/result/{jobId}` | Fetch messages for a search job (optional `?page_id`). Returns 410 if expired. |

### Simulate endpoints

| Method | Path | Description |
|--------|------|-------------|
| `GET` | `/api/simulate/sample` | Returns a sample `MESSAGING_API_NEW_MESSAGE` payload (JSON). |
| `POST` | `/api/simulate/deliver` | Fires a one-off CNS notification to the POC webhook. Optional body: `{ "notificationUuid": "...", "messageId": "...", "content": "..." }`. |

Example:
```bash
curl -X POST http://localhost:5160/api/simulate/deliver
curl -X POST http://localhost:5160/api/simulate/deliver \
  -H "Content-Type: application/json" \
  -d '{"content":"Custom test message"}'
```

### Razor Pages UI

Open http://localhost:5160 in a browser.

| Page | Description |
|------|-------------|
| Index | Lists all properties; for the selected property shows conversations with linked guest name and last-message preview. |
| New Conversation | Creates a conversation with a configurable `ConversationReference` (reservation number). Optionally create a new named guest participant, pick an existing one, or proceed with no guest linked. |
| Conversation | Full thread with chat bubbles. Send messages as **guest** (uses the conversation's linked guest participant) or **hotel**. Each send fires a CNS webhook to the POC. |

### SQLite schema (`booking-simulator.db`)

| Table | Contents |
|-------|----------|
| `Properties` | Simulated properties (seeded on first run with one property). |
| `Participants` | Guest and hotel participants scoped to a property. |
| `Conversations` | Conversations with `ConversationReference` and optional `GuestParticipantId` FK. |
| `Messages` | All messages with sender and timestamp. |
| `MessageSearchJobs` | Async search jobs (48 h TTL). |

Database is seeded on first run with one property, one hotel participant, and one guest participant with a welcome conversation.

### Simulator configuration

| Key | Description |
|-----|-------------|
| `ConnectionStrings:DefaultConnection` | SQLite path (default: `Data Source=booking-simulator.db`). |
| `BookingSimulator:PocWebhookBaseUrl` | POC base URL for CNS delivery (e.g. `http://localhost:5154`). |
| `BookingSimulator:SendWebhookOnNewMessage` | Fire CNS webhook after each new message (`true` by default). |
| `BookingSimulator:PocBearerToken` | Opaque Bearer token sent to POC webhook when JWT config is not set. |
| `BookingSimulator:JwtSigningKey` | Symmetric key for signing CNS webhook JWTs (match with `Booking:Cns:JwtSigningKey` in POC). |
| `BookingSimulator:JwtIssuer` | JWT `iss` claim (match with `Booking:Cns:JwtIssuer` in POC). |
| `BookingSimulator:JwtAudience` | JWT `aud` claim (match with `Booking:Cns:JwtAudience` in POC). |
| `BookingSimulator:ApiKey` | Optional. If set, `/messaging/*` requires `Authorization: Bearer <ApiKey>`. |

---

## Switching from simulator to real Booking.com

The POC API is designed so that only configuration changes are needed to switch from the simulator to the real Booking.com API.

| Concern | Simulator | Real Booking.com |
|---------|-----------|------------------|
| **Messaging API base URL** | `Booking:ApiBaseUrl` = `http://localhost:5160` | `Booking:ApiBaseUrl` = Booking's API base URL |
| **Messaging API auth** | `Booking:ApiKey` matching `BookingSimulator:ApiKey` (optional) | `Booking:ApiKey` = real Booking.com API key |
| **CNS webhook JWT** | `Booking:Cns:JwtSigningKey/Issuer/Audience` matching simulator config | `Booking:Cns:JwtIssuer/Audience` = Booking's values; extend validator for JWKS if needed |
| **Webhook accessibility** | Both services on localhost | POC's `/api/webhooks/booking/cns` must be publicly reachable |

No code changes are required. All three Messaging API endpoints consumed by the POC (`GET` conversations, `GET` conversation thread, `POST` message) are faithfully simulated with the exact same JSON wire format as the real Booking.com API.

---

## References

- [Booking.com Connectivity Notification Service](https://developers.booking.com/connectivity/docs/notification-service/notification-service-overview)
- [Booking.com Messaging API](https://developers.booking.com/connectivity/docs/messaging-api/understanding-the-messaging-api)
- [CNS Authentication](https://developers.booking.com/connectivity/docs/notification-service/authentication)
