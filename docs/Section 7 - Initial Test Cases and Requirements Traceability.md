# 7. Initial Test Cases and Requirements Traceability

The initial test cases below are derived from the improved requirements in Section 3.4, the acceptance criteria in Section 3.5, and the risk-based testing scope in Section 6.1. The cases focus on the workflows with the greatest potential impact: recording emergency information, creating and prioritising cases, routing cases, assigning response units, and maintaining correct case and unit states. Selected reliability, performance, usability, and concurrency requirements are included where they can be evaluated in the prototype environment.

All test data will be fictional. Before execution, the prototype must meet the entry criteria in Section 6.5. The actual result, execution date, tester, and pass/fail status will be recorded when each test is run. These cases follow the completed prototype workflow: the dispatcher submits one emergency form; the system creates, prioritises, stores, and routes the case; the first available unit in each selected department is assigned; response units sign off from their own workspace; and released units are offered to the oldest compatible waiting case.

The priority policy follows Appendix 1 using separate Police, Medical, and Fire keyword groups. It searches the incident type and description using only the groups for the departments requested by the case. A High match takes precedence over a Medium match; if no Appendix 1 keyword matches, the dispatcher-selected severity is used. For a multi-department case, the highest matching priority becomes the case priority. The specific Appendix 1 condition notes, such as a conscious fall, a stopped seizure, and a small wildfire, must be represented clearly in the entered incident information.

## 7.1 Initial Test Cases

### TC-01 - Record a complete emergency call

| Field | Details |
|---|---|
| Requirement(s) | FR-01 |
| Test level/type | System and acceptance test; positive test |
| Risk priority | High |
| Preconditions | The prototype is running and the dispatcher is on the case-creation interface. |
| Test data | Caller: Alex Morgan; phone: 021 123 4567; incident type: vehicle collision; location: 25 Queen Street; description: two vehicles blocking the road; reported severity: High; required departments: Police and Medical. |
| Steps | 1. Enter all required call details. 2. Select Police and Medical as the required departments. 3. Click **Create Case**. |
| Expected result | Exactly one case is created and given a unique case ID and automatic recorded timestamp. Every entered value is retained without alteration. In the same submission, the system determines the prototype priority, makes the case available to the selected departments, and attempts to assign suitable available units. |

### TC-02 - Reject an incomplete emergency call

| Field | Details |
|---|---|
| Requirement(s) | FR-01, NFR-U02 |
| Test level/type | System and acceptance test; negative test and equivalence partitioning |
| Risk priority | High |
| Preconditions | The dispatcher is on the call-recording interface. |
| Test data | Complete valid data except for a blank incident location. Repeat with all details present but no required unit type selected. |
| Steps | 1. Enter the test data. 2. Submit the record with the location blank. 3. Observe the response. 4. Enter the location, remove all selected unit types, and submit again. |
| Expected result | Both submissions are rejected and no case is created. The relevant missing field is identified in plain language, the message tells the dispatcher how to correct the problem, and no exception, stack trace, or debug information is displayed. |

### TC-03 - Create and dispatch a case from one submitted call form

| Field | Details |
|---|---|
| Requirement(s) | FR-02, FR-04, FR-05, FR-06, FR-08, FR-09, FR-10, NFR-R03 |
| Test level/type | Integration and system test; end-to-end positive test |
| Risk priority | Critical |
| Preconditions | The prototype is running. Police and Medical departments exist, with at least one suitable unit available in each department. The dispatcher is on the case-creation interface. |
| Test data | Caller: Alex Morgan; phone: 021 123 4567; incident type: vehicle collision; location: 25 Queen Street; description: two vehicles blocking the road; reported severity: High; required departments: Police and Medical. |
| Steps | 1. Enter all call details in the case-creation form. 2. Record the time immediately before submission. 3. Click **Create Case** once. 4. Open the dispatcher dashboard and locate the new case. 5. Open the Police and Medical dashboards. 6. Compare the displayed and stored case fields with the submitted form values. 7. Inspect the assigned units, availability, and assignment notifications. |
| Expected result | The one submission creates exactly one case with a unique case ID and an automatically recorded timestamp. All submitted values are preserved. Because **collision** is not an Appendix 1 Police or Medical keyword, the dispatcher-selected High severity is used as the fallback and the case priority is **High**. The case is visible to Police and Medical, the first available unit in each department is assigned, both units become **Not Available**, the case becomes **In Progress**, and one notification containing the case details is created for each assigned unit. |

