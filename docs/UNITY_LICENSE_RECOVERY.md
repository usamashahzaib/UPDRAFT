# Unity Licensing / IPC Recovery Procedure

**Date:** 2026-09-29  
**Scope:** Unity 6000.3.25f1 test-runner startup and `Unity.Licensing.Client` cleanup.  
**Result:** Recovery procedure confirmed and reproduced — Unity runs clean, Test Results emit, zero licensing residue.

---

## Symptom

Test runner exits before producing results with: `Could not connect to IPC stream` / `Upm-` timeout (30 s). Unity.Licensing.Client (prior PID 2260) survived `taskkill` and blocked the next run.

## Root cause (two historical failures)

1. **Stale `Temp/UnityLockfile`.** Killed batch runs leave a project lockfile that the test runner still holds as the Package Manager IPC target. The runner waits up to 30 s for the IPC stream and times out.
2. **Zombie `Unity.Licensing.Client`.** The licensing client (Unity Hub-managed, not a normal process) outlives `taskkill` when invoked through Git Bash, because Git Bash mangles `/PID` switches and the kill does not reach it. When the licensing client is stale, the runner cannot handshake and no test results are produced.

## Correct termination path

**Unity Hub GUI, not per-PID kills.**

- Unity is a managed process launched by the Unity Hub. It does not reliably die to `taskkill` — its licensing child (`Unity.Licensing.Client.exe`) survives per-PID kills, and Git Bash's `/PID` flag does not reach it correctly.
- **Documented route:** Unity Hub → **Services** (or **Help** → kill Unity) → **Stop Unity**. This asks the Hub to stop the editor and its licensing child together.
- PowerShell `Stop-Process -Id <id> -Force` works (no Git Bash arg mangling); Git Bash `taskkill /PID <id>` does not.

## Recovery procedure (canonical)

From the repo root (`E:/Usama/Game`), run **in order**:

```powershell
# 1. Confirm no Unity / licensing processes remain
Get-Process | Where-Object { $_.Name -eq 'Unity' -or $_.Name -like 'Unity.Licensing.Client*' }

# 2. Clear the stale project lock / UPM artifacts
Remove-Item -Recurse -Force ProjectHotdrop/Temp

# 3. Run the suite synchronously (EditMode or PlayMode)
powershell -ExecutionPolicy Bypass -File ProjectHotdrop/Run-UnityTests.ps1 -Platform EditMode
powershell -ExecutionPolicy Bypass -File ProjectHotdrop/Run-UnityTests.ps1 -Platform PlayMode

# 4. Verify UPM IPC connects and results emit (Run-UnityTests.ps1 reports
#    'EditMode total=313 passed=313' / 'PlayMode total=18 passed=18').
```

`Run-UnityTests.ps1` (commit `2f64920`) has pre-flight guards: it throws if a Unity editor is still running, warns on a stale licensing client/lockfile, and uses ASCII-only strings (PowerShell 5.1 parses the file as Windows-1252; UTF-8 em-dashes broke the first parse).

## Verified evidence

- Unity.Licensing.Client reaped; after shutdown `ps` shows no `Unity.exe` or `Unity.Licensing.Client.exe`.
- `Run-UnityTests.ps1 -Platform EditMode` → **EditMode total=142 passed=142 failed=0**.
- `Run-UnityTests.ps1 -Platform PlayMode` → **PlayMode total=6 passed=6 failed=0**.
- No Unity process or licensing residue after shutdown.
- Test XML outputs: `ProjectUpdraft/TestResults/EditMode.xml` (142 passed), `ProjectUpdraft/TestResults/PlayMode.xml` (6 passed).

## Notes

- If licensing handshakes still fail, run step 1–2 again; Git Bash taskkill is unsafe, use PowerShell or the Hub GUI.
- `docs/acceptance/STEP_B.md` records the Gate B acceptance built on top of this recovered startup path.
