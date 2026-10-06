# Training instruction typography acceptance

## Rules applied

- Use the Bible type scale, a maximum of three sizes on one screen, and the 12 dp minimum.
- Preserve tutorial step content, progression, skip confirmation, delayed help, and match controls.
- Do not move persistent tutorial labels over the crosshair or touch zones.

## Scope and implementation

The training card uses 12/17/22 dp caption, body, and title roles. Long-form instructions use the 17 dp body role, step names use 22 dp, and progress/help/skip labels use 12 dp. The title box height now fits the larger title, instruction height remains content-driven, and the negative line-spacing override was removed. Tutorial content and control behavior are unchanged.

## Verified this pass

- Existing offline tutorial progression, single-card, capture, and physical-size coverage was read; tests were not run.
- `dotnet build ProjectHotdrop/Hotdrop.Runtime.csproj --no-restore -v:minimal` succeeds with 0 errors and 41 existing warnings.
- `git diff --check` passes for the files changed in this pass.

## Unmet

- Recheck instruction-card occlusion, 16:9/20:9 safe areas, and 12 dp physical text size in Unity; no game capture or device run occurred.
- Localization remains an A7 prerequisite because Unity Localization is not configured; tutorial copy is still inline.
