# Part 5 product-screen load-failure, stale, and terminal-result states

Spec inputs: `docs/design/00_PILLARS.md` (honest network state, server-authoritative
outcomes), `docs/design/01_LIFECYCLE.md` (Offline/server down: no offline match facts
presented as server-verified rewards; never show a false state), `02_ART_TOKENS.json`
(48 dp touch target, existing type roles). Support is recorded separately in
`PART5_SUPPORT_ACCOUNT_LOAD_STATES.md`.

## Audit findings fixed

| Screen | Gap found | Change |
|---|---|---|
| Backend response handling | A 2xx response with an empty or unreadable body was reported as successful even when the caller needed an authoritative DTO. Transport exceptions could also escape `async void` screen flows. | The client now marks successful responses without a readable DTO as a server error, reports Unity data-processing failures as errors, and converts transport exceptions into a failed result. Product screens can therefore render their existing unavailable/error copy instead of announcing success or leaving an operation unresolved. |
| Google Play linking / recovery | The platform provider's null result meant user cancellation, but provider exceptions were also converted to null and shown as cancellation. | The provider result now distinguishes Ready, Cancelled, Unavailable, and Failed. Link/recovery pages retain a specific cancellation message and show a retryable setup/sign-in error when the provider fails. |
| Home identity strip | After app restart, the saved refresh token existed while the in-memory access token did not, so Home showed "GUEST" and skipped the profile lookup. A failed lookup could also leave that false guest label visible. | Home checks both active and saved sessions, asks the backend to refresh/load the profile, and shows the localized "ACCOUNT SYNCING" state until the result arrives. A failed or empty profile response now shows "ACCOUNT UNAVAILABLE" with no account level or seals. "GUEST" is reserved for a device with no saved session. |
| Profile | On a profile, wallet, or match-record failure, the shared `Retry()` card was added to menu rows that the full-panel profile visual covers. The player saw "ACCOUNT SYNCING / Account data is loading." with no reachable retry. | The profile footer shows a 52 px RETRY action (with icon) next to EDIT APPEARANCE only after a failed load. Unloaded values read "unavailable" (alias, account line, progress, loadout slots, record) instead of "loading/syncing". The status text width shrinks to make room. The brand-new-account empty state (no stats record) is unchanged. |
| Appearance | "INVENTORY UNAVAILABLE … Retry to confirm ownership." had the same hidden retry. | The footer shows a 52 px RETRY action only while the inventory is unavailable, and it re-runs the existing inventory load. |
| Results (online) | Cancelled matches and unconfirmed receipts could appear server-verified; results inferred practice/cancelled state from footer text. Development sessions without a match ID stayed on a pending receipt note forever. Receipt, note, XP meter, and skip lookup paths also missed the nested progression card. An ineligible but recorded receipt used the earned-progression heading. | Results now receive an explicit trust state. Eligible recorded receipts, recorded ineligible receipts, pending, unavailable, cancelled, practice, and unverified development outcomes have distinct banners and progression notes. Footer copy is informational only. Receipt/reward UI resolves the actual `Viewport/Page/Progression` hierarchy. Cancelled online sessions report telemetry result `cancelled` instead of being counted as a loss. |
| Preparation selection rebuild | `Destroy` is deferred during play mode, so the outgoing `PreparationVisual` could still be returned by name-based lookups after a weapon-selection rebuild. A countdown update in that frame could target the outgoing surface. | The outgoing root is renamed before destruction, ensuring countdown updates resolve the newly built Preparation surface. |
| Party Room | The 3 s watcher ignored refresh errors other than not-found, so a stale room looked current. | On the first failure, the status reads "Party status could not refresh; showing the last known members and readiness. Retrying automatically." It returns to "Party status is current." on the next successful refresh. Leader-queue follow and not-found handling are unchanged. |
| Contracts / Mastery | When verified progression is enabled but its initial server load fails, `Retry()` placed its action in shared menu rows covered by the full-screen goals page. | Both pages now show an explicit unavailable state with a sticky 52 px RETRY footer action and a Back action. The page does not render placeholder progress or claim the record was verified. |

## Rules applied

- Use separate states for loading, empty, unavailable, and stale. Never leave a failed request labeled as loading.
- Do not add reward, account, or gameplay rules. Results text only restates existing server receipt fields (`recorded`, `eligible`), the existing cancellation state, or the absence of a development-session receipt ID.
- Preserve cancellation in client outcome telemetry so a server-cancelled match is not aggregated as a player loss.
- A recorded but ineligible receipt is shown as server-recorded with no rewards awarded; it does not use the earned-progression state.
- Keep the retry hit area at least 48 dp. Use only the existing caption, UI, and title tokens and semantic A/B/C colors. Reuse existing icon glyphs (`refresh`), because the icon font is a fixed subset.
- Keep BACK / RETURN TO HOME and EDIT APPEARANCE available in every state. The goals error page retains Back while prioritizing Retry over cross-navigation.
- Balance and config values are untouched.

## Verified this pass

