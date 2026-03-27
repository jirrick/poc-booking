---
name: missed-notification-recovery
overview: Implement a hybrid recovery model with ConversationState as source of truth for seen messages, Search API replay as a safety net, and ingestion updates from both webhook and page-read API fetches.
todos:
  - id: add-conversation-state-table
    content: Add a per-conversation state table with latest message metadata and mapping references.
    status: pending
  - id: unify-ingestion-service
    content: Route webhook payloads and read-path Booking API payloads through one deduplicating ingest service.
    status: pending
  - id: implement-manual-search-replay
    content: Implement manual Search replay with time window + overlap, using conversation state to decide if messages are already seen.
    status: pending
  - id: update-read-path-ingestion
    content: Update Conversations and Conversation page fetch paths to ingest fetched messages and update conversation state even without prior webhook.
    status: pending
  - id: run-hybrid-drill
    content: Validate outage recovery and read-path-only ingestion with duplicate-safe behavior.
    status: pending
isProject: false
---

# Missed Notification Recovery Plan

## Selected approach (hybrid)

Use both mechanisms together:

- **ConversationState table** is the source of truth for whether a message/conversation state has already been seen.
- **Search API replay** remains the manual safety net so we do not miss messages during outages.
- **Webhooks are not the only ingestion path**: reading data from Booking API in UI flows also ingests and updates state.

## Baseline (already in code)

- Idempotent webhook ingestion on both `metadata.uuid` and `payload.message_id` in `[/Users/jiri.hudec/code/poc-booking/src/PocBooking.Api/Endpoints/BookingCnsWebhook.cs](/Users/jiri.hudec/code/poc-booking/src/PocBooking.Api/Endpoints/BookingCnsWebhook.cs)`.
- Processing path already normalizes payload and persists linked processing records in `[/Users/jiri.hudec/code/poc-booking/src/PocBooking.Api/Processing/ProcessCnsNotificationHandler.cs](/Users/jiri.hudec/code/poc-booking/src/PocBooking.Api/Processing/ProcessCnsNotificationHandler.cs)`.
- Search APIs are available in simulator and represent the same fallback pattern as real Booking Search.

## Data model to add

`ConversationState` keyed by Booking `conversation_id`:

- `PropertyId`
- `ConversationReference`
- `LastMessageId`
- `LastMessageTimestampUtc`
- `LastSeenAtUtc`
- optional denormalized refs: `InternalReservationId`, `InternalGuestId`

This table is the canonical source for “seen vs unseen”.

## Unified ingestion flow

```mermaid
flowchart TD
    sourceWebhook[WebhookSource] --> ingest[UnifiedIngestService]
    sourceRead[ReadPathSource] --> ingest
    sourceReplay[ManualSearchReplaySource] --> ingest

    ingest --> dedupMsg{MessageIdAlreadySeen}
    dedupMsg -->|yes| updateSeen[UpdateLastSeenOnly]
    dedupMsg -->|no| upsertMsg[UpsertMessageAndMappings]
    upsertMsg --> updateState[UpdateConversationStateLastMessage]
    updateSeen --> updateState
    updateState --> done[Completed]
```



## Read-path ingestion behavior

When user opens:

- `/Conversations` (list fetch)
- `/Conversation` (thread fetch)

Any messages returned by Booking API are passed to `UnifiedIngestService` and update:

- message dedup state
- `ConversationState.LastMessageId`
- `ConversationState.LastMessageTimestampUtc`

This guarantees that viewing data from Booking also repairs state, even if webhook delivery was missed.

## Manual replay flow (Search safety net)

Replay action inputs:

- optional `property_id`
- `afterUtc`, `beforeUtc`
- optional overlap (for clock-skew safety)

Replay algorithm:

- pull pages from Search results
- ingest each message via `UnifiedIngestService`
- treat `ConversationState` + `message_id` dedup as truth for seen/unseen

Safety:

- if no prior state exists, require explicit `afterUtc`
- cap replay range to Booking limits (90 days).

## Validation

- Simulate webhook outage, send messages, then open pages:
  - confirm read-path ingestion updates `ConversationState`
- Run manual Search replay:
  - confirm missed messages are recovered
  - confirm duplicates are ignored (already-seen messages not reinserted)
  - confirm conversation-level last message metadata remains correct

