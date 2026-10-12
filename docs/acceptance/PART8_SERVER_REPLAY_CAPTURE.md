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
When a server tick runs longer than the 100 ms sample interval, capture emits at
most one current-state sample and retains only the fractional interval
remainder. It does not burst out duplicate current-state samples to fill missed
intervals.

## Verification

- An earlier `--no-restore` runtime build could not start because its generated
  assets file was absent. A later restore-enabled Editor project build compiled
  the Runtime and Editor C# sources with 0 errors and 41 existing Runtime
  warnings.
- No focused `ServerMatchReplay` test exists in the current test tree; tests
  were not run.

## Unmet

- Persistent post-match review, kill-cam/highlight playback, and anti-cheat
  tooling are not implemented. The design material does not define replay data
  ownership, reviewer access, or retention. The in-memory capture is destroyed
  with its match object; extending its lifetime or uploading it requires those
  rules to be specified and approved.
- Six-client server capture, real-time performance, and device acceptance were
  not run.
- Sampler runtime behavior, six-client capture, performance, and device
  acceptance remain unverified; the successful generated-project build proves
  C# compilation only.
