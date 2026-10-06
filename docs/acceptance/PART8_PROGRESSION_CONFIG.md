# Server progression configuration acceptance

## Goal

Fail fast when progression configuration could make authoritative XP, level, rating, credit, or cosmetic rewards invalid.

## Rules applied

- XP-per-level and maximum level must be positive so level calculation is defined.
- XP and credit grants and the Elo K factor cannot be negative.
- The eligible-match unlock threshold must be at least one.
- Level unlocks must name non-empty cosmetics at reachable levels.
- Starter cosmetic entries cannot be empty.
- Existing configured values and player-facing progression behavior are unchanged.

## Verification

- The backend binds and validates `ProgressionOptions` before registering the result processor or serving requests.
- Backend build passed with 0 errors; the build reported the existing NU1900 vulnerability-feed lookup warning (`api.nuget.org` unavailable) and nullable warning in `SupportEndpoints.cs`.
- Runtime tests and database integration checks were not run.

## Unmet

- No automated invalid-configuration regression coverage was added or run in this pass.
- Production deployment values and reward policy remain subject to release configuration review.
