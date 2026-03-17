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

To be decided (e.g. .NET minimal API, or small service aligned with Communication stack).

## References

- [Booking.com Connectivity Notification Service](https://developers.booking.com/connectivity/docs/notification-service/notification-service-overview)
- [Booking.com Messaging API](https://developers.booking.com/connectivity/docs/messaging-api/understanding-the-messaging-api)
- [CNS Authentication](https://developers.booking.com/connectivity/docs/notification-service/authentication)
