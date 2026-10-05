# Offline travel — real-device acceptance checklist

Status: **NOT RUN / NOT ACCEPTED**. This is a plan for the connected phone, not evidence of a pass. No gate or production flag is authorized by this checklist.

Use `docs/audit/OFFLINE_TRAVEL_IMPLEMENTATION_2026-10-03.md` for the exact APK/source hash, test logs and known limitations. The development APK is not a Play release or publisher-signing artifact. Record the actual package/version, APK SHA-256, phone model, Android version, display resolution/cutout, refresh rate and test date before testing. Record results as PASS / FAIL / NOT RUN with screenshot/video/log references; leave unexercised rows NOT RUN.

## Safe setup

1. Enable USB debugging and approve the connected computer. Confirm the intended device serial before installation; do not install to an arbitrary connected device.
2. Preserve existing app data/progress. Do not uninstall, clear app data or induce destructive corruption without explicit approval of that test scope. A clean first-run test may require a separate device/profile or an approved fresh-data run.
3. Install only the hash-identified fresh development APK. Confirm its package/build identity on device. Keep Shop, ads and other gated systems disabled.
4. Capture the full screen, including touch controls. Editor camera renders do not capture the immediate-mode touch overlay and cannot substitute for these screenshots.

## Connected journeys

| Scenario | Required observation | Result / evidence |
|---|---|---|
| First run | Startup, body choice, nine-step tutorial, Home, settings and choice survive relaunch. | NOT RUN |
| Offline entry | Airplane mode; Home → Play Offline → Library → Detail → actual gameplay, without account or streamed content. | NOT RUN |
| Standard Relay | Full offline Relay, actual match end, result, rematch, Home; challenge world is cleaned up. | NOT RUN |
| Navigation | Detail Back, pause Library, Retry, Next, settings, tutorial and Relay do not overlap actors/menus or leave a frozen world. | NOT RUN |
| Input/layout | All controls usable on 16:9 and 20:9; actual cutout, larger text, left-handed controls and Reduce Motion; no crosshair/thumb-zone collision. | NOT RUN |
| Settings | Changes persist after background/relaunch; mid-drill control-context change cannot create a misleading Classic best. | NOT RUN |

## All twelve authored challenges

Do not use teleports, synthetic completion callbacks or accelerated match clocks for this human/device run. Record completion time, touch/aim comfort and any confusing objective wording. Completion earns Bronze; Silver/Gold thresholds remain unvalidated and disabled.

| Challenge | Required device check | Result / evidence |
|---|---|---|
| Basic Route | Grounded checkpoints in sequence; wrong-order/airborne arrival does not advance. | NOT RUN |
| Own-Pad Route | Actual owner-pad activation and qualifying touchdown; walking into the landing target is insufficient. | NOT RUN |
| Enemy-Pad Route | Actual opponent-pad activation and touchdown; owner-pad use is insufficient. | NOT RUN |
| Safe Landing | First actual pad-flight touchdown inside the 2 m marked landing target; late walking/sliding does not complete. | NOT RUN |
| Stationary Targets | Real damage eliminates all four targets; retries do not retain old target state. | NOT RUN |
| Moving Targets | Three real moving targets, readable patrols and usable touchscreen aim. | NOT RUN |
| Reload Discipline | All targets plus a real completed reload; attempted/cancelled reload alone is insufficient. | NOT RUN |
| Launch and Hit | Two actual airborne pad-flight target eliminations; grounded hits and unrelated jumps are rejected. | NOT RUN |
| Capture One Zone | Fifteen valid seconds; protection, excess feet height or leaving the zone does not count. | NOT RUN |
| Rotate Between Zones | Valid occupation in scheduled Center/West/East windows; inactive zones do not count. | NOT RUN |
| Defend Against Bots | Two distinct real easy-bot eliminations by the player inside the active zone; no disabled AI. | NOT RUN |
| Complete Offline Relay | Unaccelerated real Relay reaches its normal match end, including countdown/overtime when applicable. | NOT RUN |

## Interruption and records

| Scenario | Required observation | Result / evidence |
|---|---|---|
| Same-process pause | Actual offline player, target patrols, bots and match clock stop; resume continues once. | NOT RUN |
| Background during play | No input/callback leak or fabricated success; progress/settings remain coherent on return. | NOT RUN |
| Safe checkpoint process loss | After an actual approved OS process-kill/relaunch, a grounded reconstructable drill offers a working checkpoint resume with consistent context/time/progress. | NOT RUN |
| Unsafe world process loss | Airborne/combat/reload/bot-Relay state explains clean Restart, not a fake exact resume; completed records survive. | NOT RUN |
| Failed drill | No completion medal or improved successful personal best. | NOT RUN |
| Repeated Retry/Next | One result/save transition; no duplicate actors, callbacks or online reward. | NOT RUN |
| Save unavailable / low storage | Controlled, approved failure reports NOT SAVED and preserves the previous commit where feasible; do not fill the owner's phone destructively. | NOT RUN |
| Corrupt primary / backup | Controlled test copy only; valid backup restores, wholly invalid data explains reset without fabricated medals. | NOT RUN |
| Version/context isolation | Legacy records migrate safely; future-schema file is not overwritten; challenge/control/difficulty contexts stay separate. | NOT RUN |
| Offline isolation | No online currency/inventory/progression grant; no Shop/ad/purchase route appears. | NOT RUN |

## Performance and acceptance

Run repeated drills and at least one complete Relay long enough to observe sustained frame time/FPS, visible stutter, thermal throttling, battery/temperature observations and memory where the device tooling permits. State the method and duration; do not invent unavailable metrics. Capture Library, Detail, actual gameplay/touch HUD, Pause, Result, Resume/Restart explanations and representative failure states.

Record remaining defects by player impact. Fixes require a newly identified APK plus proportionate regression tests and a rerun of affected device checks. This offline checklist does not validate six-client online play, final art/licenses, legal/publication identity, Gate D human/retention decisions, billing or production readiness.
