# Final Test and Release Evidence

This document is the repository evidence for the final Assignment 2 test execution. It supersedes the numerical results and implementation statements in the earlier Assessment 1 testing documents. The report should use the latest successful `.trx` file and GitHub Actions run as its final screenshots.

## Final verification scope

The automated suite uses MSTest and follows Arrange, Act, Assert. Temporary SQLite databases are created for persistence, security, lifecycle, and HTTP tests, then deleted after each test. The web-test factory replaces the application database singleton so automated HTTP tests cannot read or change the developer's database.

| Required verification area | Automated evidence | Main assurance provided |
|---|---|---|
| Repository and restart persistence | `SqlitePersistenceTests` | Cases, assignments, completed states, units, coordinates and notifications survive reopening the database. |
| Password hashing, encryption and role access | `SecurityTests`, `PrototypeTesting.TC12`, `WebApplicationTests` | Salted hashes reject incorrect passwords, sensitive fields are encrypted at rest, and real cookie-authenticated requests enforce role and scope restrictions. |
| Audit-event completeness | `AuditLoggingTests`, `PriorityOverrideTests`, `FinalVerificationTests` | Creation, selected routing information, assignments, unit updates, sign-offs, closure and overrides retain the actor and relevant before/after details. Routing selections are stored in the case-created event; assignments have individual events. |
| Valid and invalid priority override | `PriorityOverrideTests` | Valid overrides preserve the calculated value and reason; missing reasons, unchanged values, closed cases and unauthorised roles are rejected. |
| Distance assignment edge cases | `DistanceAssignmentTests` | The nearest compatible available unit is selected; unavailable units are skipped; an equal-distance tie is resolved deterministically; waiting cases remain queued. |
| Database-backed performance | `FinalVerificationTests.DatabaseBackedMultiDepartmentDispatch_CompletesWithinTwoSeconds` | A local three-department SQLite dispatch, including persistence, assignment, notifications and audit writes, must complete in under two seconds. Database start-up is deliberately excluded. |
| Full database lifecycle | `FinalVerificationTests.FullDatabaseLifecycle_RemainsCorrectAcrossMultipleRestarts` | A multi-department case is created, partially signed off after one restart, closed after another restart, then reopened and checked for assignments, distances, released units and audit events. |
| Real HTTP demonstration workflows | `WebApplicationTests` | Real Razor Pages, antiforgery tokens, cookies and middleware verify login, case creation, case reporting, response-unit viewing, sign-off, denial paths and dashboard refresh output. |

## Requirements traceability summary

| Requirement | Final implementation | Verification | Result |
|---|---|---|---|
| FR-01/FR-02 Case recording and creation | Validated Razor form, generated case number and timestamp | TC-01, TC-02, TC-03, HTTP workflow | Passed |
| FR-03 Unit management | Scoped unit editing with location, coordinates and personnel | FR03 tests, restart tests, role tests | Passed |
| FR-04 Priority and authorised override | Department keyword strategy, fallback severity, reason-required override | TC-04, `PriorityOverrideTests` | Passed |
| FR-05 Department routing | Selected departments see the case; unrelated departments do not | TC-03, TC-05, TC-09, role tests | Passed for prototype routing |
| FR-06 Assignment and waiting queue | Nearest available compatible unit, deterministic tie and retry queue | TC-06 to TC-08, `DistanceAssignmentTests` | Passed |
| FR-07 Dashboard updates | Relevant dashboard projection and five-second browser refresh | TC-09 and rendered-page HTTP test | Automated checks passed; visual timing remains manual |
| FR-08/FR-09 Status and availability | Domain-controlled Open, In Progress and Closed states | TC-03, TC-06 to TC-10, database lifecycle | Passed |
| FR-10 Notifications | Persistent assignment notifications isolated from critical processing | TC-03, notification-failure and restart tests | Passed |
| FR-11 Sign-off and closure | Individual sign-off and closure after final required response | TC-10, database lifecycle, HTTP workflow | Passed |
| FR-12 History | OR search by caller, case number or date | TC-11 and repository tests | Passed |
| FR-13 Persistent audit | Persistent actor/time/action details for important workflow changes | `AuditLoggingTests`, override and lifecycle tests | Passed |
| NFR-R03 Persistence | Embedded SQLite repositories | `SqlitePersistenceTests`, database lifecycle | Passed on local prototype platform |
| NFR-S01/S03 Stored-data security | AES-GCM protected fields and PBKDF2 password hashes | `SecurityTests` | Passed for local at-rest design |
| NFR-S02/NFR-U01 Access and role GUIs | Cookie authentication, middleware and page-level scope checks | TC-12, report access tests, HTTP tests | Passed |
| Performance acceptance criterion | Local dispatch completes within two seconds | In-process TC-05 and database-backed performance test | Passed on the test machine |

