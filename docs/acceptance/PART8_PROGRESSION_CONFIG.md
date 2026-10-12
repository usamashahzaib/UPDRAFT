# Server progression configuration acceptance

## Goal

Fail fast when progression configuration could make authoritative XP, level, rating, credit, or cosmetic rewards invalid.

## Rules applied

- XP-per-level and maximum level must be positive so level calculation is defined.
- XP and credit grants and the Elo K factor cannot be negative.
- The eligible-match unlock threshold must be at least one.
- Match-count unlock and first-match badge IDs must be non-empty, so config cannot silently produce an empty inventory reward or disable the amendment-required badge.
- Level unlocks must name non-empty cosmetics at reachable levels.
- Starter cosmetic entries cannot be empty.
- The maximum per-match XP grant and the maximum-level XP threshold must fit
  `ProfileXp`'s persisted 32-bit integer range; XP level calculation uses a
  wider intermediate, and accumulated XP saturates at that storage limit rather
  than wrapping negative.
- The maximum possible per-match credit summary, including every configured
  level-up reward, must fit its persisted 32-bit integer range.
- The lifetime wallet balance is aggregated as a 64-bit value and the client
  wallet DTO preserves that range. This avoids overflow as ledger entries
  accumulate over repeated matches; it does not introduce a wallet cap.
- Existing configured values and player-facing progression behavior are unchanged.

## Verification

- The backend binds and validates `ProgressionOptions` before registering the result processor or serving requests.
- `MatchCountUnlock` and `FirstMatchBadge` now fail startup validation when blank.
- XP reward sums, maximum-level thresholds, and the maximum possible credit
  summary now fail startup validation when they exceed persisted integer bounds.
- Level calculation uses a 64-bit intermediate. Accumulated XP saturates at
  `int.MaxValue` to avoid wrapping the stored total negative.
- Backend build passed with 0 errors; the build reported the existing NU1900 vulnerability-feed lookup warning (`api.nuget.org` unavailable) and nullable warning in `SupportEndpoints.cs`.
- The 11 October overflow-guard follow-up compiled with `dotnet build Backend/HotdropBackend/HotdropBackend.csproj --no-restore`: 0 errors and one NU1900 vulnerability-feed warning. No tests were run.
- The 11 October wallet follow-up changes the endpoint's ledger sum and client
  DTO to 64-bit credits. Backend build passed with 0 errors and one NU1900
  vulnerability-feed warning. Runtime sidecar build passed with 0 errors and
  41 existing Unity/runtime warnings. No tests or database integration checks
  were run.
- Runtime tests and database integration checks were not run.

## Unmet

- Automated invalid-configuration regression coverage was not run in this pass.
- Production deployment values and reward policy remain subject to release configuration review.
