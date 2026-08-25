# Appendix Design Conformance Review

This review compares the completed Appendices 1-4 in report revision `-4` with the Emergency Dispatch Priority and Coordination System prototype. The review was completed on 25 August 2026. Where an appendix gave a clear behavioural rule, the implementation and automated tests were updated to match it.

## Overall Result

| Appendix | Design area | Result after review | Implementation evidence |
|---|---|---|---|
| Appendix 1 | Police, Medical/Ambulance, and Fire priority diagrams | Aligned, with two documented interpretation decisions | `KeywordSeverityPriority`; TC-04 appendix data rows and multi-department test |
| Appendix 2 | Critical and non-critical modules | Aligned after notification isolation change | `DispatchService.NotifySafely`; notification-failure architecture test |
| Appendix 3 | Role interfaces and permissions | Core roles aligned; prototype Admin remains an explicit exception | Cookie authentication, `RoleAccessMiddleware`, scoped pages, TC-12 |
| Appendix 4 | Core tasks by role | Aligned | Case creation/history, department dashboard/unit management, response-unit view/sign-off |

## Appendix 1 - Priority and Severity Diagrams

### Implemented behaviour

- Police, Medical, and Fire each have their own High and Medium keyword groups.
- Only keyword groups for departments requested by the case are evaluated.
- Both incident type and incident description are searched case-insensitively.
- High matches are evaluated before Medium matches.
- When no listed keyword matches, the dispatcher-selected severity determines High, Medium, or Low priority.
- For a case requiring several departments, the highest keyword outcome becomes the single case priority.
- Fire's `small wildfire` is evaluated as Medium before the broader High `wildfire` term.
- Medical conditional phrases are represented as `conscious fall`, `fall while conscious`, `seizure has stopped`, and `stopped seizure`. Moderate burns accept `moderate burn` and `burn moderate`.

### Interpretation decisions still worth stating in the report

The diagrams assign priorities per department, but the domain stores one priority per case. The prototype therefore uses the highest match across all requested departments. This is a reasonable safety-first rule, but the appendix should state it explicitly.

Some diagram entries include conditions in parentheses rather than independent structured inputs. The system therefore relies on the dispatcher including those conditions in the incident type or description. An active seizure without wording that it has stopped does not match the Medium stopped-seizure rule and falls back to dispatcher severity.

## Appendix 2 - Critical and Non-Critical Modules

| Appendix module | Prototype component | Conformance |
|---|---|---|
| Record Log & Case Creation - Critical | `Case`, `CreateCaseRequest`, `DispatchService`, `ICaseRepository` | Aligned |
| Unit & Resource Management - Critical | `Unit`, `Department`, `DispatchService.UpdateUnit`, department repository | Aligned |
| Priority & Routing Units - Critical | `KeywordSeverityPriority`, `DispatchService.AssignAvailableUnits` | Aligned |
| Case Status Of Units - Critical | `Case.Assign`, `Case.SignOff`, `Case.UpdateStatus`, unit availability | Aligned |
| Department Dashboard - Non-Critical | Razor Page projection over repositories | Aligned; dashboard is outside the domain workflow |
| Notification Service - Non-Critical | `IDispatchNotifier` | Corrected: notifier exceptions are contained so creation and assignment remain successful |
| Search Logs - Non-Critical | `ICaseRepository.Search`, History Razor Page | Aligned; dispatch does not depend on search |

The main correction was notification isolation. Previously, an exception thrown by the notifier could escape after assignment and make the critical dispatch operation appear to fail. `DispatchService` now contains notification failures, and an automated architecture test verifies that the case remains stored, assigned, and In Progress.

## Appendix 3 - Roles and Permissions

| Role | Appendix access/actions | Prototype result |
|---|---|---|
| Dispatcher | View all active/recent cases; record and create cases; search history | Aligned |
| Emergency Department | View only its department dashboard, cases, units, availability, and notifications; update unit location and personnel count | Aligned through role and department scope checks |
| Response Unit | View only its own assignment and incident details; sign off its own response | Aligned through role and unit scope checks |

The application denies cross-role and cross-scope routes on the server, rather than relying only on hidden navigation. The automated access matrix covers allowed and denied Dispatcher, Police Department, and POL-01 Response Unit paths.

### Remaining exception: Admin

Appendix 3 says unlisted features are denied and does not list an Admin role. The report body separately describes a prototype-only Admin account for demonstration and visual testing. The code retains that Admin role because it is explicitly described elsewhere in the report. The final report should identify it as a prototype exception to Appendix 3 and state that it is not part of the intended released system.

## Appendix 4 - Core Tasks

| Role | Core task | Prototype result |
|---|---|---|
| Dispatcher | Enter required caller/incident details, select response types, and create a case | Aligned |
| Dispatcher | Search by caller name, case ID, or recorded date | Aligned; search uses OR semantics |
| Emergency Department | View its units and update location/personnel count | Aligned |
| Emergency Department | Monitor its cases, availability, assignments, waiting cases, and notifications | Aligned |
| Response Unit | View its current case and assignment details | Aligned |
| Response Unit | Sign off after completing its response | Aligned; unit is released and the case closes after final required sign-off |

## Remaining Report-to-Prototype Differences

These are not appendix implementation failures, but they should be described consistently in the report:

1. The report body says unit suitability considers availability and distance. The prototype selects the first available unit in department-list order and performs no distance calculation.
2. Department routing is shared in-memory case visibility and assignment, not transmission to independent external emergency-service endpoints.
3. All repositories are in memory, so case, unit, assignment, and notification data reset when the application restarts.
4. Demo credentials are plaintext fake accounts suitable only for the university prototype.
5. The prototype-only Admin role is outside the three operational roles listed in Appendices 3 and 4.

## Verification

Run the complete suite from the repository root:

```powershell
dotnet test .\Emergency-Dispatch-Priority-and-Coordination-System\Emergency-Dispatch-Priority-and-Coordination-System.slnx --nologo
```

The final execution result should be copied into Section 7.4 and the mismatch record after any later change. Browser evidence is still recommended for validation messages, five-second dashboard refresh, and visible role-specific navigation.

Latest verified result on 25 August 2026: **103 passed, 0 failed, 0 skipped**.
