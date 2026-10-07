# Emergency Dispatch Priority and Coordination System

An ENSE707 ASP.NET Core Razor Pages prototype for recording emergency cases, calculating priority,
routing cases to selected departments, assigning nearby response units, and tracking each case until
all required units have signed off.

## Run the prototype

From the repository root:

```powershell
dotnet run --project .\Emergency-Dispatch-Priority-and-Coordination-System\DispatchWeb
```

Open the local URL shown by ASP.NET Core. Runtime data is stored in
`Emergency-Dispatch-Priority-and-Coordination-System/DispatchWeb/Data/dispatch.db`. The AES key used
by this local prototype is stored beside it as `dispatch.db.key`. Keep both files together when moving
the prototype and do not commit either file.

The application seeds fake demonstration accounts into SQLite on first use. Representative accounts
are `dispatch01` / `dispatch-demo`, `medical01` / `department-demo`, `med01` / `unit-demo`, and
`admin` / `admin-demo`. Passwords are stored as salted PBKDF2 hashes, not plaintext. These shared
demonstration passwords and the prototype Admin role are not a production authentication design.

## Main workflow

1. A dispatcher records caller, incident, location, coordinates, severity, and required departments.
2. The system creates the case, applies department keyword rules, and uses reported severity when no
   keyword matches. An authorised dispatcher may override the result with a mandatory reason.
3. The case appears only on selected department dashboards. The nearest available compatible unit is
   assigned using Haversine distance; uncovered work remains in the waiting queue.
4. Assigned response units see their incident and travel distance, then sign off individually.
5. A released unit is offered to the oldest compatible waiting case. The original case closes only
   after every required department response has signed off.
6. Dispatchers can search history and open a printable case report. Important workflow actions are
   retained in the persistent audit log.

Dashboards reload every five seconds. Cases, units, assignments, notifications, accounts, priority
overrides, and audit events survive application restart. Selected sensitive fields are encrypted at
rest using AES-GCM.

## Roles

- Dispatcher: record cases, monitor current work, override priority, search history and view reports.
- Department: view only cases routed to its department and manage its response-unit details.
- Response unit: view and sign off only its assigned response work.
- Admin: inspect all prototype interfaces and the audit log for testing and demonstration.

Access is checked on the server using an HTTP-only authentication cookie, role claims, scope claims,
middleware, and case-report scope checks. Hiding a navigation link is not treated as authorisation.

## Architecture

- `Domain` owns cases, lifecycle rules, assignment history, units and availability.
- `Application` coordinates dispatch through `DispatchService` and repository interfaces.
- `Logic` contains replaceable priority and nearest-unit assignment strategies.
- `Infrastructure` contains SQLite and in-memory test repository implementations.
- `DispatchWeb` contains Razor Pages, authentication, role interfaces and printable reporting.
- `Test` contains MSTest unit, integration, database, security, performance and HTTP tests.

## Build and test

```powershell
dotnet build .\Emergency-Dispatch-Priority-and-Coordination-System\Emergency-Dispatch-Priority-and-Coordination-System.slnx --warnaserror
dotnet test .\Emergency-Dispatch-Priority-and-Coordination-System\Emergency-Dispatch-Priority-and-Coordination-System.slnx --no-build
```

GitHub Actions repeats restore, warning-free build, the full regression suite and a dependency
vulnerability check for pushes and pull requests to `main`. Final Assignment 2 evidence is in:

- `docs/final-test-and-release-evidence.md`
- `docs/final-defect-register.md`

## Prototype limitations

Routing is internal visibility and coordination, not an external emergency-service connection.
Coordinates are entered manually, dashboard updates use polling, the encryption key is a local file,
and production identity, key management, monitoring, load testing, privacy review, accessibility
validation, and operational safety certification are outside this university prototype.