### TC-04 - Apply Appendix 1 department keywords and dispatcher-severity fallback

| Field | Details |
|---|---|
| Requirement(s) | FR-04 |
| Test level/type | Unit test; decision-table and precedence testing |
| Risk priority | Critical |
| Preconditions | The prototype priority component is available. |
| Test data | Every High and Medium term listed in the Police, Medical/Ambulance, and Fire diagrams; a Fire **small wildfire** case; an incident-type-only **chest pain** case; a Police case containing the Medical-only term **heart attack**; a multi-department case containing Police Medium and Medical High terms; and no-keyword cases using High, Medium, and Low dispatcher severity. |
| Steps | 1. Create one case for each Appendix 1 keyword using its relevant department and Low severity. 2. Calculate and record every priority. 3. Verify **small wildfire** is Medium while an unqualified **wildfire** is High. 4. Verify keywords are read from incident type as well as description. 5. Verify a keyword from an unrequested department is ignored. 6. Verify the highest matching priority wins for a multi-department case. 7. Test all three severity values when no keyword matches. |
| Expected result | Every Appendix 1 High term returns **High** and every Medium term returns **Medium** for its relevant department. High takes precedence across a multi-department case. A department-specific term does not affect a case that did not request that department. Incident type and description are both evaluated. With no matching term, the result equals the dispatcher-selected High, Medium, or Low severity. |

### TC-05 - Process a multi-department case within two seconds without data loss

| Field | Details |
|---|---|
| Requirement(s) | FR-05, NFR-P01, NFR-R01 |
| Test level/type | Integration and performance test; timing and data-consistency test |
| Risk priority | Critical |
| Preconditions | The in-memory Police, Fire, and Medical departments are available, with at least one unit available in each. Test instrumentation can measure the complete submission call. |
| Test data | A complete case requiring Police, Fire, and Medical response. |
| Steps | 1. Capture the submitted request values. 2. Start timing immediately before calling the case creation and dispatch service. 3. Stop timing when the service returns. 4. Compare the stored case with the submitted values. 5. Inspect required departments and assigned-unit types. |
| Expected result | The in-process creation, priority, storage, and assignment workflow completes within 2 seconds. The stored case preserves every submitted value, lists Police, Fire, and Medical as required departments, and has one unit of each type assigned. This prototype does not transmit separate copies to independent department endpoints. |

### TC-06 - Assign only an available unit and select the first available unit

| Field | Details |
|---|---|
| Requirement(s) | FR-06, FR-08, FR-09 |
| Test level/type | Unit and integration test; boundary value test |
| Risk priority | Critical |
| Preconditions | A Police case has been routed to the Police department. The ordered unit list is P-01 unavailable, P-02 available, and P-03 available. |
| Test data | One Open Police case and the unit list described above. |
| Steps | 1. Trigger automatic unit assignment. 2. Inspect the assigned unit. 3. Inspect the state of the case and all three units. |
| Expected result | P-02, the first available unit in the department list, is assigned. P-01 and P-03 are not assigned. P-02 changes to **Not Available**, and the case changes from **Open** to **In Progress**. |

### TC-07 - Queue a case when no unit is available

| Field | Details |
|---|---|
| Requirement(s) | FR-06, FR-08 |
| Test level/type | Unit and integration test; boundary value test |
| Risk priority | Critical |
| Preconditions | A Fire case has been routed to the Fire department, and every Fire unit is marked Not Available. |
| Test data | One Open Fire case and zero available Fire units. |
| Steps | 1. Submit the Fire case while both Fire units are assigned to earlier cases. 2. Inspect the waiting case and its status. 3. Sign off the first Fire unit from its response-unit workspace. 4. Inspect the released unit and the waiting case. |
| Expected result | Initially, no unit is assigned, the case remains **Open**, and it appears as waiting for Fire. When the first unit signs off, it is released and immediately assigned to the oldest compatible waiting case. The waiting case becomes **In Progress**, and the unit returns to **Not Available** because it has received the new assignment. |

### TC-08 - Prevent the same unit from being assigned concurrently to two cases

