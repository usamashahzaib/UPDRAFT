# Physical UI scale acceptance

## Rules applied

- Convert design dp and typography dp to canvas units using the active canvas pixel scale and reported display density.
- Keep interactive controls at least 48 dp and visible text at least 12 dp on the live canvas; enforce again after device-size/density changes and generated UI layout.
- Calculate each text/control floor against its local transform scale so scaled containers cannot shrink visible targets below the physical-size requirement.
- Preserve the current reference-frame layout and allow existing scroll views to handle vertical overflow.

## Acceptance evidence required

- Unity runtime C# build passed with 0 errors and 41 existing warnings after the transform-aware floor change.
- Source inspection confirms the floor recalculates when simulated/current screen dimensions or density change and uses each element's rendered scale.
- Run the existing A06 `UiPhysicalSizeTests` for menus and tutorial/live HUD/results, then inspect 16:9 and 20:9 layout in the Unity Editor and on a physical phone.

## Unmet

- Automated physical-size tests and Unity/device visual review were not run in this pass; the prior A06 failures therefore remain unverified after this change.
- Expanding controls and text may require screen-specific responsive reflow wherever fixed reference geometry cannot contain the larger physical sizes.