- `dotnet build Hotdrop.Runtime.csproj -v:q -m:1`: 0 errors, 41 existing warnings after hardening the client transport response and exception paths and adding saved-session-aware Home identity. `git diff --check` passed for the code and acceptance changes.
- The same runtime build includes the Google Play provider outcome change: it compiled with 0 errors and 41 existing warnings. No native SDK cancellation/failure simulation was run.
- `dotnet build Hotdrop.Network.csproj -v:q -m:1`: 0 errors, 121 existing Unity/Fusion warnings with the shared backend client and identity changes included.
- `dotnet build ProjectHotdrop/Hotdrop.Runtime.csproj --no-restore`: 0 errors, 41 existing warnings after repairing a malformed duplicate `Progression` method and adding the Contracts/Mastery unavailable state. The prior run could not start because Unity's generated assets file was absent; restore with `--ignore-failed-sources` regenerated it.
- The ineligible receipt path now uses a distinct recorded-ineligible trust state, so a durable receipt with `eligible == false` cannot receive the earned-progression heading.
- `Hotdrop.Network` restore and build: `dotnet restore ProjectHotdrop/Hotdrop.Network.csproj --ignore-failed-sources --disable-parallel -m:1` succeeded, followed by `dotnet build ProjectHotdrop/Hotdrop.Network.csproj --no-restore -m:1`; 0 errors, 121 warnings from Unity/Fusion/runtime sources. The generated project compiled its current source list directly, with no temporary project copy.
- `MatchHUD.SetResultsReceipt` updated to make `trustState` optional (`ResultsTrustState?`), avoiding accidental overwrites of `UnverifiedDevelopment` / `Cancelled` states when updating receipt notes.
- Re-verified the Results call graph end to end (`NetClientPresenter` → `MatchHUD.ShowResults` → `ResultsView.Show`): no cancellation or practice state is inferred from footer copy, each outcome maps to a distinct `ResultsTrustState`, and the sticky Play Again action is built for every state, including cancelled and development sessions. `dotnet build ProjectHotdrop/Hotdrop.Runtime.csproj` and `ProjectHotdrop/Hotdrop.Network.csproj` both reported 0 errors (41 existing runtime warnings and 121 existing Fusion/Unity warnings respectively).
- `NetClientPresenter.MatchId` now falls back to `session.MatchId` when no override is set; when neither source supplies an id (a development session), Results reports the explicit `UnverifiedDevelopment` state instead of an indefinite pending note.
- No EditMode/PlayMode tests, Unity Editor captures, live receipt/network checks, or device checks were run.
- The transport response change was source-reviewed and runtime-compiled; no simulated empty/malformed-response scenario or live service outage was run.
- Home's identity refresh now treats a saved refresh token as an account session and replaces the syncing label with the localized unavailable state when the profile response fails or has no alias. This closes a mismatch found during current-source review.
- After this Home identity fix, `dotnet build ProjectHotdrop/Hotdrop.Editor.csproj --no-restore -m:1 -v:q` succeeded with 0 errors and 41 existing warnings. No tests or device checks were run.
- Contracts and Mastery load failures now render the retry inside the page's sticky footer rather than behind the full-screen visual. Runtime compilation succeeded after this addition.
- Preparation selection now renames its outgoing deferred-destroy root before rebuilding, so the same-frame countdown update resolves the active view. The runtime and PlayMode test assemblies compiled with 0 errors and 41 existing runtime warnings; tests were not run.
- Online cancellation now emits `online_match_completed` with payload `result=cancelled`, rather than misclassifying the cancelled session as a loss. `Hotdrop.Network` restored and built successfully with 0 errors and 121 Unity/Fusion warnings. Automated tests and analytics dashboard/live ingestion checks were not run.

## Unmet / open

- Inspect the Profile and Appearance footers in Unity at 16:9 and 20:9. Check status-text truncation with RETRY present and A/B/C contrast.
- Results trust-state transitions are source-reviewed and runtime-compiled; pending-to-receipt behavior still needs live backend/network acceptance.
- Cancellation telemetry's new `result=cancelled` category is source-reviewed; analytics dashboard consumers and live event ingestion were not exercised.
- Contracts/Mastery: the initial-load failure path now has an in-page retry. Feature-on retry and recovery visuals still need Unity/network acceptance because verified progression is disabled in this build (`ContractsMasteryEnabled` false).
- Party and receipt behavior under real network loss needs runtime/network acceptance.
- Superseded by the A7 localization setup: the current failure-state copy uses
  the configured localization lookups and English fallbacks across Product,
  Party, Profile, Appearance, Results, and Contracts/Mastery screens. See
  `A7_LOCALIZATION_PRODUCT_SCREENS.md`, `A7_LOCALIZATION_PARTY_ROOM.md`,
  `A7_LOCALIZATION_RESULTS.md`, `PART5_PROFILE_HIERARCHY.md`,
  `PART5_APPEARANCE_ACCOUNT_LOAD_STATE.md`, and
  `A7_LOCALIZATION_CONTRACTS_MASTERY.md`. The English table is statically
  audited; Unity Editor import, locale switching, translated wrapping, and
  device review remain open.