| Field | Details |
|---|---|
| Requirement(s) | FR-06, FR-09, NFR-R04 (prototype concurrency coverage) |
| Test level/type | Integration and regression test; concurrency/race-condition test |
| Risk priority | Critical |
| Preconditions | Exactly one suitable Ambulance unit, A-01, is Available. Two Open Medical cases are ready for assignment. The test harness can release two assignment requests simultaneously. |
| Test data | Cases C-101 and C-102; unit A-01. |
| Steps | 1. Prepare simultaneous assignment requests for C-101 and C-102. 2. Release both requests at the same time. 3. Wait for both operations to finish. 4. Inspect both cases, A-01, and the assignment records. 5. Repeat the test to detect intermittent race conditions. |
| Expected result | A-01 is assigned to only one case and has only one active assignment record. Its status is **Not Available**. The other case remains **Open** in the queue. The system accepts and displays both cases without crashing or corrupting their data. |

### TC-09 - Display and automatically update the correct department dashboard

| Field | Details |
|---|---|
| Requirement(s) | FR-07 |
| Test level/type | System and acceptance test; positive and separation test |
| Risk priority | High |
| Preconditions | The Police dashboard is open. Existing Police and Fire cases are available. |
| Test data | Existing Police case P-C01, Fire-only case F-C01, and new Police case P-C02. |
| Steps | 1. Open the Police dashboard and verify its initial cases. 2. Submit P-C02 from the dispatcher interface. 3. Keep the Police dashboard visible and do not manually refresh it. 4. Observe the dashboard for at least one five-second polling interval. |
| Expected result | Initially, the Police dashboard displays P-C01 but not F-C01. After its automatic five-second page refresh, it also displays P-C02 without a manual refresh. It continues to exclude the Fire-only case. |

### TC-10 - Keep a multi-unit case open until the final unit signs off

| Field | Details |
|---|---|
| Requirement(s) | FR-08, FR-09, FR-11 |
| Test level/type | Integration and acceptance test; decision-table and state-transition test |
| Risk priority | Critical |
| Preconditions | A case is In Progress with Police unit P-02 and Medical unit MED-01 assigned; both units are Not Available. |
| Test data | One active multi-unit case with two assigned units. |
| Steps | 1. Sign off P-02 from its response-unit workspace. 2. Inspect the case and both unit states. 3. Sign off MED-01 from its response-unit workspace. 4. Inspect the final case and unit states. |
| Expected result | After P-02 signs off, it becomes **Available**, but the case remains **In Progress** because MED-01 is active. After MED-01 signs off, it becomes **Available** and the case automatically becomes **Closed**. |

### TC-11 - Search case history using caller, case ID, or date

| Field | Details |
|---|---|
| Requirement(s) | FR-12 |
| Test level/type | Integration and acceptance test; equivalence partitioning |
| Risk priority | High |
| Preconditions | Four cases with different caller names, case IDs, and recorded dates exist in the in-memory history. The dispatcher is authorised to use History. |
| Test data | One case matching caller **Alex**, a second case matching the entered case ID, a third case recorded on the entered date, and a fourth case matching none of the fields. |
| Steps | 1. Open History. 2. Enter **Alex**, the second case's ID, and the third case's date in the three search fields. 3. Submit the search. 4. Inspect the returned cases. |
| Expected result | The three cases matching at least one entered field are returned, because history search uses OR semantics. The fourth case is not returned. The search does not modify any case data. |

### TC-12 - Enforce role-based access to prototype features

| Field | Details |
|---|---|
| Requirement(s) | NFR-S02, NFR-U01 |
| Test level/type | System test; basic authorisation and usability test |
| Risk priority | High |
| Preconditions | Test accounts exist for Dispatcher, Department, and Response Unit roles. Cookie authentication and role/scope route enforcement are enabled. |
| Test data | One authenticated account for each implemented role. |
| Steps | 1. Sign in as Dispatcher, Police Department, and POL-01 Response Unit in turn. 2. Record the visible navigation, pages, and case information. 3. As Dispatcher, request `/Departments/Police`. 4. As Police Department, request `/Departments/Fire`. 5. As POL-01, request `/ResponseUnits/POL-02`. |
| Expected result | Each role sees only its required tools and permitted information. An unauthorised page or action is denied even when requested directly, and the denial does not expose protected case data or technical error details. |

## 7.2 Initial Requirements Traceability Matrix

The matrix links each requirement selected for the initial prototype test cycle to one or more test cases. A requirement marked **Deferred** is intentionally outside the initial executable test scope described in Section 6.1, rather than accidentally omitted.