Prototype routing means shared visibility and assignment inside this application. It does not mean integration with independent emergency-service networks.

## Quality areas and risk-based reasoning

Security testing was selected because the system stores caller and incident information and exposes several roles. The suite inspects raw database values, tests password verification and exercises allowed and denied routes through both middleware-level and real HTTP tests.

Reliability and performance testing were selected because a dispatch workflow must preserve case state and avoid excessive delay. Restart tests verify durable state through several database instances, concurrency tests protect against duplicate assignment, notification-failure tests protect the critical workflow, and the local database performance gate detects major regressions.

Manual checks are still appropriate for visual layout, keyboard-only navigation, screen-reader announcements, print layout, the visible five-second refresh, and demonstration clarity. These behaviours should be recorded with screenshots or a short test sheet in the final report.

## Current local quality workflow

The temporary quality workflow is run locally before changes are accepted:

1. Restore dependencies.
2. Build the complete solution with warnings treated as errors.
3. Run every automated test and save the `.trx` result.
4. Inspect NuGet dependencies for known vulnerabilities.
5. Review the result before merging or demonstrating the prototype.

The GitHub Actions workflow has been deferred while its Linux-only test behaviour is investigated. This is a current CI limitation and should be stated honestly in Section 5.5 rather than presenting CI as complete. Human review remains required for requirement changes, security decisions, usability and test quality.

## Final execution record

The final local run must be produced with:

```powershell
dotnet test .\Emergency-Dispatch-Priority-and-Coordination-System\Emergency-Dispatch-Priority-and-Coordination-System.slnx --no-restore --logger "trx;LogFileName=final-results.trx" --results-directory Test\TestResults\Final
```

Record the final totals from that file in the report:

| Metric | Final value |
|---|---:|
| Planned automated executions | 159 |
| Executed | 159 |
| Passed | 159 |
| Failed | 0 |
| Blocked | 0 |
| Not run/skipped | 0 |
| Pass rate | 100% |

These numbers count MSTest executions, including data-driven rows. They are not a statement of complete coverage of every possible input or production environment.

In the final local `.trx` run on 7 October 2026, the database-backed three-department dispatch test completed in 35 ms, the multi-restart lifecycle test completed in 38 ms, and the complete HTTP create/report/sign-off workflow completed in 500 ms. These are regression observations from one local machine, not production capacity or load-test claims.

## Residual risks and release decision

The prototype is **ready for the university demonstration and conditionally ready for local prototype release**. This decision is supported by complete automated execution, database restart evidence, role-level HTTP checks and no open critical defect in the final register.

It is not ready for real emergency-service operation. Residual risks include local key-file management, no independently verified TLS deployment, manually entered coordinates, polling rather than push updates, no external emergency-service integration, a small local performance sample, and manual accessibility/usability evidence still being required. Production use would require managed identity, managed encryption keys, external integration contracts, load and recovery testing, monitoring, privacy review and operational safety validation.
