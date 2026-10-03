# Oracle deployment checkpoint — 2026-09-30 (updated 2026-10-01)

Resume from here in a new session. Read this whole file before doing anything.

## Update — 2026-10-01

Steps 1–4 below are done. Summary, details after the original note:

- **Fixed the real compile error** in `BuildLinuxServer.cs:25`: `subtarget = (int)StandaloneBuildSubtarget.Server` is correct for Unity 6000.3.25f1 (confirmed against the installed `UnityEditor.CoreModule.dll` build error, not a guess). The "same error after removing the cast" from yesterday was a stale `Library/Bee/artifacts` cache serving an old diagnostic, exactly as suspected below.
- Also found and fixed the real cause of apparent "Unity crashes" during batchmode builds: a stale `Library/ilpp.pid` pointed at a dead process. It isn't clear this was strictly required (a later crash-free build happened after removing it), but it's gone now and cost nothing to remove. Separately, **the "crashes" during this session's early attempts were a red herring**: `Unity.exe` + `UnityCrashHandler64` were still running normally (confirmed via `Get-Process`/CPU climbing) — the build tool call was just returning before Unity actually finished (batchmode compiles can take several minutes). Lesson for next time: poll `Get-Process -Id <pid>` until it actually exits before concluding anything crashed.
- Added the same pre-flight guards from `Run-UnityTests.ps1` into `Build-LinuxServer.ps1` (single-owner check, stale-lockfile warning, `HOTDROP_CLEAR_BEE_CACHE=1` opt-in cache wipe — default is now to NOT wipe the cache, since a full wipe isn't actually needed once the cache itself isn't stale).
- **Fixed the UDP port problem** this file flagged in step 4 below: `NetSession.StartRunner` never set `StartGameArgs.Address`, so Fusion bound an OS-assigned ephemeral port — unpredictable for firewall rules. `NetSession.cs` now sets `Address = NetAddress.Any(port)` for `GameMode.Server`, fixed at **UDP 27015** by default, overridable via `HOTDROP_SERVER_PORT` env var. Unity EditMode: 194/194 still pass.
- Rebuilt `UpdraftServer.x86_64`/`UnityPlayer.so` with the fix (SHA-256 unchanged from 2026-09-29: `C4361B92A96F764C885510343418F0A584DFD2592FB4F097ECB6E493D2419101` / `F20FA0E5584C2BA7EA821390B26D371571A519C695776AECCD70F8A3C607DAF2` — expected, native engine binary is Mono-backend and doesn't embed C# changes; `UpdraftServer_Data/Managed/Updraft.Network.dll` SHA-256 `9EA254DA0A12948CD8982658C9677D6217660C3613AD8653BCC6BC292426D6F9` and `Updraft.Runtime.dll` SHA-256 `A85114FA21AF8D065714F01D170786321786768F8CD17BD10795214146416460` do carry the fix — verified by byte-searching the compiled DLL for the new `HOTDROP_SERVER_PORT` string).
- **Transferred to server 2** (`game-match-server`, `141.145.146.32`) via `tar` over SSH to `~/updraft/Builds/Linux/`; remote SHA-256 of the launcher and `UnityPlayer.so` match the local build exactly.
- **Smoke-tested it on server 2**: `./UpdraftServer.x86_64 -batchmode -nographics -updraftServer -devSession smoketest -devHumans 1 --insecure-dev -logFile smoke.log`. It ran clean — no missing shared libs (`ldd` found everything), connected outbound to the Photon Fusion cloud relay using the existing `PhotonAppSettings.asset` app ID (`a91a9f6e-7470-44fb-a32d-de6db0a910ef` — already configured, not a placeholder), held steady ~60 FPS, and `ss -ulpn` confirmed it bound `0.0.0.0:27015/udp` exactly as configured. This is not a Gate C acceptance run (no real client connected, no backend-matched admission, no six-client matrix) — just confirms the binary runs correctly on the actual target host.
- **UDP 27015 is open end-to-end**: host `iptables` (`INPUT` chain, persisted) AND the Oracle Cloud Security List (`game-server-network` → Default Security List → ingress rule `0.0.0.0/0` UDP 27015, added via the console 2026-10-01). Both layers confirmed open.
- (Correction: the `Shader.Find`/`AudioClip:SetData` warnings noticed in the smoke-test log are a one-time ~29-line burst from the one spawned player's audio-cue warmup and cosmetic material setup, not per-frame spam. Not an issue.)
- Fixed a real bug found while reading the smoke-test log closely: `DevFileLog.cs` called `Time.realtimeSinceStartup` inside `Application.logMessageReceivedThreaded`, which Photon's own background networking threads (region pinger) also trigger — main-thread-only API from a background thread throws `UnityException` on every such log line. Replaced with a `Stopwatch` (thread-safe). Unity EditMode still 194/194.
- **Backend is now reachable from the match server over the private network.** `deploy/docker-compose.yml`'s `backend` service now publishes `10.0.0.59:8080` (bound to the private IP only, never `0.0.0.0` — see the comment in the compose file for why) instead of being `expose`-only/unreachable. Host `iptables` on `game-updraft-server` opened for TCP 8080 from `10.0.0.0/16` only (persisted). Oracle Security List got a matching ingress rule (TCP 8080, source `10.0.0.0/16` — **not** `0.0.0.0/0`, this one is intentionally private-only). Verified end-to-end: `ssh` into the match server and `curl http://10.0.0.59:8080/api/auth/guest` returns `HTTP 200`.
- **Set up free HTTPS for the backend (closes QA-001's biggest blocker).** `game-updraft-server`'s `.env` now has `DOMAIN=145-241-114-4.sslip.io` (sslip.io gives a free DNS name that resolves to an embedded IP — no purchased domain needed) and `ACME_EMAIL=usamashahzaib789@gmail.com`. Started the `caddy` container (already defined in `deploy/docker-compose.yml`, never run before); it obtained a real Let's Encrypt certificate on the first try. Opened TCP 80/443 + UDP 443 in host `iptables` (persisted) and the Oracle Security List (source `0.0.0.0/0` — these are public web ports, meant to be open). Verified: `curl -X POST https://145-241-114-4.sslip.io/api/auth/guest` from the match server returns `HTTP/2 200` with a real guest token.
- **`ProjectUpdraft/Assets/Resources/Config/BackendConfig.asset` now points at this real backend**: `environmentName: stage`, `baseUrl: https://145-241-114-4.sslip.io` (was `dev` / `http://localhost:5000`, unreachable from any real device). This satisfies `BackendConfig.IsConfigured`'s HTTPS requirement for release builds too, not just debug.
- Built a fresh Android dev APK with this config, installed it on the connected Samsung SM-A065F (`R8VXC02QEVB`) via `adb`, cleared app data for a clean first run, and launched it — no crash, UI renders, touches register. Then independently confirmed the *device's own network* (not just server-to-server) can reach the new HTTPS backend: opened `https://145-241-114-4.sslip.io/` in the device's browser and got a clean 404 page (proves TLS handshake + DNS + routing all work from the real phone, not just from inside Oracle's network). The app itself doesn't call the backend until a player taps into online mode from the Home screen — exercising that (and the rest of the real in-game journey: queueing, a match, results) needs a human tapping through the UI, same as the QA checkpoint already noted for U02–U20.
- **Still not done, and intentionally not attempted by this session:** a real backend-matched dedicated-server run needs `HOTDROP_SERVER_KEY` set on the match server to match the backend's `GameServer:ApiKey`, which lives only in `~/updraft/src/deploy/.env` on `game-updraft-server` — a production secret. Per this file's own earlier rule ("never copied back to this machine or into git") and the harness's own credential-handling guard, this session did not read or relay that secret. To actually run a real match: either (a) the user SSHes between the two boxes directly to copy/derive the key themselves, or (b) the user pastes the key value for one-time use (not recommended to paste secrets into chat), or (c) generate a fresh key and wire it on both ends in one step the user runs themselves. Until then, only `-devSession --insecure-dev` (no backend ticket checks, confirmed working) can be used to smoke-test the match server.

## Original note (2026-09-30)

## What exists right now (all free tier, nothing costs money)

**Oracle Cloud account:** tenancy `usama4654`, region **UAE East (Dubai)**.

**Networking (already built, don't redo):**
- VCN: `game-server-network` (10.0.0.0/16)
- Public subnet: `public-subnet` (10.0.0.0/24)
- Internet Gateway: attached, route table has `0.0.0.0/0 → Internet Gateway`
- Default Security List: port 22 (SSH) open to `0.0.0.0/0`. **Ports 80/443/game-port are NOT open yet** — add when needed.

**SSH key:** both files in `E:\Usama\Game\Oracle keys\` (`ssh-key-2026-09-30.key` + `.pub`). Same key pair is authorized on **both** servers below. This folder is now gitignored — never commit it.

**Server 1 — `game-updraft-server`** (backend + database)
- Public IP: `145.241.114.4`
- Shape: VM.Standard.A1.Flex (ARM64), 4 OCPU / 24 GB, Ubuntu 22.04
- SSH: `ssh -i "E:\Usama\Game\Oracle keys\ssh-key-2026-09-30.key" ubuntu@145.241.114.4`
- **Status: working, tested.** Docker + Docker Compose installed. Two containers running under `~/updraft/src/deploy` (docker-compose.yml from Codex):
  - `deploy-postgres-1` — Postgres 16, healthy, all EF Core migrations applied (10 tables).
  - `deploy-backend-1` — .NET 10 backend, built from source, confirmed working: `POST /api/auth/guest` inside the docker network returns `200 OK`.
  - Real secrets (DB password, JWT signing key) are in `~/updraft/src/deploy/.env` **on the server only** — never copied back to this machine or into git.
  - Backend is **not exposed to the internet yet** — no port published, no domain, Caddy container defined in compose but never started (needs `DOMAIN`/`ACME_EMAIL` filled in `.env` first — user postponed getting a domain).
  - Source was transferred via `tar` over SSH pipe (not git) — see the command in the "how to redeploy" section below if source changes need pushing again.

**Server 2 — `game-match-server`** (will run the Unity dedicated server)
- Public IP: `141.145.146.32`
- Shape: VM.Standard.E2.1.Micro (x86_64), 1 OCPU / 1 GB RAM, Ubuntu 22.04
- SSH: `ssh -i "E:\Usama\Game\Oracle keys\ssh-key-2026-09-30.key" ubuntu@141.145.146.32`
- **Status: server ready, empty.** 2 GB swap file added (`/swapfile`, already in `/etc/fstab`, survives reboot). No game files transferred yet.
- Same VCN/subnet as server 1, so they can reach each other over the private 10.0.0.0/16 network once the backend's port is exposed there (not done yet).

## Why two servers (don't merge them)

DeepSeek research (verified against the actual repo by Codex) confirmed: **Photon Fusion's native NanoSockets library has no Linux ARM64 build** — only x86_64. The match server binary (`ProjectUpdraft/Builds/Linux/UpdraftServer.x86_64`) is genuinely x86_64-only. So the match server cannot run on the free ARM box (server 1); it needs the separate free x86_64 box (server 2). Confirmed via ELF header inspection of the bundled `NanoSockets/Linux/libnanosockets.so`.

## Where I was stuck (fix this first)

Building the Linux dedicated server locally (`ProjectUpdraft\Build-LinuxServer.ps1`) fails to compile:

```
Assets\Scripts\Editor\BuildLinuxServer.cs(25,29): error CS0266:
Cannot implicitly convert type 'UnityEditor.StandaloneBuildSubtarget' to 'int'.
```

This is odd: I tried it two ways and got the **same error both times**:
1. Original: `subtarget = (int)StandaloneBuildSubtarget.Server,` → this error
2. My fix attempt: `subtarget = StandaloneBuildSubtarget.Server,` (no cast) → **same error, same line/column**

That's contradictory for a real type mismatch (removing a cast should change the error, not reproduce it identically), which suggests one of:
- Unity's Bee/Tundra incremental build cache (`Library/Bee/artifacts/`) served a stale cached diagnostic instead of recompiling — try deleting `ProjectUpdraft/Library/Bee` (or at least the `artifacts` subfolder) before rebuilding, or do a `-executeMethod` build after a full domain reload.
- Or the actual current `BuildPlayerOptions.subtarget` type in Unity 6000.3.25f1 is neither plain `int` nor bare `StandaloneBuildSubtarget` — check `UnityEditor.BuildPlayerOptions` in the installed Editor's API docs/IntelliSense directly rather than guessing again.

**A build from yesterday (2026-09-29) already exists** at `ProjectUpdraft/Builds/Linux/UpdraftServer.x86_64` and works, but it predates today's security fixes (silent-aim fix in `NetPlayer.cs`, AFK-removal fix in `NetMatch.cs`/`NetSession.cs`/`OnlinePlay.cs`/`NetClientPresenter.cs`). Don't ship that build for anything real — rebuild first.

Current file state (already edited, on disk): `ProjectUpdraft/Assets/Scripts/Editor/BuildLinuxServer.cs` line 25 reads `subtarget = StandaloneBuildSubtarget.Server,` (no cast). Verify this compiles cleanly in the Unity Editor GUI directly (open the project, let it compile, check the Console) before trying another batch build — that sidesteps whatever the Bee cache issue is.

Also: twice a batchmode build left a **hung `Unity.exe` process** holding the project lock after a compile error (the `-quit` flag didn't fire). Before any new build attempt, check `tasklist | grep -i unity` first and kill leftover processes + delete `ProjectUpdraft/Temp/UnityLockfile` if present, same as `Run-UnityTests.ps1`'s own pre-flight guard already does.

## Remaining steps, in order

1. **Fix the BuildLinuxServer.cs compile error for real** (see above), get a fresh `UpdraftServer.x86_64` build with today's fixes included.
2. **Transfer the build to server 2** — same pattern as server 1's source transfer: `tar` the `ProjectUpdraft/Builds/Linux/` folder over an SSH pipe to `ubuntu@141.145.146.32`.
3. **Install any missing runtime deps on server 2** (Unity Linux headless builds sometimes need a few system libs — check the build's own error output on first run).
4. **Open the game's UDP port** in the Default Security List (Networking → VCN → Security) — find the actual port Fusion/`DedicatedServer.cs` uses (check `-region`/session-start args and `NetConfigs`) and open it for `0.0.0.0/0` UDP, same way port 22 was opened for TCP.
5. **Run the dedicated server** on server 2, pointed at server 1's backend (`http://145.241.114.4:8080` — but that's only reachable inside the VCN right now since the port isn't published; may need to publish it or put both on the private 10.0.0.x addresses instead of going over the public internet, which is actually the *better* choice for backend↔match-server traffic — cheaper and more secure than round-tripping over the public IP).
6. **Domain name** — user postponed this. Needed before: (a) Caddy/HTTPS can start on server 1, (b) `BackendConfig.IsConfigured` will accept the URL in a release build (requires `https://`). Revisit when user has picked one.
7. Once backend is reachable and match server runs, do an actual end-to-end online match test (this closes QA-001 from the earlier audit for real, with a live server instead of `localhost`).

## Task delegation note

This session split work three ways: Codex wrote `deploy/Dockerfile` + `deploy/docker-compose.yml` + `deploy/Caddyfile` + `deploy/.env.example` (all still in the local repo, untracked — fine to commit, no secrets in them). DeepSeek did the Fusion/ARM64 research above. Claude (this session) did all live SSH execution. Reuse the same split if picking this back up with the same tools available.
