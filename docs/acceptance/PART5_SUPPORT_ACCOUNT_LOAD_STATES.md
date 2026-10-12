# Support account and wallet load states

## Rules applied

- Show a truthful unavailable state and a retry action when profile or wallet data cannot be loaded.
- Do not imply that an unknown account is a guest, linked, recoverable, or linkable.
- Keep match reports, compliance options, and web account deletion available while account state is unavailable.
- Keep support typography on the established 12/17/22 dp roles and semantic A/B/C colors.
- Preserve the existing account, wallet, reporting, and deletion backend contracts.

## Implementation

Support now distinguishes account loading from account-service failure, presents an in-page retry, and hides account actions until profile state is known. A wallet failure is labeled as unavailable in both the credits chip and account status row, with the same retry path. Retry preserves the report inputs and re-fetches profile and wallet data. The page keeps reporting and web-deletion options available during profile failure.

## Verified this pass

- Unity runtime C# compilation: `dotnet build Hotdrop.Runtime.csproj --no-restore` succeeded with 0 errors and 41 existing warnings.
- No tests or device checks were run.

## Unmet

- Inspect the support screen, retry transition, report roster, deletion gate, and A/B/C contrast in Unity at 16:9 and 20:9.
- Unity Localization is configured and authored Support/account-recovery copy uses the `support.*` table lookups, as recorded in `A7_LOCALIZATION_SUPPORT.md`. The Unity Editor seed command has not populated all current entries in the checked-in English table; translated locales, Urdu/Devanagari fonts, and device acceptance remain open.
- Offline and server-failure behavior still needs runtime/network acceptance.
