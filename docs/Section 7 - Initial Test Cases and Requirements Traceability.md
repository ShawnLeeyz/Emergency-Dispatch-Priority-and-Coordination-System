# 7. Initial Test Cases and Requirements Traceability

The initial test cases below are derived from the improved requirements in Section 3.4, the acceptance criteria in Section 3.5, and the risk-based testing scope in Section 6.1. The cases focus on the workflows with the greatest potential impact: recording emergency information, creating and prioritising cases, routing cases, assigning response units, and maintaining correct case and unit states. Selected reliability, performance, usability, and concurrency requirements are included where they can be evaluated in the prototype environment.

All test data will be fictional. Before execution, the prototype must meet the entry criteria in Section 6.5. The actual result, execution date, tester, and pass/fail status will be recorded when each test is run. Where a test uses the priority rules in Appendix 1, the exact input and expected priority must be selected from the approved Medical, Police, or Fire decision process before execution.

## 7.1 Initial Test Cases

### TC-01 - Record a complete emergency call

| Field | Details |
|---|---|
| Requirement(s) | FR-01 |
| Test level/type | System and acceptance test; positive test |
| Risk priority | High |
| Preconditions | The prototype is running and the dispatcher is on the case-creation interface. |
| Test data | Caller: Alex Morgan; phone: 021 123 4567; incident type: vehicle collision; location: 25 Queen Street; description: two vehicles blocking the road; reported severity: High; required departments: Police and Medical. |
| Steps | 1. Enter all required call details. 2. Select Police and Medical as the required departments. 3. Click **Create and dispatch case**. |
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
| Requirement(s) | FR-02, FR-04, FR-05, FR-06, FR-08, FR-09, NFR-R03 |
| Test level/type | Integration and system test; end-to-end positive test |
| Risk priority | Critical |
| Preconditions | The prototype is running. Police and Medical departments exist, with at least one suitable unit available in each department. The dispatcher is on the case-creation interface. |
| Test data | Caller: Alex Morgan; phone: 021 123 4567; incident type: vehicle collision; location: 25 Queen Street; description: two vehicles blocking the road; reported severity: High; required departments: Police and Medical. |
| Steps | 1. Enter all call details in the case-creation form. 2. Record the time immediately before submission. 3. Click **Create and dispatch case** once. 4. Open the dispatcher dashboard and locate the new case. 5. Open the Police and Medical dashboards. 6. Compare the displayed and stored case fields with the submitted form values. 7. Inspect the assigned units and their availability. |
| Expected result | The one submission creates exactly one case with a unique case ID and an automatically recorded timestamp. All submitted caller, incident, location, description, severity, and required-department values are preserved. The system calculates a priority, exposes the case to both selected departments, and assigns the first suitable available Police and Medical units. The assigned units become **Not Available**, and the case is **In Progress**. No separate call-record or case-creation action is required. |

### TC-04 - Determine priority using the approved department rules

| Field | Details |
|---|---|
| Requirement(s) | FR-04 |
| Test level/type | Unit and acceptance test; decision-table testing |
| Risk priority | Critical |
| Preconditions | The priority decision processes in Appendix 1 have been completed and approved. A newly created case is available for processing. |
| Test data | One representative Medical, Police, and Fire case for each priority outcome defined in Appendix 1, including any documented boundary conditions. Each data row must have a predetermined expected priority. |
| Steps | 1. Submit each case to the priority component. 2. Record the returned priority. 3. Compare it with the outcome defined by the relevant department decision process. |
| Expected result | Every case is assigned a priority automatically, without dispatcher input. Each result matches the approved decision process for its department, including the defined boundary cases. |

### TC-05 - Route a multi-department case within two seconds without data loss

| Field | Details |
|---|---|
| Requirement(s) | FR-05, NFR-P01, NFR-R01 |
| Test level/type | Integration and performance test; timing and data-consistency test |
| Risk priority | Critical |
| Preconditions | Police, Fire, and Medical department endpoints are available. A timer or test instrumentation can record routing duration. |
| Test data | A complete case requiring Police, Fire, and Ambulance units. |
| Steps | 1. Capture a copy of every case field. 2. Start timing when the completed case is submitted for routing. 3. Stop timing when all relevant departments receive it. 4. Compare the received copies with the submitted case. 5. Check that no unrelated department received it. |
| Expected result | The case is delivered to the Police, Fire, and Medical departments within 2 seconds. No unrelated department receives it, and every received case contains the same case ID, caller details, incident details, priority, location, timestamp, and requested unit types as the submitted case. |

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
| Steps | 1. Trigger automatic unit assignment. 2. Inspect the case queue and case status. 3. Change the first Fire unit to Available. 4. Process the queue again. |
| Expected result | Initially, no unit is assigned, the case remains **Open**, and it is retained in the Fire queue. When a unit becomes available, the queued case is assigned to that unit, the case becomes **In Progress**, and the unit becomes **Not Available**. |

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
| Test data | Police case P-C01, Fire case F-C01, and a new Police case P-C02. |
| Steps | 1. Open the Police dashboard. 2. Verify its initial cases. 3. Route P-C02 to Police. 4. Change the priority or location of P-C01 from another valid session. 5. Observe the dashboard without manually refreshing. |
| Expected result | The dashboard displays P-C01 and P-C02 but not F-C01. P-C02 appears automatically, and the changed information for P-C01 is shown automatically without a manual refresh. |

### TC-10 - Keep a multi-unit case open until the final unit signs off

