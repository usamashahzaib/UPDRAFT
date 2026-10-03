# CI setup — Unity EditMode + PlayMode on every push

[`.github/workflows/unity-tests.yml`](../.github/workflows/unity-tests.yml) runs both Unity suites on
every push (and on manual dispatch) and archives `ProjectHotdrop/TestResults/<Platform>.xml` plus
`<Platform>.log` as workflow artifacts.

It targets the **self-hosted Windows machine that already has Unity 6000.3.25f1 installed and
licensed**, reusing `ProjectHotdrop/Run-UnityTests.ps1` — the same runner that produced every
recorded gate result — so no Unity license secret is needed and the known licensing/`Temp`-lock
recovery steps apply unchanged.

## One-time runner setup

1. Get a registration token: GitHub → repo → **Settings → Actions → Runners → New self-hosted runner**.
2. In an **interactive** PowerShell window on the Unity machine:

   ```powershell
   mkdir C:\actions-runner; cd C:\actions-runner
   # download + extract the latest runner package from the page above, then:
   .\config.cmd --url https://github.com/usamashahzaib/HOTDROP --token <TOKEN> --name hotdrop-unity --labels unity
   .\run.cmd
   ```

3. Leave `run.cmd` running. Runner online = queued pushes start; machine asleep = jobs wait.

Notes:

- The runner must run as the **same Windows user that has the Unity license activated**. Installed
  as a service under a different account, the editor exits at the license check.
- Labels the workflow expects: `self-hosted`, `windows`, `x64` (automatic on Windows) and `unity`
  (from `--labels unity`).
- Prerequisites: Git + Git LFS (checkout fetches the LFS DLLs), and Unity Hub + Unity 6000.3.25f1
  at the default path. If Unity lives elsewhere, change the `-UnityPath` default in
  `Run-UnityTests.ps1`.
- No Unity editor should be open while a run executes; the script fails fast if one is.

## What each run does

1. Checks out the pushed revision with LFS; `clean: false` keeps `ProjectHotdrop/Library` so imports
   are incremental instead of a cold rebuild on every push.
2. EditMode suite → results archived (XML + editor log, 30-day retention).
3. PlayMode suite, only when EditMode passed (both suites compile the same assemblies).
4. Runs are serialized (`concurrency: unity-tests`) and never cancelled mid-run; a killed editor
   leaves a stale `Temp/UnityLockfile` that blocks the next start.
5. `Run-UnityTests.ps1` now exits non-zero when any test fails, so red tests fail the job.

## Reading the evidence

Actions run page → **Artifacts**: `unity-editmode-<run>`, `unity-playmode-<run>`. The XML is the same
NUnit `test-run` document the local runs produce, so counts can be quoted into gate records directly.

Optional README badge:

```markdown
![Unity Tests](https://github.com/usamashahzaib/HOTDROP/actions/workflows/unity-tests.yml/badge.svg)
```

## Troubleshooting

- **"A Unity editor process is running"** — close the editor (or stop `Unity.exe`) on the runner.
  One owner at a time; the script refuses to share the project.
- **Stale `Temp/UnityLockfile` / zombie `Unity.Licensing.Client`** — delete `ProjectHotdrop/Temp` and
  `Stop-Process` the licensing client. Never `taskkill /PID` from Git Bash (it mangles the switches).
- **Job queued forever** — the runner is offline (machine asleep, `run.cmd` closed).
- **PlayMode dies at startup with exit -1** — seen once on 2026-10-01: a crashed batch editor left
  Universal/ILPP/ShaderCompiler processes holding `Temp`. Stop them, delete `Temp`, re-run; the
  editor log is attached to the failed run's artifacts.
- **Machine offline most of the day** — add a `paths:` filter to trim doc-only pushes, or switch the
  job to GitHub-hosted GameCI (`game-ci/unity-test-runner@v4`) with Unity license secrets and an
  image that ships 6000.3.25f1. The self-hosted job stays the reference because it runs the exact
  gate-local procedure.

**Security:** never expose this self-hosted workflow to untrusted code (fork PRs on a public repo);
GitHub recommends hosted runners for those.
