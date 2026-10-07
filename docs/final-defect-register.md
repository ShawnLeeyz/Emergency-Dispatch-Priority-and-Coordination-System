# Final Defect Register and Root Cause Analysis

This register records significant defects or quality gaps found during development and final verification. Severity describes impact; priority describes correction urgency.

## Definitions and lifecycle

- Severity: Critical stops safe use or loses/provides unauthorised access to important data; High breaks a core workflow; Medium affects a non-critical workflow or assurance evidence; Low is cosmetic or minor.
- Priority: P1 immediate, P2 before final release, P3 planned improvement, P4 optional.
- Status: New, Confirmed, In Progress, Fixed, Retested, Closed, or Deferred.
- Lifecycle: New -> Confirmed -> assigned severity/priority -> In Progress -> Fixed -> independent regression test -> Retested -> Closed. A failed retest returns the defect to In Progress.

## Final register

| ID | Defect or quality gap | Severity / priority | Root cause | Corrective action and regression evidence | Status |
|---|---|---|---|---|---|
| D-01 | Cases, assignments and unit changes were lost when the application restarted. | High / P1 | Assessment 1 repositories intentionally stored state only in process memory. | Replaced runtime repositories with embedded SQLite implementations. Restart tests cover case creation, unit updates, notifications, closure and a complete multi-restart lifecycle. | Closed |
| D-02 | Demo credentials and sensitive case values could be readable in local storage. | High / P1 | The original prototype treated fake accounts and in-memory data as sufficient and had no stored-data threat model. | Added salted PBKDF2 password hashes and AES-GCM field encryption with raw-database inspection and restart authentication tests. | Closed |
| D-03 | Unit assignment selected the first available unit rather than the closest available unit required by the final scope. | High / P2 | The original rule had no coordinates or isolated assignment strategy, so repository order became the selection rule. | Added case/unit coordinates, Haversine distance, deterministic equal-distance handling and stored assignment distance. Tests cover closest, unavailable, tied and queued units. | Closed |
| D-04 | A notification-store exception could escape after a successful critical dispatch and make the workflow appear to fail. | High / P1 | A non-critical adapter shared the critical transaction path without an exception boundary. | Notification calls were isolated from critical case storage and assignment. A regression test injects a throwing notifier and confirms the case remains stored and assigned. | Closed |
| D-05 | Early HTTP tests could use the developer database instead of an isolated temporary database. | Medium / P1 | Adding a test configuration value did not replace the `SqliteDatabase` singleton already registered by application startup. | The web-test factory now removes and replaces the database singleton. The exact two test-created records were removed, and real HTTP tests now run against a per-factory temporary database that is deleted on disposal. | Closed |
| D-06 | The earlier test suite asserted some final values without fully proving negative paths, audit fields or real page behaviour. | Medium / P2 | Initial tests focused on happy-path outcomes and in-memory services. | Tests were strengthened with negative/boundary cases, field-level audit assertions, restart checks, database performance, page-model access and cookie/antiforgery HTTP workflows. | Closed |

## Root cause analysis 1: restart data loss (D-01)

Why did data disappear? It existed only in repository collections. Why were collections used? They were fast for the initial prototype. Why was this not caught earlier? Earlier tests reopened repository objects but did not restart a durable store. The underlying cause was a prototype storage decision combined with an incomplete interpretation of persistence testing.

Prevention: make durability explicit in requirements, test through a newly constructed database instance, and keep restart tests in the regression suite. The change improved reliability and also allowed accounts, notifications and audit evidence to survive restart.

## Root cause analysis 2: non-critical notification failure (D-04)

Why could dispatch appear to fail? The notifier exception left `CreateAndDispatch`. Why was that unsafe? Case storage and unit assignment had already succeeded, so the caller could retry and create inconsistent expectations. Why did this occur? The workflow did not distinguish critical state changes from a non-critical notification adapter. The underlying cause was a missing failure boundary.

Prevention: classify critical and non-critical components during design, isolate non-critical adapter failures, and inject a deliberately failing test double. This made the workflow more fault tolerant without hiding failures in critical repositories.

## Root cause analysis 3: HTTP test database isolation (D-05)

Why did a repeated HTTP test find duplicate test callers? The test server was resolving the application database singleton. Why did the temporary connection setting not prevent this? Application startup had already registered a concrete `SqliteDatabase`; changing configuration alone did not replace that object. Why was this not caught by earlier page tests? Read-only route tests produced no obvious persistent side effect. The underlying cause was assuming configuration replacement and service replacement were equivalent.

Prevention: replace stateful services explicitly in `ConfigureServices`, use a unique path for every factory, delete temporary database/key files on disposal, and include a write-based HTTP workflow in the suite. This protects developer data and improves test repeatability.

## Process improvements

- Add a regression test with every defect fix and state what would fail before the fix.
- Separate test data from development data at the dependency-injection boundary.
- Review requirements for measurable terms before implementation.
- Use failure-injection tests at external or non-critical boundaries.
- Retain test logs and link defect IDs to requirements, commits and test cases in the final report.
