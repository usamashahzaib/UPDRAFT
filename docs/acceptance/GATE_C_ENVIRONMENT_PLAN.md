# Gate C acceptance environment plan — for the implementing agent

Prepares everything Gate C's PASS matrix needs that doesn't already exist in source. Source state:
Fusion server mode + dedicated-server loop + allocator + hardened ticket admission exist;
`DedicatedServer.cs` documents the headless loop; `ServerBackend.cs` the backend contract. Missing:
the Linux build, the client fleet, shaping, and the failure-injection runs.

## 1. Build the Linux headless server

1. Unity → Build Profiles → **Linux Server** target (or `-buildTarget Linux64` CLI).
2. Scene set: the generated Main scene; `OnlineHooks` headless detection handles no-rendering.
3. Build script: extend the existing editor build helpers with a `BuildLinuxServer()` method;
   record the binary's SHA-256 in the gate evidence.
4. Verify headlessness: run with `-batchmode -nographics -logFile -` and confirm no GL init
   errors; `NetVisuals` already no-ops in batch mode.

## 2. Topology and ports

- Backend: `docker-compose up` (Postgres + backend:8080) on the LAN host; create a service API key
  (`GameServer:ApiKey` must be set or ticket validation fails closed — correct).
- Dedicated server: run 1–2 instances on the Linux box/WSL2, `--insecure-dev` session join for
  controlled tests is acceptable for Gate C **development evidence only** (spec: insecure dev
  tickets never count as production acceptance; record which mode every run used).
- Six Android clients on the reference devices (or 5 devices + 1 editor player for the first
  matrix pass, then all-6 real phones).

## 3. Network shaping matrix

Linux `netem` on the server interface (or a shaping box between):
```
# 30ms:  tc qdisc add dev eth0 root netem delay 30ms 2ms
# 80ms:  tc qdisc change dev eth0 root netem delay 80ms 5ms
# 150ms: tc qdisc change dev eth0 root netem delay 150ms 8ms loss 1%
# jitter/loss cases: delay 80ms 20ms ; loss 2%
```
Each configuration: full 3v3 match, capture bandwidth via the server process or `iftop`, compare
against the 100 KB/s/client budget. Include one pad-heavy scripted fight (repeated pad
place/launch cycles with 6 players) — that's the budget's stated worst case.

## 4. Failure-injection runbook (each: reproduce → capture server log + client logs → record)

1. Kill one client mid-fire, mid-launch, mid-capture, during respawn countdown (disconnect → 5s →
   labeled bot substitution; verify same avatar on reconnect, no state refresh).
2. Duplicate join: same account joins twice concurrently → second rejected (backend admission).
3. Duplicate result submission: server retries result POST → exactly-once progression (tests cover
   backend logic; verify end-to-end once).
4. Server crash (SIGKILL the server mid-match) → clients surface "match ended, nothing lost", no
   invented winners, allocator lease expires and cancels without participation.
5. Backend outage (stop the backend container mid-queue) → explicit failure states, retry loop,
   no stranded queue.
6. Wrong clock (device clock +10 min) → admission unaffected (server-owned facts).
7. Repeated matches back-to-back ×3 → no leaks (pad count, object count, memory).

## 5. Evidence checklist for STEP_C.md closeout

Per run: build hash (server + client), start/end timestamps, topology, shaping config, six client
IDs (device models), measured bandwidth per client, RTT target vs achieved, pass/fail per
injection case with log excerpts, known-issues updates. PASS requires every matrix cell green and
the bandwidth budget met in the pad-heavy case.
