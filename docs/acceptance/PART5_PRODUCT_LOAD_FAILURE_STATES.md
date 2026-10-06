# Part 5 product-screen load-failure, stale, and terminal-result states

Spec inputs: `docs/design/00_PILLARS.md` (honest network state, server-authoritative
outcomes), `docs/design/01_LIFECYCLE.md` (Offline/server down: no offline match facts
presented as server-verified rewards; never show a false state), `02_ART_TOKENS.json`
(48 dp touch target, existing type roles). Support is recorded separately in
`PART5_SUPPORT_ACCOUNT_LOAD_STATES.md`.

## Audit findings fixed

| Screen | Gap found | Change |
|---|---|---|
| Profile | On a profile, wallet, or match-record failure, the shared `Retry()` card was added to menu rows that the full-panel profile visual covers. The player saw "ACCOUNT SYNCING / Account data is loading." with no reachable retry. | The profile footer shows a 52 px RETRY action (with icon) next to EDIT APPEARANCE only after a failed load. Unloaded values read "unavailable" (alias, account line, progress, loadout slots, record) instead of "loading/syncing". The status text width shrinks to make room. The brand-new-account empty state (no stats record) is unchanged. |
| Appearance | "INVENTORY UNAVAILABLE … Retry to confirm ownership." had the same hidden retry. | The footer shows a 52 px RETRY action only while the inventory is unavailable, and it re-runs the existing inventory load. |
| Results (online) | A cancelled match polls no receipt but still read "MATCH COMPLETE · SERVER VERIFIED", "EARNED PROGRESSION BREAKDOWN", and "Waiting for the authoritative match receipt." with no end. Ineligible or unavailable receipts also left the "waiting" note in place. | Cancelled: "MATCH CANCELLED", "PROGRESSION UNCHANGED", and "Cancelled matches change no rating, XP, or credits." Ineligible receipt: "The server recorded this match without XP or credits." Unconfirmed receipt: "This screen could not confirm the receipt. Check your profile later." The pending, verified, and practice paths are unchanged. |
| Party Room | The 3 s watcher ignored refresh errors other than not-found, so a stale room looked current. | On the first failure, the status reads "Party status could not refresh; showing the last known members and readiness. Retrying automatically." It returns to "Party status is current." on the next successful refresh. Leader-queue follow and not-found handling are unchanged. |

## Rules applied

- Use separate states for loading, empty, unavailable, and stale. Never leave a failed request labeled as loading.
- Do not add reward, account, or gameplay rules. Results text only restates existing server receipt fields (`recorded`, `eligible`) and the existing cancelled footer.
- Keep the retry hit area at least 48 dp. Use only the existing caption, UI, and title tokens and semantic A/B/C colors. Reuse existing icon glyphs (`refresh`), because the icon font is a fixed subset.
- Keep BACK / RETURN TO HOME and EDIT APPEARANCE available in every state.
- Balance and config values are untouched.

## Verified this pass

- `dotnet build ProjectHotdrop/Hotdrop.Runtime.csproj --no-restore`: 0 errors, 41 existing warnings.
- `Hotdrop.Network` (contains `NetClientPresenter`): 0 errors. It was compiled through a temporary copy of the generated csproj that adds the untracked `ServerMatchReplay.cs`, because the Unity-generated csproj predates that file. The temporary file was removed.
- `Hotdrop.Tests` and `Hotdrop.PlayModeTests` builds succeeded after a local restore.
- No EditMode/PlayMode tests, Unity Editor captures, or device checks were run.

## Unmet / open

- Inspect the Profile and Appearance footers in Unity at 16:9 and 20:9. Check status-text truncation with RETRY present and A/B/C contrast.
- Results: cancellation is detected from the existing `"MATCH CANCELLED"` footer prefix, the same string-based pattern as practice detection. A typed flag would be sturdier but changes the `ShowResults` contract. Dev sessions without a match ID still show the "waiting" note.
- Contracts/Mastery: if a re-fetch fails after the goals page has been drawn (for example after switching a contract), its retry row can be hidden in the same way. This was not changed because the feature is disabled in this build (`ContractsMasteryEnabled` false).
- Party and receipt behavior under real network loss needs runtime/network acceptance.
- Localization is not configured (A7 prerequisite). The new strings are literal UI copy.