| Requirement | Requirement summary | Linked test case(s) | Initial coverage/status |
|---|---|---|---|
| FR-01 | Record all required emergency call details | TC-01, TC-02, TC-03 | Positive input, missing input, and preservation during the single-step submission |
| FR-02 | Create a case when the completed call form is submitted | TC-01, TC-03 | Single-step case creation, unique ID, automatic timestamp, and persistence |
| FR-03 | Department management of response-unit details | - | Deferred; not selected in the Section 6.1 core scope |
| FR-04 | Automatically determine priority from incident information and reported severity | TC-03, TC-04 | All Appendix 1 department keywords, cross-department precedence, department scoping, incident-type/description matching, and severity fallback |
| FR-05 | Route to every relevant department | TC-03, TC-05 | Submission integration, multi-department routing, and exclusion of unrelated departments |
| FR-06 | Assign the first available suitable unit or queue the case | TC-03, TC-06, TC-07, TC-08 | Submission integration, available/unavailable boundaries, and concurrent assignment |
| FR-07 | Show and automatically update department cases | TC-09 | Department separation and new-case visibility after the five-second refresh |
| FR-08 | Maintain valid Open, In Progress, and Closed states | TC-03, TC-06, TC-07, TC-10 | Creation, waiting, assignment, partial sign-off, and closure transitions |
| FR-09 | Synchronise unit availability with assignment and release | TC-03, TC-06, TC-07, TC-08, TC-10 | Assignment, concurrency, sign-off, release, and immediate reassignment paths |
| FR-10 | Notify an assigned unit with case details | TC-03 | One assignment notification per assigned unit with case number, incident, and location |
| FR-11 | Permit unit sign-off and close after final sign-off | TC-10 | Partial and final sign-off for a multi-unit case |
| FR-12 | Search historical cases by caller, case ID, or date | TC-11 | OR matching across all three supported search fields |
| NFR-P01 | Route a completed case within 2 seconds | TC-05 | Executable timing measurement |
| NFR-P02 | Support 100 concurrent users at 2 seconds or less | - | Deferred at full scale; Section 6.4 proposes a separate 10-user prototype benchmark |
| NFR-M01 | Isolate core functions from non-critical modules | Design/code review | Reviewed against Appendix 2; not a pass/fail test case |
| NFR-M02 | Add roles/departments without modifying existing classes | Design/code review | Reviewed against Appendix 2; not a pass/fail test case |
| NFR-R01 | Preserve case data during department routing | TC-05 | Field-by-field sender/receiver comparison |
| NFR-R02 | Maintain 99.9% availability over one week | - | Deferred; long-term availability testing is outside prototype scope |
| NFR-R03 | Save reports and case-log data without loss | TC-03 | Field-by-field comparison with stored case data |
| NFR-R04 | Continue operating with 100 open cases | TC-08 | Partial prototype evidence for concurrency and stability; the full 100-case test remains deferred |
| NFR-S01 | Encrypt data passing between users and the system | - | Deferred until encryption is implemented; full security testing is outside prototype scope |
| NFR-S02 | Restrict features and information by role | TC-12 | Basic interface and direct-access authorisation checks |
| NFR-U01 | Tailor the interface to each authenticated role | TC-12 | Visible tools and information checked against Appendix 3 |
| NFR-U02 | Display actionable plain-language errors | TC-02 | Validation error content and absence of technical details |
| NFR-U03 | Complete core tasks after no more than one hour of training | - | Deferred; real-user usability testing is outside prototype scope |

## 7.3 How Traceability Supports Quality Assurance and Change Management

Requirements traceability gives the team evidence that testing is based on the agreed behaviour of the Emergency Dispatch Priority & Coordination System. The matrix makes it possible to check that every high-risk requirement selected in Section 6 has at least one test and that important acceptance criteria have not been missed. It also shows where a requirement is covered by several tests. For example, FR-08 is tested during case creation, assignment, queuing, partial sign-off, and final sign-off because a case-status defect could occur at any of these transitions. This supports quality assurance by making coverage visible, helping the team prioritise critical tests, and providing a basis for the exit criteria in Section 6.5.

Traceability also helps the team assess and control changes. If a requirement changes, its row identifies the test cases that must be reviewed and rerun. For example, if FR-06 changes from selecting the first available unit to selecting the nearest available unit, TC-06, TC-07, and TC-08 must be updated and used as regression tests. The team can also trace a failed test back to the affected requirement and acceptance criteria, which makes defect reporting and impact analysis clearer. Conversely, a new or changed test with no linked requirement may indicate uncontrolled scope or a requirement that has not been documented.

