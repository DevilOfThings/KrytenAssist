# AGENTS.md

AI Contributor Guide for Kryten Assist

## Purpose

This document is the primary onboarding guide for AI coding agents contributing
to Kryten Assist.

All AI agents should read this document before making changes to the codebase.

Unless explicitly instructed otherwise, this document takes precedence over
general coding preferences.

## Your Role

You are joining an existing long-running software project.

You are **not** designing the architecture.

You are the implementation engineer responsible for making high-quality, incremental changes while preserving the existing architecture and coding standards.

The architecture, roadmap and implementation philosophy already exist and should be treated as the source of truth.

---

# Before Writing Any Code

Read the following documents in order.

1. docs/Roadmap.md
2. docs/AI Playbook/031a - Runtime Context Injection.md
3. The current prompt you have been asked to implement.
4. Any files directly referenced by that prompt.

Do not begin implementation until you understand the scope.

---

# About Kryten Assist

Kryten Assist is a desktop-first personal AI assistant.

Its purpose is:

> Making Future Robin's Life Easier.

The application is intended to become Robin's long-term personal desktop assistant rather than simply another AI chat client.

---

# Architecture Principles

Always preserve the following principles.

## Clean Architecture

Dependencies point inward.

Do not introduce shortcuts.

Keep provider-specific implementations isolated.

---

## Provider Independence

The application owns its abstractions.

Never leak OpenAI (or any provider) SDK types outside provider implementations.

Interfaces belong to the application.

Providers adapt to the application.

---

## Offline First

Offline behaviour is a first-class feature.

Do not replace offline functionality with cloud functionality.

Where online features are introduced, preserve an offline implementation whenever practical.

---

## Dependency Injection

Register services through extension methods.

Avoid manual registrations in Program.cs where possible.

Prefer constructor injection.

Avoid service location.

---

## MVVM

The Avalonia application uses MVVM.

Do not move logic into XAML code-behind.

Prefer ViewModels, commands and services.

---

# Coding Style

Prefer:

- small focused classes
- meaningful names
- constructor injection
- immutable models where practical
- async throughout
- comprehensive unit tests

Avoid:

- static helper classes
- duplicated logic
- large methods
- unnecessary abstractions
- provider coupling

---

# Existing Project Status

The following features already exist.

✔ CRUD Prompt API

✔ React Client

✔ Avalonia Desktop Client

✔ Offline Prompt Store

✔ Offline Prompt Search

✔ Offline Semantic Search

✔ OpenAI Embedding Provider

✔ AI Conversations

✔ Conversation Memory

✔ Provider-independent Tool Framework

✔ Runtime Context Injection

These are complete.

Do not redesign them unless explicitly instructed.

---

# How To Work

Implement only the requested prompt.

Do not add unrelated improvements.

If you identify possible future enhancements, mention them separately rather than implementing them.

---

# Build Process

After completing a coherent set of changes:

- build the solution
- resolve compiler warnings introduced by your changes
- run relevant tests
- stop

Do not continue implementing additional ideas.

---

# Output

When complete provide:

## Summary

A concise summary of what changed.

## Files Modified

List every file changed.

## Build

Build status.

## Tests

Tests executed and results.

## Notes

Anything the reviewer should inspect manually.

---

# Important

Architectural consistency is more important than cleverness.

When in doubt:

- preserve the existing design
- implement incrementally
- stop after the requested scope

## Definition of Done

A prompt is not complete until:

- implementation is complete
- tests pass
- prompt markdown is updated
- roadmap is updated
- session handover is created if appropriate
- results section is completed

## Never

- Do not redesign completed prompts.
- Do not change architecture without being asked.
- Do not introduce provider-specific dependencies into shared layers.
- Do not remove tests to make builds pass.
- Do not implement future roadmap items unless requested.

