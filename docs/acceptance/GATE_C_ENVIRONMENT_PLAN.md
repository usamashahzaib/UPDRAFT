# Gate C acceptance environment plan — for the implementing agent

Prepares everything Gate C's PASS matrix needs that doesn't already exist in source. Source state:
Fusion server mode + dedicated-server loop + allocator + hardened ticket admission exist
(`DedicatedServer.cs`, `ServerBackend.cs`, `NetSession.cs`). Missing: the
six-client fleet, network shaping, and the failure-injection runs themselves.

Working rules: this plan is preparation for the Gate C implementing agent — no code here is added
to the project by the data desk; the build script below is ready-to-paste and follows the existing
`BuildAndroidApk.cs` conventions. `--insecure-dev` sessions are acceptable for development
evidence only and every run record must state which admission mode it used.

## 1. Linux headless server build

**Prerequisite (Hub):** install module **Linux Build Support (Server)** for Unity 6000.3.25f1
alongside the editor. Verify: Hub → Installs → 6000.3.25f1 → Add modules.

**Done 2026-09-29.** Editor script `Assets/Scripts/Editor/BuildLinuxServer.cs` (menu *Tools → Updraft →
Build Linux Dedicated Server*) plus wrapper `ProjectUpdraft/Build-LinuxServer.ps1` cross-build from
Windows to `Builds/Linux/UpdraftServer.x86_64`; first successful build and hashes are in STEP_C.
Differences from the originally proposed snippet, all required to compile/build on 6000.3:
`BuildPlayerOptions.subtarget` needs `(int)StandaloneBuildSubtarget.Server`; `EnableHeadlessMode`
is dropped (obsolete — the Server subtarget replaces it); `Handheld.Vibrate` in
`TouchInputManager` is compiled only for Android/iOS. Deploy the whole `Builds/Linux/` folder —
the `.x86_64` is a stub launcher next to `UnityPlayer.so` and `UpdraftServer_Data/`.

**Run on the Linux host** (backend mode — the acceptance path):
```bash
chmod +x UpdraftServer.x86_64
export HOTDROP_SERVER_KEY="<service key from GameServer:ApiKey>"   # env only, never argv
export HOTDROP_BACKEND_URL="http://<backend-host>:8080"
./UpdraftServer.x86_64 -updraftServer -serverId gatec-01 -logFile gatec-01.log
```
Server claims composed matches via `/api/servers/claim`, hosts, submits results, repeats
(`DedicatedServer.Run`). Headless check: log shows no GL init; `NetVisuals` no-ops in batch mode.

**Local no-backend dev session** (topology debugging only):
```bash
./UpdraftServer.x86_64 -updraftServer -devSession gatec-dev -devHumans 6 --insecure-dev
```

## 2. Six-client test harness

**Fleet:** 6 Android devices with the dev APK (`adb install -r Builds/Android/UpdraftDev.apk`).
First pass may substitute 1 editor player; the recorded acceptance run must be 6 real phones.
Device serials → roster: `adb devices`.

**Two admission modes, both required in the matrix:**
1. **Backend-matched (primary):** each device launches → Home → queue. Six solos enqueue within
   one composition window → backend composes 3v3 (Δ150 window). This exercises tickets, admission,
   claims, results — the real path.
2. **Dev direct (repeatable timing runs):** client `BackendConfig.devDirectSession = gatec-dev`
   (dev builds only) joins the `--insecure-dev` session directly. Used for bandwidth/shaping cells
   where start-time variance must be zero. Never counts as production acceptance.

**Per-run wrapper** (run from the adb host; log to `TestResults/gatec/`):

```bash
#!/usr/bin/env bash
# gatec_run.sh RUN_ID SHAPING_LABEL -- <serial...>
RUN=$1; LABEL=$2; shift 3
mkdir -p TestResults/gatec/$RUN
echo "run=$RUN shaping=$LABEL start=$(date -Iseconds)" > TestResults/gatec/$RUN/meta.txt
for s in "$@"; do
  adb -s $s shell am force-stop com.DefaultCompany.ProjectUpdraft
  adb -s $s logcat -c
  adb -s $s shell am start -n com.DefaultCompany.ProjectUpdraft/com.unity3d.player.UnityPlayerActivity
done
# ... play the match (manual or scripted), then:
for s in "$@"; do
  adb -s $s logcat -d > TestResults/gatec/$RUN/$s.logcat
  adb -s $s pull /sdcard/Android/data/com.DefaultCompany.ProjectUpdraft/files/analytics_queue.jsonl \
      TestResults/gatec/$RUN/$s.queue.jsonl
done
echo "end=$(date -Iseconds)" >> TestResults/gatec/$RUN/meta.txt
```

