# Emergency Dispatch Priority and Coordination System

An ENSE707 ASP.NET Core Razor Pages prototype for recording emergency calls, creating cases,
determining prototype priority, routing work to Medical, Police, and Fire departments, and
coordinating in-memory response units.

## Run the prototype

From the repository root:

```powershell
dotnet run --project .\Emergency-Dispatch-Priority-and-Coordination-System\DispatchWeb
```

Open the local URL printed by ASP.NET Core. All cases, unit changes, assignments, and
notifications are stored in memory and reset when the application stops.

To build and run the automated regression tests:

```powershell
dotnet build .\Emergency-Dispatch-Priority-and-Coordination-System\Emergency-Dispatch-Priority-and-Coordination-System.slnx
dotnet test .\Emergency-Dispatch-Priority-and-Coordination-System\Emergency-Dispatch-Priority-and-Coordination-System.slnx --no-build
```

## Current prototype workflow

1. A dispatcher records caller, incident, location, description, and required response details.
2. The system creates the case, applies the isolated prototype priority policy, and routes it to
   each selected department.
3. The first available unit in each department is assigned. Uncovered department work remains
   visible in a deterministic first-in waiting queue.
4. Assigned units sign off individually from their department dashboard. Only that unit is
   released, and it is immediately offered to the oldest compatible waiting case.
5. The case remains In Progress while another response is active, returns to Open when incomplete
   work has no active unit, and closes automatically after all required department responses sign off.

Department dashboards and the dispatcher dashboard poll every five seconds. Unit details can be
updated at prototype level, while availability remains controlled by assignment and sign-off.
History search uses OR semantics across caller name, case ID, and recorded date.

## Architecture

- `Domain` owns case lifecycle, assignment history, unit availability, and department state.
- `Application` coordinates case creation, routing, deterministic assignment, queue retry,
  sign-off, notifications, and unit updates through interfaces.
- `Infrastructure` provides the in-memory repositories and notification store.
- `Logic` contains the replaceable priority and unit-assignment policies.
- `DispatchWeb` contains the Razor Pages operational interface.
- `Test` contains focused MSTest workflow regressions.

## Priority limitation

The repository report names Appendix 1 as the location for Medical, Police, and Fire
priority/severity activity diagrams, but the diagrams and their decision rules are not present.
`KeywordSeverityPriority` therefore remains an explicitly isolated prototype placeholder and
must not be treated as a real emergency-service decision process.
