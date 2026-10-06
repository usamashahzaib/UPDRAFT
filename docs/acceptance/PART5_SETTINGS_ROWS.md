# Part 5 settings rows acceptance

## Acceptance criteria

- Toggle settings use a 56 px row, with the setting name and a single plain-language description line.
- The switch stays right-aligned and retains its existing 48 px hit area.
- Long descriptions remain within their text bounds and are shortened or ellipsized rather than drawn over the switch or adjacent content.
- Existing setting values, defaults, persistence, and callbacks do not change.

## Review still required

Check the settings screen at narrow and wide landscape sizes and verify the 48 px target in device dp. No device review is implied by this implementation.
