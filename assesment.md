# Assessment: Microsoft Teams Meeting Bot with Transcript Storage in SharePoint

## Overview

This assessment evaluates your ability to build production-grade integrations within the Microsoft ecosystem, specifically across Microsoft Teams, Microsoft Graph, and Microsoft 365 Copilot.

You are expected to demonstrate:

- Strong backend engineering (C# / .NET preferred for bot layer)
- Deep understanding of Microsoft Graph
- Identity and security design (Entra ID, OBO flow)
- Distributed system thinking (retries, idempotency, async workflows)
- Ability to design Copilot agents and structured actions

---

# Part 1: Teams Meeting Bot + Transcript Pipeline (Hands-on)

## Objective

Build a Microsoft Teams meeting bot that:

1. Joins a meeting
2. Retrieves transcript via Microsoft Graph
3. Stores transcript in SharePoint

## Requirements

### Bot Implementation

- Use C# / .NET with Bot Framework SDK
- Bot must join a Teams meeting (proactive join preferred)
- Log lifecycle events (join, failure, exit)

### Transcript Pipeline

- Fetch transcript using Microsoft Graph API
- Handle delayed availability (retry/polling)
- Do NOT rely on manual downloads

### SharePoint Storage

- Upload transcript using Graph API
- Structure:

```
/Interview-Transcripts/{meetingId}/transcript_{timestamp}.txt
```

### Reliability (Mandatory)

- Retry logic for Graph failures
- Idempotency (no duplicate uploads)
- Proper error handling

---

# Part 2: M365 Copilot Action Agent (Hands-on)

## Objective

Design and implement Copilot actions that interact with hiring data.

## Requirements

### Action 1: Read Action

- Retrieve top candidates
- Return structured response

### Action 2: Write Action

- Schedule interview
- Must include:
    - Confirmation gate
    - Idempotency handling

### Mandatory Components

- JSON Schema (Draft-07) for actions
- Clear request/response contract
- Validation logic

### Expected Output

- Action definitions (JSON)
- Sample requests/responses
- Optional implementation (API handlers)

---

# Part 3: Identity, Security & Tenant Isolation (Design)

## Objective

Demonstrate secure multi-tenant architecture.

## Requirements

Explain or implement:

- Entra ID authentication flow
- OBO (On-Behalf-Of) token exchange
- Delegated vs Application permissions
- tenantId extraction from JWT (NOT from request payload)

### Must Answer

1. How do you prevent cross-tenant data leakage?
2. Where is JWT validated?
3. How are secrets managed?

---

# Part 4: System Design (Architecture)

## Objective

Design a scalable system for:

- 10,000 meetings/day
- Transcript processing
- Copilot integration

## Requirements

Provide a diagram or explanation covering:

- Bot → Graph → Processing pipeline
- Async processing (queues/events)
- Retry strategy
- Storage (Cosmos / Blob / SharePoint)

---

# Part 5: Data Integrity & Audit (Design or Implementation)

## Requirements

### Idempotency

- Explain or implement idempotency keys
- Prevent duplicate operations

### Audit Logging

- Append-only audit system
- No updates/deletes

---

# Deliverables

1. Source code (Part 1 + optional Part 2)
2. README with setup instructions
3. Architecture diagram
4. Short explanation document covering:
    - Copilot actions
    - Identity flow
    - System design

---

# Evaluation Criteria

## 1. Core Functionality (25%)

- Bot joins meeting
- Transcript retrieved
- Stored in SharePoint

## 2. Microsoft Graph Depth (15%)

- Correct API usage
- Handles delays/retries

## 3. Copilot Agent Design (20%)

- Proper action schema
- Clear contracts
- Handles write safety

## 4. Identity & Security (15%)

- OBO understanding
- Tenant isolation
- No insecure practices

## 5. System Design (15%)

- Scalability
- Async architecture

## 6. Data Integrity (10%)

- Idempotency
- Audit logging

---

This is not a basic CRUD assignment. The goal is to evaluate your ability to build secure, scalable, enterprise-grade integrations inside the Microsoft ecosystem.