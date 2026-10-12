# Reconnect and bot-substitution configuration

## Rules applied

- Keep disconnect recovery and bot substitution aligned with the server's existing reserved-match contract.
- Preserve the same reserved avatar, five-second bot takeover default, final AFK removal, and fresh 90-second join credentials.
- Keep each join credential's expiry distinct from the reconnect window; do not introduce a new fixed reconnect duration.

## Scope and implementation

The existing five-second disconnect-to-bot grace now comes from `ProgressionConfig.disconnectBotSubstitutionSeconds`, the same config asset that owns AFK warning/removal windows. The authoritative match loads and validates the asset before play; missing or malformed values fail closed instead of reverting to server literals. The checked-in value and generated default remain five seconds, so valid-config runtime behavior is unchanged. Reconnect eligibility and join-ticket lifetime are unchanged.

## Verified this pass

- Existing reconnect authority coverage was read; tests were not run.
- The config asset, config generator, and authoritative disconnect callback use the configured field.
- `dotnet build ProjectHotdrop/Hotdrop.Runtime.csproj --no-restore -v:q` passed with 0 errors and 41 existing warnings.
- Focused `git diff --check` passed for the changed reconnect/configuration files.

## Unmet

- Verify same-avatar restore, bot substitution, final AFK removal, and refreshed-ticket behavior in a dedicated-server/multi-client run.
- Verify background suspension and reconnect on target Android/iOS devices; no network or device validation was performed.
- No separate fixed rejoin-duration value is defined by the current A5 product contract.
