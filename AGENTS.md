# AI Agent Guide: poc-booking

Entry point for AI agents working on this POC repository.

## Purpose

**poc-booking** is a proof-of-concept for integrating with Booking.com Messaging via the **Connectivity Notification Service (CNS)**. It is not production code; it validates flows, constraints, and mapping strategies that will inform an implementation in the Communication platform.

## Quick Start

- **Read first**: [booking-cns-messaging-overview.md](booking-cns-messaging-overview.md) — technical and business limitations, delivery model, retention, identity mapping, authentication.
- **Repo layout**: POC structure may evolve (e.g. `/src`, `/docs`). Overview and README are the source of truth for scope and references.

## Conventions

- Follow the constraints and guardrails described in `booking-cns-messaging-overview.md`.
- Prefer minimal, runnable code over full production patterns.
- Keep documentation and code in sync when changing scope.

## Related Repositories

- **communication** — Production omni-channel communication platform; eventual home for Booking.com integration.
