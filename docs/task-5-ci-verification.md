# Task 5: CI and DevOps QA verification

Investigation and local verification: 13 October 2026 (Pacific/Auckland).
Branch: `CI-Test-Branch`. Inspected HEAD: `bee3eea3af8a15d0cab904303c4c32c61b065e9f`.
The fix is an uncommitted local change; no commit, push or merge was performed.

## Confirmed root cause

There were two workflow files:

| Path relative to repository root | Before the fix | GitHub Actions discovery |
| --- | --- | --- |
| `.github/workflows/ci.yml` | `windows-latest` | Active workflow |
| `Emergency-Dispatch-Priority-and-Coordination-System/.github/workflows/ci.yml` | `ubuntu-latest`, checkout into `src` | Ignored nested copy |

Commit `4448f18` moved the workflow to the repository root. Commit `75fb275`
reintroduced the nested copy, and `09734a0`, `c48bcfb`, `e9bb4fb` and `bee3eea`
modified only that nested copy. The active root workflow remained unchanged.
[GitHub discovers workflows at the repository root](https://docs.github.com/en/actions/concepts/workflows-and-actions/workflows).

This is confirmed remotely, rather than inferred from the local YAML:

- [Push run 38321952738](https://github.com/ShawnLeeyz/Emergency-Dispatch-Priority-and-Coordination-System/actions/runs/38321952738)
  used `.github/workflows/ci.yml`, branch `CI-Test-Branch`, HEAD `bee3eea`, attempt 1.
- [Job 115051848085](https://github.com/ShawnLeeyz/Emergency-Dispatch-Priority-and-Coordination-System/actions/runs/38321952738/job/115051848085)
  was named `build-and-test-windows`; its logs identify Windows Server 2025,
  image `windows-2025-vs2026` and Git for Windows.
- Checkout failed while creating
  `Emergency-Dispatch-Priority-and-Coordination-System/Emergency-Dispatch-Priority-and-Coordination-System/Emergency-Dispatch-Priority-and-Coordination-System.csproj`.
  This relative path is 162 characters; combined with the logged checkout root
  `D:\a\Emergency-Dispatch-Priority-and-Coordination-System\Emergency-Dispatch-Priority-and-Coordination-System`
  and a separator, the absolute path is 271 characters, exceeding the classic
  Windows 260-character limit. The log explicitly reports `Filename too long`.
- Restore, build and MSTest were skipped. The upload step found no files;
  its success did not represent successful testing or a saved test artifact.
- The latest pull-request run, `38321957128`, also used the Windows workflow
  at the same HEAD. Both are attempt 1, so these failures were fresh runs,
  not evidence of an old rerun retaining its configuration.

## Fix

The root `.github/workflows/ci.yml` now uses `ubuntu-latest` and Bash.
The ignored nested copy is removed to give future edits one authoritative location.
No solution or application project was renamed.

Push CI is restricted to `CI-Test-Branch`. Pull-request CI retains its existing
`main` target filter; checking a pull request does not modify or merge `main`.
Checkout uses the event ref by default, preserving normal pull-request merge testing.
The workflow records both the event SHA and actual checked-out SHA, which can differ
on pull-request events.

The job installs .NET `10.0.x`, restores the solution, builds all four projects
in Release with `--no-restore`, then runs the MSTest project with `--no-build`.
It verifies that a nonempty TRX exists after successful testing. Test failures
continue to fail the job; the artifact upload uses `always()` to preserve evidence
when earlier steps fail.

## Verified paths and local results

The solution is
`Emergency-Dispatch-Priority-and-Coordination-System/Emergency-Dispatch-Priority-and-Coordination-System.slnx`.
Its four entries are `ConsoleApp/ConsoleApp.csproj`,
`Emergency-Dispatch-Priority-and-Coordination-System/Emergency-Dispatch-Priority-and-Coordination-System.csproj`,
`DispatchWeb/DispatchWeb.csproj` and `Test/Test.csproj`, relative to the solution directory.
All solution entries and project references were checked and exist.
All projects target `net10.0`; the test project references MSTest `4.0.2`.

Executed from the repository root with local Windows SDK `10.0.401`:

```powershell
dotnet restore Emergency-Dispatch-Priority-and-Coordination-System/Emergency-Dispatch-Priority-and-Coordination-System.slnx
dotnet build Emergency-Dispatch-Priority-and-Coordination-System/Emergency-Dispatch-Priority-and-Coordination-System.slnx --configuration Release --no-restore
dotnet test Emergency-Dispatch-Priority-and-Coordination-System/Test/Test.csproj --configuration Release --no-build --logger "trx;LogFileName=TestResults.trx" --results-directory ./TestResults
```

| Verification | Result |
| --- | --- |
| Dependency restore | Passed |
| Release build of all four projects | Passed; 0 warnings, 0 errors |
| MSTest | 167 passed; 0 failed; 0 skipped |
| TRX XML counters | 167 total, executed and passed; 0 failed, errors or not executed |
| Local result | `TestResults/TestResults.trx` |

The TRX finished at 12:37:13 NZDT on 13 October 2026. Local restore, build
and tests required execution outside the restricted sandbox; the sandboxed
build failed without compiler diagnostics and the sandboxed test run stalled.
These were execution-environment limitations, not changes to the test suite.

## SQA artifacts and remote acceptance checks

The workflow uploads `TestResults/` as
`mstest-results-<run-id>-<attempt>`, with `retention-days: 90`.
It contains `TestResults.trx` when tests produce it, `ci-environment.txt`,
`dotnet-info.txt` and `solution-projects.txt`. If restore or build fails before
testing, environment evidence can still be uploaded; it is not a test-pass report.
Checkout failure may leave no evidence files to upload.
Download artifacts before expiry and retain them with the report. Local
`TestResults/` is intentionally Git-ignored but remains on disk.

**Remote Ubuntu acceptance is pending.** Local Windows tests do not prove Linux
execution or GitHub artifact upload. No new run can test these uncommitted changes.
After explicit permission to commit and push the fix to `CI-Test-Branch`:

1. Use the fresh push run for the new commit. Confirm its SHA and workflow path,
   job `build-and-test-linux`, and Linux/Ubuntu operating system in the setup log.
2. Confirm checkout, restore, Release build, MSTest and TRX verification pass.
3. Download the artifact from the run summary; inspect the TRX counters and
   environment files. Record the actual Ubuntu test totals rather than assuming
   they equal the local totals.
4. Capture the run summary, operating system, test totals and artifact for Task 5.

[Rerunning an old run retains its original event SHA and ref](https://docs.github.com/en/actions/how-tos/manage-workflow-runs/re-run-workflows-and-jobs).
It is therefore not an acceptance test for newly committed workflow changes.
The earlier report note in `final-test-and-release-evidence.md` describes CI
as deferred; use this investigation for the checkout cause, and update the
report's final CI status only after a successful fresh Ubuntu run.
