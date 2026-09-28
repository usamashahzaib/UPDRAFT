# Gate D recruitment & facilitation kit — for the publisher

Everything the human owner needs to run the §25 pad A/B study. The study IS Gate D; without this,
the project correctly stalls. Budget per master plan: $50–150 total incentives.

## 1. Recruiting (30–50 non-friend testers)

Target: adult (16+ per rating, but recruit 18+ for consent simplicity) mobile-shooter players in
Pakistan/India/SEA communities (WhatsApp/Telegram/Discord gaming groups, university clubs).

Recruitment message (copy/adapt):

> Looking for Android players to test a new 3v3 arena shooter (3-minute matches). You get: early
> access, a small cash/grab reward (≈PKR 300–500 / ₹150–250 per completed session block), and your
> feedback shapes the game. Requirements: Android 9+ phone with 3–4GB+ RAM, ~30 min on day 1 and
> ~15 min on days 2 and 8 (three scheduled sessions), stable internet. We record anonymous gameplay
> statistics only — no name, contacts, or chat is ever collected. Interested? Reply with your phone
> model.

Rules baked into the study design (do not soften in the message):
- **Non-friends only.** Anyone you know personally is excluded from the 30–50 and tagged as a
  separate cohort if they play anyway.
- **No rewards for choosing a version.** Compensation is for completing sessions, never for
  picking pads-on vs pads-off (§25 hard rule).
- Recruit 40+ to survive attrition down to the 30–50 analysable band; plan ~15% no-show.

## 2. Consent script (read/send before day 1)

> The game collects anonymous gameplay statistics (match events, performance, a random ID). No
> personal information, contacts, or chat is collected, and the ID cannot identify you. Sessions
> are scheduled so we can measure matchmaking; play during your assigned windows if possible.
> You may stop anytime and request deletion via the in-game account deletion option. By installing
> and playing, you agree to this.

## 3. Cohort assignment (counterbalanced)

Alternating assignment as testers confirm: odd → `study_pads_on`, even → `study_pads_off`.
Log tester code ↔ phone model ↔ cohort ↔ session dates in the roster sheet (keep OFF the devices;
roster lives with the publisher only — data files must never carry names).

## 4. Facilitator panel (on each device, before handover)

1. Install the APK: `adb install -r ProjectUpdraft/Builds/Android/UpdraftDev.apk`.
2. Launch, tap the Home **title 5×** → study panel appears.
3. Tap `VERSION: WITH PADS` or `VERSION: WITHOUT PADS` per the roster (sets `study_pads_on`/`study_pads_off`).
4. Tap `TOGGLE INCENTIVIZED TESTER FLAG` **only** for paid testers — this is mandatory honesty
   telemetry (§24) and lets the data desk exclude incentivized cohorts from KPI gates.
5. Verify the status line shows the right cohort, pads ON/OFF, and event count > 0.
6. Note the tutorial will run on first launch (`TutorialDone` clear if a device is reused).

## 5. Session schedule (three windows, why each matters)

| Session | When | What | Evidence it feeds |
|---|---|---|---|
| S1 | Day 1, evening window | Uncoached tutorial + ≥1 practice match + first public queue attempt | Tutorial completion ≥80%, comprehension, queue times |
| S2 | Day 2 (24–48h) | ~15 min: practice + public matches | **D1 retention** window |
| S3 | Day 8 | ~15 min: same | **D7 retention** window — never estimated earlier |

Scheduled windows are required for the median-queue <45 s KPI (single-region, simultaneous players).

## 6. Data collection (twice weekly, per device — 4 MB queue cap makes this mandatory)

```bash
adb pull /sdcard/Android/data/com.DefaultCompany.ProjectUpdraft/files/analytics_queue.jsonl gate_d/<testercode>_<yyyymmdd>.jsonl
```

Keep pulls ≥2/week so nothing evicts. Deliver files to the data desk unchanged; do not rename
events or merge files. Raw server-side aggregates for public matches come from the backend export
when available.

## 7. What the facilitator observes (no coaching allowed)

Note per S1 session, without prompting: did they finish the tutorial unskipped? Did they place a
pad unprompted? Did they use/steal an enemy pad? Ask afterwards: "explain what pads do" (verbatim),
"which version would you replay?" Record answers in the roster — these are the §25 uncoached
comprehension and preference measures the data desk needs alongside telemetry.

## 8. Device matrix assignment (four tiers, §23)

One device per tier minimum, run the 20-min thermal session once: low (≤3 GB, e.g. the Samsung
A06), mid alt-GPU (~4 GB), current midrange, 16 KB-page device. Record model/OS/session length;
the data desk computes FPS/thermal/memory vs budgets from telemetry + observed logs.