**Bandwidth measurement (≤100 KB/s per client budget):** on the Linux server host, sample NIC
traffic to the clients' Wi-Fi subnet during the match:
```bash
sar -n DEV 5 24 | grep -E "eth0"   # 2 minutes @ 5s samples
```
Per-client downlink ≈ server egress ÷ 6 (upper bound; record it that way honestly). The budget's
stated worst case is a pad-heavy fight — run the **pad drill**: all six players place+launch every
~5 s for 2 minutes. Prove pad-heaviness from the server log: `grep -c ServerEvent gatec-01.log`
(DedicatedServer logs every pad event) — require ≥ 60 pad events in the window or the run doesn't
qualify as the pad-heavy cell.

## 3. RTT/loss/jitter shaping (netem, server host only)

Shape **only the server's game traffic**, not the adb LAN or backend HTTP, by filtering the
Fusion UDP port (read the actual bound port from the server log line at startup; substitute below):

```bash
#!/usr/bin/env bash
# netem_apply.sh DELAY_MS JITTER_MS LOSS_PCT [PORT]
D=${1:-0}; J=${2:-0}; L=${3:-0}; P=${4:-27015}; IF=eth0
sudo tc qdisc del dev $IF root 2>/dev/null
sudo tc qdisc add dev $IF root handle 1: prio
sudo tc qdisc add dev $IF parent 1:3 handle 30: netem delay ${D}ms ${J}ms loss ${L}%
sudo tc filter add dev $IF protocol ip parent 1:0 prio 3 u32 \
     match ip protocol 17 0xff match ip dport $P 0xffff flowid 1:3
sudo tc filter add dev $IF protocol ip parent 1:0 prio 3 u32 \
     match ip protocol 17 0xff match ip sport $P 0xffff flowid 1:3
tc -s qdisc show dev $IF   # verify; counters prove shaping is active
```
Cleanup after every session: `sudo tc qdisc del dev eth0 root`.
Requires root on the Linux host — request publisher approval, never shape the Windows/adb host.

**Matrix (each cell = full 3v3 backend-matched match; fill STEP_C with achieved vs target):**

| Cell | delay | jitter | loss | Also exercise |
|---|---|---|---|---|
| C0 baseline | 0 | 0 | 0% | — |
| C1 | 30ms | 2ms | 0% | — |
| C2 | 80ms | 5ms | 0% | — |
| C3 | 150ms | 8ms | 0% | mid-air aim + pad launch usability note |
| C4 | 80ms | 20ms | 0% | jitter worst case |
| C5 | 80ms | 5ms | 1% | reconnect drill mid-match |
| C6 | 80ms | 5ms | 2% | pad drill bandwidth cell |

**Verification honesty:** `tc -s qdisc show` counters are the proof shaping ran; Fusion's own RTT
statistics (server stats page/log) are the proof of achieved RTT. Record both; a cell whose
achieved RTT is far from target is rerun, not explained away.

## 4. Failure-injection runbook (each: reproduce → capture server log + client logcats → record)

1. **Disconnect mid-action:** kill one client (force-stop) during fire, during a pad launch, while
   standing in the capture zone, and during the respawn countdown → avatar persists, labeled bot
   after 5 s, reconnect returns to the same avatar with no state refresh.
2. **Duplicate join:** same account second concurrent connection → rejected by admission.
3. **Duplicate result:** block the server's result POST once (firewall drop), let it retry →
   progression applied exactly once (ledger unique constraint is the backend backstop).
4. **Server crash:** SIGKILL the server process mid-match → clients see the explicit failure state;
   allocator lease expires and cancels reservations without participation (backend regression
   covers this; verify end-to-end once).
5. **Backend outage:** stop the backend container while clients queue → explicit failure states,
   client retry loop, no stranded queue.
6. **Wrong clock:** one device +10 min → admission unaffected (facts are server-owned).
7. **Repeated matches:** ×3 back-to-back backend-matched matches → pad count zero at start, no
   object/memory leak growth across runs (server log + client `startup_budget` deltas).

## 5. Evidence checklist for STEP_C.md closeout

Per run: run ID, shaping cell, admission mode, server build SHA-256, client build version, six
device models/serials, start/end times, `tc` counter snapshot, achieved RTT, per-client bandwidth
(egress ÷ 6) with the pad-drill event count, injection case + result + log excerpt paths under
`TestResults/gatec/<run>/`. Gate C PASS requires: every matrix cell green, bandwidth budget met in
C6's pad-heavy cell, all seven injection cases with captured evidence, repeated-match leak check —
all tied to one frozen source revision. No paid deployment without publisher approval.
