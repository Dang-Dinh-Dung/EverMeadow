# Test Infrastructure

**Engine**: Unity 6000.3.27f1 (6.3 LTS)
**Test Framework**: Unity Test Framework 1.6.0 (+ Input System 1.20.1 test fixture for PlayMode)
**CI**: `.github/workflows/tests.yml`
**Setup date**: 2026-10-31

## Directory Layout

Unity compiles only `Assets/` and `Packages/`, so unit and integration tests live under the engine test root,
not under `tests/`. A test placed in `tests/` is never compiled and a run reports **0 tests, Passed** — a false pass.

```
Assets/Tests/
  EditMode/[System]/   # Unit tests — pure C# cores (FrameClock, Input core, Controller core). No Play Mode.
  PlayMode/[System]/   # Integration tests in a running player loop — Unity adapters, Input System fixture,
                       #   engine-assumption checks (ADR-0001 V1–V10, Physics2D for the Controller)
tests/
  smoke/               # Critical path list for the /smoke-check gate
  README.md
```

```
production/qa/
  evidence/            # Screenshots and manual test sign-off records (Visual / UI / Playtest evidence)
```

> **Manual evidence lives under `production/qa/evidence/`, not `tests/`** — that is where every consumer reads it.

### Assemblies (ADR-0001 convention `Evermeadow.<Layer>.<System>[.Unity]`)

| Test assembly | References | Notes |
|---|---|---|
| `EditModeTests` | test runner, NUnit, production asmdefs under test (e.g. `Evermeadow.Foundation.Time`, `Evermeadow.Foundation.Input`) | Pure-C# cores use `noEngineReferences`; tests inject a fake clock and a fake input event source |
| `PlayModeTests` | test runner, NUnit, `Unity.InputSystem`, `Unity.InputSystem.TestFramework`, production + adapter asmdefs | `InputTestFixture` for virtual devices (ADR-0001 V6) |

A test assembly cannot see `Assembly-CSharp`: every production system has its own `.asmdef`, and the test
assembly lists it in `references`.

## Running Tests

Use the editor executable (paths from `project.yaml` › `commands`):

```
"D:/Unity/Unity Hub/Unity/Hub/Editor/6000.3.27f1/Editor/Unity.exe" -batchmode -runTests -projectPath . -testPlatform EditMode -testResults test-results/editmode.xml
"D:/Unity/Unity Hub/Unity/Hub/Editor/6000.3.27f1/Editor/Unity.exe" -batchmode -runTests -projectPath . -testPlatform PlayMode -testResults test-results/playmode.xml
```

- Close the Editor first (a second instance cannot open the project).
- Exit **0** = all passed, **2** = a test failed, **1** = compile error (no results file), **3** = run failed,
  **4** = unknown `-testPlatform`. Any non-zero exit is a FAIL.
- Delete the results file before each run so an old file is never read as new.
- Read the `<test-run>` element: `testcasecount="0"` means nothing was compiled or found — **not assessed**, never a pass.
- Do not add `-quit`; `-runTests` exits by itself.
- An Edit Mode run never runs `Assets/Tests/PlayMode/`; `/smoke-check` runs both.

## Test Naming

Unity: class `[System]Tests` in `[System]Tests.cs`, method `[Scenario]_[Expected]`
— `InputBufferTests.cs` → `PressInsideWindow_IsConsumedOnce()`.

Tests must be deterministic (injected clock, no real time, no random seeds), isolated (own setup/teardown), and free
of magic numbers except boundary tests where the exact number is the point. Acceptance-criterion IDs from the GDDs
(e.g. `AC-R14`, `AC-F01`) go in the test's `[Description]` or a comment so coverage can be traced.

## Story Type → Test Evidence

| Story Type | Required Evidence | Location |
|---|---|---|
| Logic | Automated unit test — must pass | `Assets/Tests/EditMode/[System]/` |
| Integration | Integration test OR playtest doc | `Assets/Tests/PlayMode/[System]/` |
| Visual/Feel | Screenshot + lead sign-off | `production/qa/evidence/` |
| UI | Retained screenshot of each screen touched | `production/qa/evidence/` |
| Config/Data | Smoke check pass | `production/qa/smoke-*.md` |

## CI

`.github/workflows/tests.yml` runs Edit Mode and Play Mode suites with `game-ci/unity-test-runner@v4` on every push to
`main` and on every pull request. It needs the `UNITY_LICENSE` repository secret (configure manually) and a GitHub
remote — the repo currently has neither and works on `master`, so CI is dormant until a remote with a `main` branch exists.
A failed suite blocks merging.