| Field | Details |
|---|---|
| Requirement(s) | FR-08, FR-09, FR-11 |
| Test level/type | Integration and acceptance test; decision-table and state-transition test |
| Risk priority | Critical |
| Preconditions | A case is In Progress with Police unit P-02 and Ambulance unit A-01 assigned; both units are Not Available. |
| Test data | One active multi-unit case with two assigned units. |
| Steps | 1. Sign off P-02. 2. Inspect the case and both unit states. 3. Sign off A-01. 4. Inspect the final case and unit states. |
| Expected result | After P-02 signs off, P-02 becomes **Available**, but the case remains **In Progress** because A-01 is active. After A-01 signs off, A-01 becomes **Available** and the case automatically becomes **Closed**. |

### TC-11 - Return a case to Open when all assigned units are removed

| Field | Details |
|---|---|
| Requirement(s) | FR-08, FR-09 |
| Test level/type | Integration test; state-transition and negative-path test |
| Risk priority | Critical |
| Preconditions | A case is In Progress, one unit is assigned, and the unit has not completed or signed off. |
| Test data | Case C-103 and assigned unit P-03. |
| Steps | 1. Remove P-03 from the case before response completion. 2. Inspect the case, unit, and assignment record. |
| Expected result | The active assignment is removed, P-03 becomes **Available**, and the case returns to **Open** rather than becoming Closed. The case can be queued or reassigned normally. |

### TC-12 - Enforce role-based access to prototype features

| Field | Details |
|---|---|
| Requirement(s) | NFR-S02, NFR-U01 |
| Test level/type | System test; basic authorisation and usability test |
| Risk priority | High |
| Preconditions | The role permissions in Appendix 3 have been completed and implemented. Test accounts exist for a Dispatcher, Department user, and Response Unit user. |
| Test data | One authenticated account for each implemented role. |
| Steps | 1. Sign in as each role. 2. Record the visible pages, tools, and case information. 3. Attempt to open one URL or action that Appendix 3 does not permit for that role. |
| Expected result | Each role sees only its required tools and permitted information. An unauthorised page or action is denied even when requested directly, and the denial does not expose protected case data or technical error details. |

## 7.2 Initial Requirements Traceability Matrix

The matrix links each requirement selected for the initial prototype test cycle to one or more test cases. A requirement marked **Deferred** is intentionally outside the initial executable test scope described in Section 6.1, rather than accidentally omitted.

| Requirement | Requirement summary | Linked test case(s) | Initial coverage/status |
|---|---|---|---|
| FR-01 | Record all required emergency call details | TC-01, TC-02, TC-03 | Positive input, missing input, and preservation during the single-step submission |
| FR-02 | Create a case when the completed call form is submitted | TC-01, TC-03 | Single-step case creation, unique ID, automatic timestamp, and persistence |
| FR-03 | Department management of response-unit details | - | Deferred; not selected in the Section 6.1 core scope |
| FR-04 | Automatically determine priority using department rules | TC-03, TC-04 | Submission integration plus decision-process outcomes and boundaries; final decision data depends on Appendix 1 |
| FR-05 | Route to every relevant department | TC-03, TC-05 | Submission integration, multi-department routing, and exclusion of unrelated departments |
| FR-06 | Assign the first available suitable unit or queue the case | TC-03, TC-06, TC-07, TC-08 | Submission integration, available/unavailable boundaries, and concurrent assignment |
| FR-07 | Show and automatically update department cases | TC-09 | Department separation, new case, and changed case information |
| FR-08 | Maintain valid Open, In Progress, and Closed states | TC-03, TC-06, TC-07, TC-10, TC-11 | Main lifecycle transitions and removal path |
| FR-09 | Synchronise unit availability with assignment and release | TC-06, TC-08, TC-10, TC-11 | Assignment, sign-off, and removal paths |
| FR-10 | Notify the assigned unit on its dashboard | - | Deferred; advanced notification is out of scope in Section 6.1 |
| FR-11 | Permit unit sign-off and close after final sign-off | TC-10 | Partial and final sign-off for a multi-unit case |
| FR-12 | Search historical logs | - | Deferred; historical log searching is out of scope in Section 6.1 |
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

Requirements traceability gives the team evidence that testing is based on the agreed behaviour of the Emergency Dispatch Priority & Coordination System. The matrix makes it possible to check that every high-risk requirement selected in Section 6 has at least one test and that important acceptance criteria have not been missed. It also shows where a requirement is covered by several tests. For example, FR-08 is tested during case creation, assignment, queuing, unit removal, and final sign-off because a case-status defect could occur at any of these transitions. This supports quality assurance by making coverage visible, helping the team prioritise critical tests, and providing a basis for the exit criteria in Section 6.5.

Traceability also helps the team assess and control changes. If a requirement changes, its row identifies the test cases that must be reviewed and rerun. For example, if FR-06 changes from selecting the first available unit to selecting the nearest available unit, TC-06, TC-07, and TC-08 must be updated and used as regression tests. The team can also trace a failed test back to the affected requirement and acceptance criteria, which makes defect reporting and impact analysis clearer. Conversely, a new or changed test with no linked requirement may indicate uncontrolled scope or a requirement that has not been documented.

The matrix should be maintained throughout development rather than treated as a one-time document. When Appendix 1 and Appendix 3 are completed, the final priority test data and role-permission expectations should be added to TC-04 and TC-12. Each test execution should then record its result and any defect ID. Keeping these links current gives the team an audit trail from requirement to test evidence and defect, supports targeted regression testing after code changes, and reduces the chance that a change will introduce an unnoticed failure elsewhere in the system.
