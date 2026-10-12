# Level 5 guest-account link reminder

## Rules applied

- At level 5, remind guest players to link their account so progress can be recovered after reinstall or device change.
- Keep the prompt non-blocking and route it through the existing account screen and server-backed Google Play linking flow.
- Do not claim a first-purchase trigger while purchasing is disabled.
- If the profile lookup fails, do not infer guest eligibility; tell the player the check failed and retry on the next More open.

## Scope and implementation

When a level-5-or-higher guest opens More, the client checks the authoritative profile and shows one `LINK ACCOUNT` action plus the existing account-recovery explanation. It is shown once per installation and only when the platform linking action is available. The Account page still owns the actual link operation and backend state. No authentication, account data, purchase, or reward behavior changed.

If the profile cannot be loaded, More now reports that the account level could not be checked. Reopening More naturally retries the check; no link reminder is shown from unknown profile state.

## Verified this pass

- Existing first-run/Home/settings capture and Account guest/linked-state test source was read; tests were not run.
- The Unity runtime C# build succeeds with 0 errors and 41 existing warnings. Focused `git diff --check` passes.

## Unmet

- Verify the level-5 prompt on an Android build with Google Play linking configured; no device or network verification was performed.
- The first-purchase reminder remains unavailable while purchases are disabled.
- Home and account UI localization has since been migrated under A7; language,
  locale-switch, and device acceptance remain open in
  `docs/acceptance/A7_LOCALIZATION_HOME.md` and
  `docs/acceptance/A7_LOCALIZATION_PRODUCT_SCREENS.md`.
