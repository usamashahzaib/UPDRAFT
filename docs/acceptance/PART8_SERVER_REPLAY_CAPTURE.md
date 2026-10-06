# Server replay capture

## Rules applied

- Capture avatar positions and sanitized movement/look/action input at the Bible's
  10 Hz sample rate from the authoritative server simulation.
- Keep captured facts server-owned and separate from client telemetry or hit
  claims.
- Bound the sample list by the configured Relay regulation plus maximum overtime
  duration and the match participant capacity.

## Implementation

`NetMatch` owns a `ServerMatchReplay` for state-authority matches. During live
play, after authoritative movement, shot resolution, elimination/respawn, and
absence handling, it samples each participant's position, velocity, alive state,
weapon, shot counter, sanitized movement/look input, button bits, and weapon
selection, plus authoritative hit and headshot confirmation counters. These
counters make later server-side accuracy analysis possible without trusting
client claims. The data is not replicated to clients or uploaded to analytics.

## Verification

- `dotnet build ProjectHotdrop/Hotdrop.Runtime.csproj --no-restore -v:q` succeeds
  with 0 warnings and 0 errors.
- Tests were not run.

## Unmet

- Persistent post-match review, kill-cam/highlight playback, and anti-cheat
  tooling are not implemented. The design material does not define replay data
  ownership, reviewer access, or retention. The in-memory capture is destroyed
  with its match object; extending its lifetime or uploading it requires those
  rules to be specified and approved.
- Six-client server capture, real-time performance, and device acceptance were
  not run.
