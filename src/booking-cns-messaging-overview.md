# Booking.com Messaging via CNS (New API) - Technical & Business Limitations Overview

## Executive Summary

Using Booking.com's Connectivity Notification Service (CNS) with `MESSAGING_API_NEW_MESSAGE` is the right direction (and aligns with Booking's deprecation of `GET /messages/latest` as the primary ingestion flow).  
Source: [Managing messages - deprecation notice](https://developers.booking.com/connectivity/docs/messaging-api/managing-messages), [Managing notifications - newly created messages](https://developers.booking.com/connectivity/docs/notification-service/managing-notifications#newly-created-messages--messaging_api_new_message)
The integration is robust if treated as:

- Webhook-driven ingestion
- Idempotent processing
- Replay/reconciliation capable

The biggest risks are:

- Assuming perfect/exactly-once delivery
- Underestimating retention/backfill boundaries
- Weak cross-system mapping for reservations and guest profiles

---

## 1) Technical Limitations

### 1.1 CNS is a notification trigger, not a full state synchronization layer

Booking positions CNS as a notification platform that signals updates and expects follow-up actions on partner side.  
For messaging, the notification payload is rich, but you still need recovery logic via Messaging Search APIs.  
Source: [Notification Service overview](https://developers.booking.com/connectivity/docs/notification-service/notification-service-overview), [Managing notifications - Recovering missed notifications](https://developers.booking.com/connectivity/docs/notification-service/managing-notifications#recovering-missed-notifications), [Searching messages](https://developers.booking.com/connectivity/docs/messaging-api/searching-messages)

### 1.2 Delivery semantics require defensive design

Booking docs do not provide an explicit exactly-once guarantee for CNS messaging notifications.  
Design implications:

- Treat processing as at-least-once compatible
- Implement idempotency and deduplication
- Handle out-of-order arrivals safely

Source: [Managing notifications - one notification per message](https://developers.booking.com/connectivity/docs/notification-service/managing-notifications#newly-created-messages--messaging_api_new_message), [Managing notifications - Recovering missed notifications](https://developers.booking.com/connectivity/docs/notification-service/managing-notifications#recovering-missed-notifications), [Managing messages - duplicate handling context](https://developers.booking.com/connectivity/docs/messaging-api/managing-messages)

### 1.3 API shape/version differences matter

Use `Accept-Version: 1.2` consistently where applicable:

- Includes richer fields (`message_type`, `attributes`)
- Supports self-service event payloads

Also normalize participant type values across endpoints because legacy and CNS payload vocabularies differ.  
Source: [Understanding the Messaging API - versioning](https://developers.booking.com/connectivity/docs/messaging-api/understanding-the-messaging-api#versioning), [Searching messages - v1.0 vs v1.2](https://developers.booking.com/connectivity/docs/messaging-api/searching-messages#comparing-response-changes-between-v1.0-and-v1.2), [Managing notifications - participant_type note](https://developers.booking.com/connectivity/docs/notification-service/managing-notifications#newly-created-messages--messaging_api_new_message), [Managing messages - response elements](https://developers.booking.com/connectivity/docs/messaging-api/managing-messages#response-body-elements)

### 1.4 Conversation retrieval is partial and paged

`GET /properties/{property_id}/conversations` returns conversations with latest message, not full history, and uses pagination.  
Source: [Managing conversations - Retrieving all conversations per property](https://developers.booking.com/connectivity/docs/messaging-api/managing-conversations#retrieving-all-conversations-per-property)

### 1.5 Content may be transformed by Booking security settings

Unapproved links can be removed from message content due to Booking messaging security settings.  
Do not assume all original links are preserved.  
Source: [Managing conversations - security link setting note](https://developers.booking.com/connectivity/docs/messaging-api/managing-conversations), [Managing messages - security link setting note](https://developers.booking.com/connectivity/docs/messaging-api/managing-messages)

---

## 2) Business & Operational Limitations

### 2.1 Messaging time windows are policy-bound

Booking constraints influence UX and support flows:

- Property can message from booking creation until 7 days after cancellation/checkout
- If guest messages, property receives an additional 14-day reply window
- Guests can message until 66 days after checkout

Source: [Understanding the Messaging API - When can properties send messages](https://developers.booking.com/connectivity/docs/messaging-api/understanding-the-messaging-api#when-can-properties-send-messages-to-guests)

### 2.2 Self-service request handling is not uniform

Some self-service request types must be resolved in Extranet (not API reply flow), and some API replies can change what is possible in Extranet afterward.  
Source: [Understanding the Messaging API - self-service limitations](https://developers.booking.com/connectivity/docs/messaging-api/understanding-the-messaging-api#limitations-for-self-service-requests)

### 2.3 Request-to-Book (RtB) behavior introduces process complexity

Pre-reservation messaging exists behind feature flags and has limitations (including testing/feature behavior differences).  
You need explicit support guidance and fallback handling for pre-reservation threads.  
Source: [Pre-reservation messaging](https://developers.booking.com/connectivity/docs/request-to-book/pre-reservation-messaging)

### 2.4 Operational readiness is mandatory

You need runbooks for:

- Authentication failures
- Delivery gaps
- Replay/backfill execution
- Unmatched reservation/profile mapping

---

## 3) Message Delivery Model (Recommended)

### 3.1 Normal flow

1. CNS sends `MESSAGING_API_NEW_MESSAGE` webhook. Source: [Managing notifications - newly created messages](https://developers.booking.com/connectivity/docs/notification-service/managing-notifications#newly-created-messages--messaging_api_new_message)
2. Validate auth token + schema. Source: [CNS authentication](https://developers.booking.com/connectivity/docs/notification-service/authentication)
3. Idempotency check.
4. Persist raw payload + normalized message.
5. Map property/reservation/profile. Source: [Managing notifications - message payload schema](https://developers.booking.com/connectivity/docs/notification-service/managing-notifications#newly-created-messages--messaging_api_new_message)
6. Upsert message thread state.

### 3.2 Idempotency and deduplication keys

Use layered keys:

- Notification-level key: `metadata.uuid` (from CNS metadata). Source: [Managing notifications - metadata.uuid](https://developers.booking.com/connectivity/docs/notification-service/managing-notifications#common-notification-metadata)
- Message-level key: `payload.message_id` (from messaging payload). Source: [Managing notifications - `message_id`](https://developers.booking.com/connectivity/docs/notification-service/managing-notifications#newly-created-messages--messaging_api_new_message)

Store both and enforce uniqueness at DB/inbox layer.

### 3.3 Out-of-order and replay-safe processing

- Upserts must be timestamp-safe and order-agnostic.
- Replays must use the same ingestion pipeline as live webhooks.
- Never rely on single-pass processing.

Source: [Managing notifications - recovering missed notifications](https://developers.booking.com/connectivity/docs/notification-service/managing-notifications#recovering-missed-notifications), [Searching messages - fallback nature and async jobs](https://developers.booking.com/connectivity/docs/messaging-api/searching-messages), [Managing notifications - one notification per message scope](https://developers.booking.com/connectivity/docs/notification-service/managing-notifications#newly-created-messages--messaging_api_new_message)

### 3.4 Missed notification recovery

Use Messaging Search endpoints only as fallback/reconciliation (not primary retrieval path).  
Source: [Managing notifications - recovering missed notifications](https://developers.booking.com/connectivity/docs/notification-service/managing-notifications#recovering-missed-notifications), [Searching messages - intensive/slow warning](https://developers.booking.com/connectivity/docs/messaging-api/searching-messages)

---

## 4) Message Retention & Recovery Constraints

From Booking Messaging Search:

- Maximum query date range: **90 days**
- Query results are asynchronous (`job_id`)
- Ready query results expire after **48 hours**

Source: [Searching messages - query parameters and expiry](https://developers.booking.com/connectivity/docs/messaging-api/searching-messages)

Operational implication:

- Detect gaps early
- Backfill quickly
- If outage detection is delayed too long, full recovery risk increases

---

## 5) Reservation/Profile Mapping Strategy

## 5.1 Canonical fields to persist

Persist at minimum:

- `conversation.property_id`
- `conversation.conversation_id`
- `conversation.conversation_reference`
- `payload.message_id`
- `sender.participant_id`
- `sender.metadata.participant_type`
- CNS `metadata.uuid`

Source: [Managing notifications - message payload schema](https://developers.booking.com/connectivity/docs/notification-service/managing-notifications#newly-created-messages--messaging_api_new_message), [Managing notifications - common metadata](https://developers.booking.com/connectivity/docs/notification-service/managing-notifications#common-notification-metadata)

### 5.2 Property mapping (strong deterministic)

- Map Booking `property_id` to internal property id through pre-provisioned external-id mapping.
- This mapping must be complete before production rollout.

Source field: [Managing notifications - `conversation.property_id`](https://developers.booking.com/connectivity/docs/notification-service/managing-notifications#newly-created-messages--messaging_api_new_message)

### 5.3 Reservation mapping (usually deterministic)

When `conversation_type = reservation`:

- `conversation_reference` is the Booking reservation id reference
- Map to internal reservation by Booking external reservation id

Source: [Managing notifications - `conversation_type` and `conversation_reference`](https://developers.booking.com/connectivity/docs/notification-service/managing-notifications#newly-created-messages--messaging_api_new_message), [Searching messages - response parameters](https://developers.booking.com/connectivity/docs/messaging-api/searching-messages#response-body-parameters)

### 5.4 RtB edge case (non-reservation phase)

When `conversation_type = request_to_book`:

- `conversation_reference` points to Booking Request Object id pre-confirmation
- Store thread as pre-reservation
- Re-link to reservation once booking is confirmed and identifier becomes available

Source: [Managing notifications - conversation_reference note](https://developers.booking.com/connectivity/docs/notification-service/managing-notifications#newly-created-messages--messaging_api_new_message), [Pre-reservation messaging](https://developers.booking.com/connectivity/docs/request-to-book/pre-reservation-messaging)

### 5.5 Guest/profile mapping (probabilistic unless already linked)

Messaging payload does not guarantee a universal guest master id equivalent to your profile id.

Recommended approach:

- Resolve guest primarily through reservation linkage
- Keep Booking `participant_id` as channel identity
- Perform delayed profile resolution/enrichment where needed
- Maintain unresolved queue + reconciliation jobs

Source fields available in messaging payload: [Managing notifications - sender object](https://developers.booking.com/connectivity/docs/notification-service/managing-notifications#newly-created-messages--messaging_api_new_message), [Searching messages - sender fields](https://developers.booking.com/connectivity/docs/messaging-api/searching-messages#response-body-parameters)

---

## 6) Authentication Model

## 6.1 CNS -> Our webhook (Booking calls us)

Booking CNS authentication setup:

- We provide token endpoint + webhook endpoint in Provider Portal
- CNS requests JWT from our auth service using OAuth2 client credentials (`grant_type=client_credentials`)
- CNS delivers webhook with `Authorization: Bearer <jwt>`

Our responsibilities:

- Validate JWT signature/claims/expiry
- Return `401` for invalid token
- Enforce TLS 1.2+

Source: [CNS authentication](https://developers.booking.com/connectivity/docs/notification-service/authentication), [Configuring notification subscriptions](https://developers.booking.com/connectivity/docs/notification-service/configuring-notification-subscriptions), [Notification Service getting started](https://developers.booking.com/connectivity/docs/notification-service/notification-service-getting-started)

### 6.2 Our services -> Booking APIs (we call Booking)

For Booking Connectivity APIs:

- Obtain access token via Booking token exchange endpoint
- Token TTL: 1 hour
- Token generation limit: 30 per hour per machine account

Source: [Token-based authentication](https://developers.booking.com/connectivity/docs/token-based-authentication)

Implementation requirements:

- Token cache and proactive refresh
- Handle `401` with refresh-and-retry logic
- Avoid token request storms

---

## 7) Recommended Guardrails

- Inbox table for raw notifications (`metadata.uuid` unique)
- Message table with `message_id` uniqueness
- Unified ingestion pipeline for live + replay paths
- Periodic reconciliation jobs for mapping and gaps
- Dashboards/alerts for:
  - webhook intake drop
  - auth failures
  - dedup rate anomalies
  - unresolved mapping backlog
  - replay lag

---

## 8) Go-Live Checklist

- CNS subscription enabled for `MESSAGING_API_NEW_MESSAGE`. Source: [Managing notifications](https://developers.booking.com/connectivity/docs/notification-service/managing-notifications)
- Token endpoint and webhook endpoint configured and validated in Provider Portal. Source: [Configuring notification subscriptions](https://developers.booking.com/connectivity/docs/notification-service/configuring-notification-subscriptions)
- Idempotency and out-of-order tests passed
- Replay drill completed (simulated outage + backfill). Source: [Managing notifications - recovering missed notifications](https://developers.booking.com/connectivity/docs/notification-service/managing-notifications#recovering-missed-notifications), [Searching messages](https://developers.booking.com/connectivity/docs/messaging-api/searching-messages)
- Property external-id mapping completeness confirmed
- Reservation mapping success KPI defined and validated
- Unresolved guest/profile flow in place (queue + retries + support visibility)
- Monitoring + alerting + incident runbook ready

---

## Source References

- [CNS managing notifications](https://developers.booking.com/connectivity/docs/notification-service/managing-notifications)
- [CNS authentication setup](https://developers.booking.com/connectivity/docs/notification-service/authentication)
- [Messaging API understanding](https://developers.booking.com/connectivity/docs/messaging-api/understanding-the-messaging-api)
- [Messaging search/backfill](https://developers.booking.com/connectivity/docs/messaging-api/searching-messages)
- [Messaging conversations](https://developers.booking.com/connectivity/docs/messaging-api/managing-conversations)
- [Token-based auth for partner->Booking API calls](https://developers.booking.com/connectivity/docs/token-based-authentication)
