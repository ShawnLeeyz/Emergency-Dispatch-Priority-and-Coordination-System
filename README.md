# Emergency Dispatch Priority and Coordination System

An ENSE707 prototype that records emergencies, applies a consistent priority policy,
routes each case to relevant departments, and assigns the first available response unit.

## Run the GUI

```sh
dotnet run --project Emergency-Dispatch-Priority-and-Coordination-System/DispatchWeb
```

Open the local URL printed by ASP.NET Core. The prototype stores data in memory, so it
is reset whenever the application stops.

## Design

- `Domain` contains encapsulated case, unit, and department state.
- `Application` contains the dispatch workflow and its interfaces.
- `Infrastructure` supplies replaceable in-memory repositories and notifications.
- `DispatchWeb` is the Razor Pages GUI; it only coordinates HTTP input/output.

The current priority rules are intentionally isolated behind `IPriorityStrategy`, and
repositories/notifications are interface-backed, so later database, dashboard, or
department-specific implementations can be added without changing dispatch workflow code.