The matrix should be maintained throughout development rather than treated as a one-time document. If the prototype keyword list or dispatcher-severity fallback changes, TC-03 and TC-04 identify the affected regression coverage. If role paths or scopes change, TC-12 identifies the authorisation cases that must be reviewed. Each execution should record its result and any defect ID. Keeping these links current gives the team an audit trail from requirement to test evidence and defect, supports targeted regression testing after code changes, and reduces the chance that a change will introduce an unnoticed failure elsewhere in the system.

## 7.4 Test Execution Summary

The automated suite was executed on 25 August 2026 using .NET 10 and MSTest 4.0.2. The command was:

```powershell
dotnet test .\Emergency-Dispatch-Priority-and-Coordination-System\Emergency-Dispatch-Priority-and-Coordination-System.slnx --nologo
```

The suite was most recently rerun after the Appendix 1 and Appendix 2 implementation updates. It completed successfully: **103 passed, 0 failed, and 0 skipped**. The total is greater than twelve because TC-04 expands into 85 Appendix priority rows plus a multi-department test, TC-12 expands into six role/path rows, and an additional architecture test verifies that failure of the non-critical notification service cannot break critical dispatch processing.

| Test case | Automated result | What the automated test proves | Additional evidence needed |
|---|---|---|---|
| TC-01 | Pass | One case is stored with the entered values, generated case number, and submission timestamp. | Optional screenshot of the completed form and dispatcher dashboard. |
| TC-02 | Pass | Blank location and empty department selection are rejected without storing a case. | Manually confirm field-level messages are visible, understandable, and contain no stack trace or debug details. |
| TC-03 | Pass | One call creates, prioritises, stores, assigns, and notifies a Police-and-Medical case correctly. | Optional screenshots from the dispatcher, Police, Medical, and response-unit interfaces. |
| TC-04 | Pass - 85 data rows plus multi-department test | Every Appendix 1 department keyword, conditional phrase variants, special Fire ordering, department scoping, incident-type matching, highest-priority selection, and all severity fallbacks behave as designed. | The report should clarify how condition notes such as “fall if conscious” and “seizure if stopped” are phrased in dispatcher input. |
| TC-05 | Pass | The in-process three-department workflow completes below two seconds and preserves request data. | Do not present this as a network or independent-endpoint benchmark. Record elapsed time if the report requires a numeric result. |
| TC-06 | Pass | Assignment skips an unavailable unit and chooses the first available unit in list order. | None. |
| TC-07 | Pass | A case waits Open when no unit is available, then receives the released unit after sign-off. | Optional screenshots of the waiting and reassigned states. |
| TC-08 | Pass | Simultaneous submissions cannot assign the only Medical unit to both cases. | Repeat during later regression runs because scheduling-related defects can be intermittent. |
| TC-09 | Pass - data projection | A refreshed Police projection includes new Police cases and excludes Fire-only cases. | Browser test must confirm the page visibly reloads after approximately five seconds without manual refresh. |
| TC-10 | Pass | Partial sign-off leaves the case In Progress; final sign-off releases the remaining unit and closes the case. | Optional screenshots from both response-unit workspaces. |
| TC-11 | Pass | History uses OR matching across caller name, case ID, and date and excludes a non-match. | Optional screenshot of the search form and three returned rows. |
| TC-12 | Pass - 6 role/path rows | Middleware permits each tested role's own path and redirects cross-role or cross-scope requests. | Browser test must confirm each role sees only its intended navigation and information. |

No automated test failures were present in this execution. Any later failure should be entered in the table below and linked to the affected requirement in Section 7.2.

## 7.5 Manual Verification and Defect Record

This table is ready for the team member completing the report evidence. Use one row per manual check or defect. Keep the actual result factual and attach a screenshot or defect reference where available.

| Test case | Date | Tester | Environment/browser | Expected result | Actual result | Status | Evidence or defect ID |
|---|---|---|---|---|---|---|---|
| TC-02 |  |  |  | Validation is clear and no technical details are shown. |  | Not run |  |
| TC-09 |  |  |  | The Police dashboard reloads after about five seconds and displays the new Police case only. |  | Not run |  |
| TC-12 |  |  |  | Each role sees only permitted navigation and direct unauthorised requests are denied. |  | Not run |  |

For a failed check, record the observed behaviour, reproduction steps, severity, and defect ID. After a fix, rerun the linked automated and manual cases and add a new row rather than overwriting the original result.
