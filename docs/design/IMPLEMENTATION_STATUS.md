# Design Bible implementation tracker

- 12 October Gate C bandwidth evidence helper: added a Linux standard-library
  sampler for the existing two-minute, six-client pad-heavy acceptance cell.
  It writes interval CSV and summary averages from the selected interface's TX
  counter; per-client egress ÷ 6 is reported as an upper bound, and the highest
  five-second interval average is not called an instantaneous peak or a PASS.
  **Spec rule applied:** preserve the Gate C ≤100 KB/s/client target and the
  acceptance plan's honest aggregate-to-per-client estimate. Python AST syntax
  parsing, CLI help, and documentation whitespace checks passed; no game build or
  tests were run. **Unmet:** no Linux host/device run, pad-drill log, per-peer
  attribution, or budget decision has been captured. See
  `docs/acceptance/GATE_C_ENVIRONMENT_PLAN.md` and
  `docs/acceptance/GATE_G_READINESS_INPUTS.md`.

- 12 October localized Graphics summary casing: the active preset note now
  uses the selected locale's uppercase rules after resolving its localized FPS
  and render-scale template. English appearance and graphics settings remain
  unchanged. Runtime build passed with 0 errors and 44 existing warnings;
  Editor build passed with 0 warnings/errors. No tests were run. **Unmet:**
  Unity locale switching and settings-screen visual/device review remain open.
  See `docs/acceptance/A7_LOCALIZATION_SETTINGS.md`.

- 12 October Results locale refresh: the live Results reflow watcher now
  subscribes to Unity Localization's selected-locale event. Switching locale
  while the page is open rebuilds its authored strings from the retained result
  snapshot and restores the scroll position; personal-best recording and match
  state are not repeated. **Spec rule applied:** migrated Results strings are
  refreshed for the active locale through the configured localization system.
  No tests were run. **Unmet:** Unity runtime locale-switch/render review,
  translated wrapping, RTL shaping, and non-English font coverage remain open.
  See `docs/acceptance/A7_LOCALIZATION_RESULTS.md`.

- 12 October Results header fit: the outcome eyebrow and combined mode/arena
  metadata now reserve the measured player-card column, alongside the title
  and score. Localized or larger-text copy ellipsizes within the remaining
  width at the existing 12 dp role. Runtime, Network, and Editor incremental
  builds passed with 0 errors and 0 warnings. No tests were run. **Unmet:**
  narrow/wide Unity captures, translated expansion, and device layout review
  remain open. See `docs/acceptance/PART5_RESULTS_TYPE_SCALE.md`.

- 12 October localized challenge-title casing: Offline Challenge detail,
  library inspection, progress tracker, result, and Home next-goal titles now
  use the selected locale's uppercase rules through `UiDraw.ToLocaleUpper`.
  English output and challenge behavior are unchanged. Runtime build passed
  with 0 errors and 44 existing warnings; Editor build passed with 0 warnings
  and errors. No tests were run. **Unmet:** Unity locale switching, translated
  shaping, and device layout acceptance remain open. See
  `docs/acceptance/A7_LOCALIZATION_OFFLINE_RUNTIME.md`.

- 12 October cosmetic-name localization: Appearance, Profile, Results rewards,
  and product-screen copy now resolve current cosmetic display names through
  the configured `UIStrings` table. Added three localized slot templates and
  nine current item-name keys, preserving English output and server-owned IDs.
  Unknown safe IDs retain the readable English fallback; unsafe IDs are not
  used as lookup keys. The next-wrap message uses the localized value-only name
  rather than stripping an English prefix, and uppercase cosmetic labels now
  use the selected locale's casing rules. No gameplay, entitlement, or catalog
  behavior changed.
  Runtime build passed with 0 errors and 44 existing warnings; Editor build
  passed with 0 warnings/errors. No tests were added or run. **Unmet:** Unity table import/re-save,
  non-English translation, locale switching, and rendered wrapping/device
  acceptance remain open. New cosmetic IDs need a localized entry. See
  `docs/acceptance/A7_LOCALIZATION_PRODUCT_SCREENS.md` and
  `docs/acceptance/A7_LOCALIZATION_TABLE_SEED.md`.

- 12 October Results privacy: MatchHUD's retained Results summary and table
  now rebuild on live Stream-Safe changes. Participant names, next-goal,
  personal-best, and receipt copy are re-presented under the current preference
  without re-recording match results or changing server data. Runtime build
  passed with 44 existing warnings; Network and Editor builds passed with 0
  warnings/errors. No tests were run. Unity capture review remains open. See
  `docs/acceptance/PART9_STREAM_SAFE_DISPLAY.md`.

- 12 October Preparation privacy: the live Preparation watcher now rebuilds
  when Stream-Safe Display changes, clearing cached teammate aliases or
  restoring their normal display names while preserving the selected weapon
  and countdown. No network identity or server data changes. Runtime, Network,
  and Editor builds pass with 0 errors and the Runtime dependency's 44 existing
  warnings; no tests were run. Unity toggle and capture review at 16:9/20:9
  remain open. See `docs/acceptance/PART9_STREAM_SAFE_DISPLAY.md`.

- 12 October Preparation reflow: the live weapon-selection screen now rebuilds
  on display-metric, root-width, or larger-text changes, preserving the chosen
  weapon and latest countdown without replaying phase-entry motion. This
  recalculates measured text and canvas-dependent placement while retaining the
  existing 12/17/22 dp typography and selection rules. Runtime, Network, and
  Editor builds passed with 0 errors; their Runtime dependency reported 44
  existing warnings. No tests were run. Unity rendering, card wrapping, and
  16:9/20:9 device acceptance remain open. See
  `docs/acceptance/PART5_PREPARATION_TYPE_SCALE.md`.

- 12 October: added the online Results scoreboard report shortcut. Rows mapped
  to a reportable human alias offer a localized 60 × 48 dp `REPORT` control;
  bot-only slots have none while a bot-substituted slot remains eligible only
  when it retains its original human alias. The local player has no control.
  Practice, UnverifiedDevelopment, and missing-match-ID routes remain hidden.
  Scoreboard columns below 560 dp now switch to a stacked, three-metric compact
  row so the name and report control retain space; wider columns keep the
  existing table.
  The action retains only the participant slot, then follows the existing Home
  callback into Support with that roster member selected. The selection survives
  account redraws and is cleared on screen exit, navigation, or recent-roster
  forget. The report still requires the existing violation class and explicit
  submit; strict “two taps to submit” remains unmet because defaulting a class
  would change product behavior. Runtime build passed with 44 existing warnings;
  Network and Editor builds passed with 0 warnings/errors using serial MSBuild.
  Static localization check found 1,458 unique aligned shared/English IDs. No
  tests were added or run. **Unmet:** Unity table import/re-save, rendered layout
  and touch-floor review on narrow devices, live report submission, and device
  acceptance remain open. See `docs/acceptance/PART6_REPORT_FROM_SCOREBOARD.md`,
  `docs/acceptance/PART5_RESULTS_TYPE_SCALE.md`, and
  `docs/acceptance/A7_LOCALIZATION_TABLE_SEED.md`.

- 12 October documentation reconciliation: the 10 October spawn-room clearance
  entry below still says deck and blocker placement formulas are separate.
  Current source and the 11 October acceptance update show that
  `TryGetSwitchyardDeckGeometry`, `TryGetSwitchyardRampGeometry`, and
  `TryGetSwitchyardSightlineBlockerGeometry` now supply generation and
  validation transforms/bounds from the same helpers. This supersedes only the
  stale drift note; shipped geometry and the remaining egress, collider, route,
  server, and device gaps are unchanged. The historical paragraphs in
  `PART8_MAP_CONFIG_VALIDATION.md` now label the drift finding as superseded
  while retaining those unresolved acceptance gaps. No code, tests, or builds
  were run.
  See `docs/acceptance/PART8_MAP_CONFIG_VALIDATION.md`.

- 12 October Gate G data mirror: `02_ART_TOKENS.json` now records the complete
  A/B/C `SwitchyardAtmosphere.Grade` values, including filters, skybox
  parameters, ambient sky/ground, bloom threshold, and shadow strength. Static
  JSON parsing and comparison matched all 20 grade fields per profile against
  the runtime constants. `PART0_SWITCHYARD_ATMOSPHERE_TIERS.md` records the
  applied mirror rule and the still-open captures, device performance, and Gate
  G scoring. No runtime behavior, tests, or builds changed.

- 12 October Relay audio parity: online clients now play the existing zone
  activation, contested-entry, and 30-point milestone cues from authoritative
  replicated match state, matching offline Relay. The presenter baselines zone,
  occupancy, and scores on local bind to avoid replaying old cues after
  reconnect; captions retain their existing localization, preference, and
  priority path. Scoring and server authority are unchanged. Network build
  passed with 0 warnings/errors. No tests were added or run. **Unmet:** regional
  spoken voice packs, live multi-client cue timing, Unity playback, and device
  acceptance remain open. See
  `docs/acceptance/PART6_CRITICAL_CUE_CAPTIONS.md` and
  `docs/design/05_AUDIO.md`.

- 12 October flight-audio teardown: disabling the online match presenter clears
  the local wind, cloth, and city-rush targets. `ClearFlightMix` no longer
  lazily constructs the persistent audio service when cleanup runs before any
  sound has initialized. Network build passed with 0 errors and 44 existing
  Runtime warnings. No tests were added or run. **Unmet:** Unity playback,
  scene-transition timing, and Android/iOS audio acceptance remain open. See
  `docs/acceptance/PART6_FLIGHT_CITY_RUSH.md` and
  `docs/design/05_AUDIO.md`.

- 12 October Results banner reflow: the outcome title/score line now reserves
  the right-side player card; the score moves below the title on narrow layouts
  and splits its FINAL SCORE caption when the available column is still tight.
  The banner height follows the selected arrangement while retaining the
  12/17/30 dp roles and result facts. Network build passed with 0 errors and 44
  existing Runtime warnings. No tests were added or run. **Unmet:** Unity
  rendering, locale expansion, and narrow/wide device acceptance remain open.
  See `docs/acceptance/PART5_RESULTS_TYPE_SCALE.md`.

- 12 October generated-project refresh: the locally generated and ignored
  `Hotdrop.Runtime.csproj` now includes `FlightSpeedLinesView.cs` and
  `RadarPingSprites.cs`; the previous source-omission limitation is superseded
  for this workspace. The normal Network build passed with 0 errors and 44
  existing Runtime warnings; the Editor build passed with 0 warnings and 0
  errors. **Unmet:** these generated project files are not source-controlled,
  and Unity import, Fusion weaving, rendered runtime, and device acceptance
  remain open. No tests were run. See
  `docs/acceptance/PART2_FLIGHT_SPEED_LINES.md`.

- 12 October radar ping cue parity: Go Here no longer falls back to the generic
  ping sound. A distinct generated rising chirp is appended to `GameCue` and
  explicitly mapped, preserving prior enum indices and existing text-caption,
  team, mute, and cooldown behavior. Network build passed with 0 errors and 44
  existing Runtime warnings. **Unmet:** listening, multi-client, and
  phone/headphone acceptance remain open. No tests were added or run. See
  `docs/acceptance/PART5_RADAR_TEAM_CUES.md`.

- 12 October: cached the localized Stream-Safe seat template and formatted
  labels per selected locale, and cached the preference after its first read.
  Match HUD name refreshes no longer call synchronous Localization lookups or
  read `PlayerPrefs` every frame; changing locale invalidates the label cache.
  Invalid translations that omit or escape the seat placeholder fall back to
  the English numbered template. The Support report roster now uses the shared
  null-name fallback and the participant's actual slot, matching the later
  report confirmation. Calls before Localization finishes initialization use
  uncached English labels so the HUD retries with the active locale next frame;
  failed localized lookups also stay uncached and retry at most once every five
  seconds per locale. `UiDraw.TryL` preserves the existing `L` fallback behavior
  while exposing lookup success to this cache.
  This preserves local-only redaction behavior. Runtime
  build passed with 44 warnings; Network and Editor builds passed with 0
  warnings and 0 errors. No tests were added or run. **Unmet:** Unity locale
  switching, translated formatting, capture review, and device readability
  remain open. See `docs/acceptance/PART9_STREAM_SAFE_DISPLAY.md` and
  `docs/acceptance/A7_LOCALIZATION_SUPPORT.md`.

- 12 October: routed Stream-Safe fallback labels through the existing
  localization table. Generic player and hidden-progress labels reuse their
  current keys; numbered seat labels use the new `streamSafe.playerSeatTemplate`
  entry. Redaction, account/network identity, and English output are unchanged.
  This applies `06_TECH.md`'s localization rule and Part 9's local-only,
  presentation-only behavior. Runtime build passed with 0 errors and 44 existing
  warnings; Network and Editor builds passed with 0 warnings and 0 errors. Static
  table counts are 1,456 unique aligned IDs. No tests were added or run.
  **Unmet:** Unity table import, live locale switching, translated formatting,
  stream-safe capture review, and device readability remain open. See
  `docs/acceptance/PART9_STREAM_SAFE_DISPLAY.md` and
  `docs/acceptance/A7_LOCALIZATION_TABLE_SEED.md`.

- 11 October: localized the procedural Switchyard landmark sign through the
  existing `UIStrings` table (`world.sign.switchyard`) with its current English
  fallback. Sign placement, geometry, and material styling are unchanged. This
  applies `06_TECH.md`'s localization rule and `07_WORLD_ART.md`'s
  presentation-only constraint. No tests were run. **Unmet:** Unity table
  import, translated fit, locale switching, and device readability remain
  unverified. See `docs/acceptance/PART2_SWITCHYARD_WORLD_TOKENS.md` and
  `docs/acceptance/A7_LOCALIZATION_TABLE_SEED.md`.

- 11 October: `SwitchyardSurfacing.Apply` now resolves each standalone material
  independently instead of returning when an unrelated surface is missing.
  Spawn-room floors with an available textured material still receive their
  metric UV mesh, avoiding stretched tiles in partial-material fallback. This
  preserves presentation-only geometry and the flat graybox fallback. No tests
  or builds were run. **Unmet:** partial-resource rendering in Unity and device
  checks remain unverified. See
  `docs/acceptance/PART8_SWITCHYARD_PROP_INSTANCING.md`.

- 11 October: moved the existing weapon-report mix distances, spatial range,
  mid-band low-pass cutoff, and nearby-caption split into `MatchRulesConfig`.
  Runtime sources and caption decisions use those config values; shared config
  validation enforces spatial minimum < full-mix < distant-report < spatial
  maximum for network and offline audio. `GameAudio` falls back to the existing
  type defaults if the asset is missing or its audio fields are invalid;
  `NetConfigs` continues to reject invalid match config. The defaults preserve
  current behavior. Editor build succeeded with 0 errors and 44 existing
  Runtime warnings; Network build succeeded with 0 errors and 0 warnings. No
  tests were added or run. **Unmet:** boundary listening,
  loudness/directional review, and device audio acceptance remain open. See
  `docs/acceptance/PART6_WEAPON_DISTANCE_MIX.md`.

- 11 October: added relative elevation arrows to radar pings using the same
  half-platform-height threshold and profile-aware symbols as teammate radar
  markers. Ping position, kind, team/expiry filtering, and enemy-reveal rules
  are unchanged. This applies `03_HUD_SPEC.md`'s map-elevation requirement and
  A/B/C token use. Editor build succeeded with 0 errors and 44 existing Runtime
  warnings. No tests were added or run. **Unmet:** Unity radar readability,
  grayscale/profile contrast, overlapping pips, online timing, and device
  behavior remain unverified. See
  `docs/acceptance/PART5_RADAR_TEAM_CUES.md`.

- 11 October: Claude independently reviewed the Switchyard ground and atlas
  cache mitigation, then added cache revalidation for destroyed atlas materials
  and a warning when the standalone Structure ground material is missing.
  The Editor build completed with 0 errors and 44 warnings, including the
  shared checkout. This applies `07_WORLD_ART.md`'s no-stretched-texture and
  complete-fallback requirements. **Unmet:** cache recovery, missing-material
  fallback, texture residency, rendering, and device behavior remain untested.
  See `docs/acceptance/PART8_SWITCHYARD_PROP_INSTANCING.md`.

- 11 October: centralized the low-health threshold and audio repeat cadence in
  `MatchRulesConfig`. Player sound and HUD labeling now share the configured
  threshold; the audio cadence has its own validated field and keeps the
  existing 1.2 s behavior. This applies Bible §2.5's configured low-health
  feedback rule. No tests were added or run. **Unmet:** runtime audio/HUD
  capture and representative-device verification remain open. See
  `docs/acceptance/PART2_DAMAGE_FEEDBACK.md`.

- 11 October: completed the previously missing Performance-tier low-health
  desaturation asset setup through Unity's `Hotdrop/Art/Configure Performance
  Damage Desaturation` command. The project-original shader material is
  registered on `HotdropForward.asset` through a fullscreen feature saved
  disabled; `SwitchyardAtmosphere` activates it only during the configured
  Performance low-health cue. Unity's Console showed 0 errors and 14 warnings
  after setup. This applies Bible §2.5 and `07_WORLD_ART.md`'s tiered-cost,
  presentation-only rules. **Unmet:** runtime cue activation, camera-stack/UI
  ordering, A/B/C captures, and device GPU cost remain unverified; no tests were
  run. See `docs/acceptance/PART2_DAMAGE_FEEDBACK.md`.

- 11 October: mitigated the worst Switchyard surface-atlas mip-bleed case by
  keeping the broad arena ground on the standalone repeating Structure
  material at its existing 6×5 scale; spawn-room floors and eligible roof
  surfaces remain atlas-backed. The atlas resolver now validates all four
  material references and their shared albedo/normal/mask textures, including
  the occlusion slot, and resets its cache on runtime startup and after the
  last arena releases it. Unity 6.3 script reload completed with 0 Console
  errors and 14 warnings. No tests were added or run. This applies Bible §8.1
  atlas use to eligible surfaces and `07_WORLD_ART.md`'s presentation-only and
  no-stretched-texture rules. **Unmet:** deep-mip isolation on the remaining
  atlased surfaces, Unity render comparison, runtime texture residency, player
  fallback behavior, and representative-device performance remain open. The
  standalone .NET Editor build could not reach compilation because MSBuild
  could not resolve installed workload SDK directories. See
  `docs/acceptance/PART8_SWITCHYARD_PROP_INSTANCING.md`.

- 11 October: the Unity prop-atlas bake now includes eight mapped material
  variants across five source prefabs, preserving the verified glass
  transparency mode. The crate remains excluded because its FBX has no
  diffuse/metallic links and its other source paths point outside the project;
  opacity-linked variants that currently import as opaque also remain on their
  original materials. This implements the atlas path for the eligible set but
  does not close §8.1: runtime resolution, rendered parity, mip behavior,
  instancing, memory savings, player shader retention, and device performance
  remain unverified. Source model/texture assets are still untracked in Git.
  No tests were run. See
  `docs/acceptance/PART8_SWITCHYARD_PROP_INSTANCING.md`.

- 11 October: fixed the unbounded lifetime of Switchyard surfacing cache objects.
  The runtime tracks each Switchyard before procedural mesh generation, then
  detaches its generated mesh and instancing-material references at teardown.
  Generated metric/atlas meshes plus both the surface and prop instancing
  material caches are destroyed after the final live root releases them;
  source materials and imported assets are preserved. Editor project build
  passed with 0 errors and 41 existing warnings. Switchyard mirror tests were
  inspected but not run, as requested. Unity teardown, re-entry, Profiler,
  player, and device checks remain to be confirmed. The presentation-only
  geometry/collider rules were preserved. See
  `docs/acceptance/PART8_SWITCHYARD_PROP_INSTANCING.md`.

- 11 October: replaced the invalid non-instanced MPB-marker approach for
  Switchyard props/surfaces with explicit `Graphics.RenderMeshInstanced`
  submissions for repeated opaque mesh/material groups. The configured map has
  12 platform decks with identical 10 m × 8 m footprints, deck tile value,
  atlas material, and cached mesh key, so they form a repeated group; singleton
  meshes remain on their existing renderer path. Eligible groups use instancing-enabled runtime
  materials and shared material-level profile tint with the existing A/B/C
  calculation; no unsupported MPB property is sent with instanced draws. Editor
  build succeeded with 0 errors and 41 warnings. No tests, Unity render, Frame
  Debugger, Profiler, player build, or device checks were run. Actual batching
  and performance remain unverified. See
  `docs/acceptance/PART8_SWITCHYARD_PROP_INSTANCING.md`.

- 11 October: fixed the partial-output failure in the Switchyard surface-atlas
  baker. It now encodes all three PNGs before replacing any atlas, stages them
  together with backups, and restores the previous set if commit or Unity
  import validation fails. Editor project build succeeded with 0 warnings and
  0 errors. No tests or Unity atlas action were run. Runtime/import behavior
  and frame-time/device gates remain unverified. See
  `docs/acceptance/PART8_SWITCHYARD_PROP_INSTANCING.md`.

- 11 October: implemented the §8.1 Shader Graph VFX material path for the
  existing soft-particle and tracer materials. Unity imported the adapted
  additive graph and particle-vertex-color subgraph; the Editor configuration
  action assigned the graph to both materials while preserving their textures.
  The Editor build succeeded with 0 errors and 41 existing warnings; Unity's
  visible Console showed 0 errors and 13 warnings after import. No tests were
  run. Scene rendering, shader-variant retention in a player build, Frame
  Debugger, Profiler, and representative-phone checks remain open. See
  `docs/acceptance/PART8_SHADERGRAPH_VFX.md`.

- 11 October: repaired the Switchyard surface-atlas source path after a static
  defect review. Rooftop-concrete cubes, including spawn-room floors, now retain
  the existing 6×5 repeat inside the structure atlas; incomplete atlas sets
  consistently fall back to non-atlas materials. The Editor baker now reads raw
  source images from disk, and the packed-mask atlas gutters were rebuilt from
  their registered CC0 source maps. Unity's atlas command completed, and a pixel
  comparison confirms all three atlas interiors and edge-clamped gutters match
  their source images. Atlas mode now avoids explicitly loading the four
  fallback surface materials; runtime texture residency and memory savings
  remain unmeasured. Unity reimported 39 prop textures; importer metadata
  confirms diffuse sRGB, normal/data linear roles, and Android ASTC 6×6 at 512.
  The material audit found 11 unique URP Lit materials: 10 opaque and one
  transparent, with no cutout. The imported materials bind diffuse/normal maps
  but not the restored metal/roughness/opacity maps, so the prop atlas remains
  open pending those associations. No tests, scene render, Frame Debugger,
  Profiler, player build, or device checks were run. Shader Graph VFX material
  integration is implemented; its scene-render and device gates remain open.
  Mip-bleed review, batching, and mobile performance remain open. See
  `docs/acceptance/PART8_SWITCHYARD_PROP_INSTANCING.md`.

- 11 October: prop atlas groundwork now imports source textures with the correct
  color-data roles: `_opacity_`, `_alpha_`, `_rough_`, and `_metal_` maps under
  `Assets/Art/Props` use linear sampling, normal maps remain normal data, and
  diffuse maps remain sRGB.
  A raw FBX inventory found roughness references for all six props and metallic
  references for five. Restored the remaining source-preserving Poly Haven 1K
  PBR maps, including exterior aircon variants and the security-light alpha map;
  all 38 unique 1K filenames referenced by bundled prop FBXs now resolve in the
  project. Unity created importer sidecars for all 39 texture files. A raw FBX
  inventory confirms 38 unique referenced filenames; the extra plastic-crate
  diffuse file is not referenced by an FBX. The interactive Editor audit
  recorded the actual material modes and assignments. A separate `dotnet`
  Editor build restored successfully but ended at output write with CS2012
  because the active Unity Editor held `Hotdrop.Editor.dll`; Unity itself loaded
  and ran the Editor menu without compile errors. No tests or device/render
  verification were run. At that point, the prop atlas remained open until
  restored source maps could be associated without guessing alpha behavior;
  the later transactional bake entry below records the eligible-set result and
  remaining exclusions. See
  `docs/acceptance/PART8_SWITCHYARD_PROP_INSTANCING.md`.

- 11 October: authored the Performance-tier low-health scene-desaturation path:
  a luminance blend shader, an inactive URP fullscreen feature configured by
  `WorldArtSetup`, and runtime activation only while its configured cue is
  nonzero. Balanced/Cinematic retain the atmosphere-volume path and the
  Performance UI vignette remains the edge cue. The renderer feature/material
  have not yet been generated in Unity; run `Hotdrop/Art/Configure Performance
  Damage Desaturation` or the complete world-art build. Runtime compilation
  passed with 0 errors and 41 existing warnings using a temporary target for two
  sources omitted by the generated project; Editor compilation passed with 0
  errors and 0 warnings using the already-built project references. The
  temporary target was removed. No tests, Unity import/render, camera-stack, or
  device checks were run. Shader import, feature registration/activation,
  visual result, and mobile GPU cost remain open. A Unity batch setup attempt
  did not reach the method because editor licensing initialization failed and
  `com.unity.editor.headless` was missing; no material or renderer subasset was
  created. A second non-batch Editor launch exited before the setup method; its
  log records refused Licensing IPC and Windows management-instrumentation
  assertions. `HotdropForward.asset` and the absent material remain unchanged.
  See
  `docs/acceptance/PART2_DAMAGE_FEEDBACK.md`.

- 11 October: the A/B/C world tint pass now maps the authored `Surface_Deck`
  material to the existing structure palette role and `Surface_Wall` to the
  facade role; rooftop cladding cloned from the wall atlas also resolves to the
  facade role instead of cover. The two authored surface names previously
  matched no tint branch and retained a single profile-independent color.
  Geometry, UVs, colliders, and gameplay are unchanged; the existing
  textured-surface blend and renderer property blocks remain. The latest
  Editor build graph passed with 0 errors and 121 existing warnings, using a
  temporary target to include two sources omitted by the generated project;
  the target was removed. No tests or renders were run. See
  `docs/acceptance/PART8_SWITCHYARD_PROP_INSTANCING.md`.

- 11 October: four Switchyard roof/surface PBR sets now share generated
  2304×2304 albedo, normal, and packed-mask atlas textures with 64 px extruded
  gutters. Four URP/Lit variants select aligned regions. Runtime boxes split
  faces at existing meter-repeat boundaries; the generated floor retains its
  6×5 UV repeat count. Collider meshes, transforms, layout, and profile roles
  are preserved. The Editor and Runtime build graph compiled with 0 errors and
  121 existing warnings, using a removed temporary target to include two source
  files omitted by the generated Runtime project. Static artifact inspection
  confirmed map/material alignment, unique Unity GUIDs, and exact source pixels
  inside every albedo/normal/mask tile; no tests, Unity import, scene render,
  Frame Debugger, Profiler, or device checks were run.
  Unity licensing failures prevented editor asset-import confirmation at that
  point. Later follow-ups below record the successful atlas import and prop
  atlas work. Shader Graph VFX, mip-bleed review, and mobile performance remain
  open. See `docs/acceptance/PART8_SWITCHYARD_PROP_INSTANCING.md`.

- 11 October: corrected the Gate F preview status to distinguish the existing
  localized concept-only shop from the still-unimplemented store-backed preview
  and purchase flow. No commerce behavior or claims changed. Verified against
  `ShopConceptView` and its acceptance record; no build or tests were needed.
  SKU mapping, purchase integration, legal/business approval, and device
  acceptance remain open. See `docs/acceptance/GATE_F_MONETIZATION_PLAN.md`.

- 11 October: party-room invite codes and member aliases now refresh when
  Stream-Safe Display changes while the room is open. The privacy listener is
  owned by the generated visual and is removed on disable/destruction. Copy
  behavior, party callbacks, and network identity are unchanged. Network
  compilation passed with 0 errors and 121 existing Unity/Fusion/runtime
  warnings. No tests, captures, or Unity/device checks were run; live-toggle
  acceptance remains open. See `docs/acceptance/PART9_STREAM_SAFE_DISPLAY.md`.

- 11 October: the pad-state radial fill no longer uses team identity colors on
  a button/control. It now uses the active profile's neutral primary-text role;
  the existing localized ALLY PAD / ENEMY PAD label continues to carry team
  identity. No control behavior changed. Source review is complete; Unity
  readability and device review remain open. No tests were run. See
  `docs/acceptance/PART2_PROFILE_PALETTE.md`.

- 11 October: spatial ARC, ZIP, and BRICK shots now omit the attack transient
  from 15 m through under 40 m, matching the filtered-body-only distance mix.
  Close spatial shots retain all layers, far shots retain their distant report,
  and local/non-weapon cues keep their existing transients. No gameplay values
  changed. Static source review and Network compilation passed; listening,
  device, and runtime acceptance remain open. No tests were run. See
  `docs/acceptance/PART6_WEAPON_DISTANCE_MIX.md`.

- 11 October: the earlier prop MeshRenderer instancing shortcut was corrected
  after auditing Unity's MPB rules and the installed URP/Lit declarations.
  Its undeclared, non-instanced marker has been removed. Repeated opaque prop
  and surface groups now use explicit `Graphics.RenderMeshInstanced` calls,
  with profile tint stored on the shared material; one-offs and transparent
  props keep their renderer path. Current Editor build passed with 0 errors
  and 41 warnings. Frame Debugger, Profiler, render, build-variant, and device
  verification remain open. Roof surface atlasing was implemented; at that
  review, prop texture atlasing remained open. The later transactional bake
  entry below records the eligible-set result. Shader Graph VFX material integration is implemented,
  with rendered visual and device verification still open. See
  `docs/acceptance/PART8_SWITCHYARD_PROP_INSTANCING.md` and
  `docs/acceptance/PART8_SHADERGRAPH_VFX.md`.
  No tests were run. See
  `docs/acceptance/PART8_SWITCHYARD_PROP_INSTANCING.md`.

- 11 October: `TeamProfileTint` now reapplies the selected A/B/C colors when a
  pad or avatar visual is enabled after being inactive during a profile change.
  This closes a presentation lifecycle gap without changing geometry, collision,
  or team rules. Runtime build passed with 0 errors and 41 existing warnings;
  Network build passed with 0 errors and 121 Unity/Fusion/runtime warnings.
  No tests, Unity import, rendered profile captures, multiplayer, or device
  checks were run. Gate G visual/device evidence remains open. See
  `docs/acceptance/PART0_NETWORK_PAD_PROFILE_TINT.md`.

- 11 October: networked pad discs and their ally-ring/enemy-cross markers now
  use the current A/B/C team palette and refresh live when the presentation
  profile changes. Existing shape cues, geometry, colliders, and team rules are
  unchanged. Network C# build passed with 0 warnings and 0 errors. No tests,
  Unity import, render captures, multiplayer session, or device checks were
  run. Gate G capture, scoring, and device checks remain open. See
  `docs/acceptance/PART0_NETWORK_PAD_PROFILE_TINT.md`.

- 11 October: the lifetime wallet ledger sum and client DTO now use 64-bit
  credits so repeated grants cannot overflow the wallet display range. No
  wallet cap or reward-policy change was introduced. Backend build passed with
  0 errors and one NU1900 vulnerability-feed warning; runtime sidecar build
  passed with 0 errors and 41 existing Unity/runtime warnings. No tests or
  database integration checks were run. See
  `docs/acceptance/PART8_PROGRESSION_CONFIG.md`.

- 11 October: the Operative Profile view now repaints from its cached snapshot
  when Stream-Safe Display changes, masking identity and account metrics
  immediately while the page stays open. Its listener is tied to the generated
  profile visual. Runtime sidecar build passed with 0 errors and 41 existing
  Unity/runtime warnings. No tests, captures, or Unity/device review were run.
  See `docs/acceptance/PART9_STREAM_SAFE_DISPLAY.md`.

- Combat source audit found overlapping online body/head hit volumes and a
  different offline head threshold. No geometry or damage change was made
  because the combat spec does not define a boundary/priority rule. Review the
  proposed geometry with combat balance owners and verify in Fusion/Unity; see
  `docs/acceptance/PART3_BALLISTICS.md`.

- 11 October: weapon report tone and noise now play as independent cached body
  and transient clips, using the existing envelope and gain. Close, mid, and
  far distance routing otherwise retains its prior layers. Runtime sidecar
  build passed with 0 errors and 41 existing warnings; Editor build passed
  with 0 errors and 0 warnings. No tests or listening/device checks were run.
  See `docs/acceptance/PART6_OFFLINE_WEAPON_AUDIO_LAYERS.md`.

- 11 October: corrected the Part 5 pad-readiness acceptance note to match the
  HUD: the teammate weapon label is omitted so pad and objective/respawn state
  fit in the compact card. This documentation-only correction does not change
  the implementation. See `docs/acceptance/PART5_HUD_TEAM_PAD_STATUS.md`.

- 11 October: fresh tutorial launches now show the localized “Do you want to
  fly?” question once in the startup splash, then preserve the accepted rig
  choice before starting training. The normal path uses the full 2.5-second
  launch beat; recovery states keep their existing explicit actions. Runtime
  build passed with 0 errors and 41 existing
  warnings; Network and Editor builds passed with 0 errors and 0 warnings.
  Static localization checks show 1,454 aligned rows. No tests, captures, or
  device checks were run. See
  `docs/acceptance/STEP_E.md` and
  `docs/acceptance/PART1_FIRST_FLIGHT_QUESTION.md`.

- 11 October: server pad damage now rejects expired/inactive pads as well as
  removed pads, preserving the acceptance rule that only active enemy pads
  can take damage. Damage values and friendly-fire behavior are unchanged.
  Network build passed with 0 errors and 0 warnings. No tests or live
  multi-client checks were run. See
  `docs/acceptance/PART8_DAMAGE_STATE_AUTHORITY.md`.

- 11 October: Switchyard roof validation now checks the 3D oriented ramp
  slabs pairwise so future map edits cannot reintroduce ramp/ramp intersections.
  Positions, ramp count, and dimensions are unchanged. Runtime build passed
  with 0 errors and 41 existing warnings; Network and Editor builds passed
  with 0 errors and 0 warnings. No tests or Unity geometry/playability checks
  were run. See
  `docs/acceptance/PART4_SWITCHYARD_ROOF_PLATES.md`.

- 11 October: the product-shell Account page now masks guest/linked state,
  account alias, recovery guidance and state-specific control captions under
  Stream-Safe Display. Live preference changes update those labels and the
  email-field masking; account actions and network identity are unchanged.
  Existing localized privacy/action strings are reused. Editor build passed
  with 0 errors and 0 warnings incrementally; no tests, captures or device
  checks were run. See `docs/acceptance/PART9_STREAM_SAFE_DISPLAY.md`.

- 11 October: Results now hides XP, credits, level, cosmetic unlocks, XP-meter
  animation, personal-best and next-goal callouts when Stream-Safe Display is
  active; the duplicate HUD receipt uses the localized `PROGRESS HIDDEN` label.
  Server reward records and result actions are unchanged. Runtime sidecar build
  passed with 0 errors and 41 existing warnings. No tests, captures, or
  Unity/device review were run. See
  `docs/acceptance/PART9_STREAM_SAFE_DISPLAY.md`.

- 11 October: the More-page level-5 guest account-link reminder is suppressed
  under Stream-Safe Display, including its status copy, and restored live when
  the setting turns off. A pending profile lookup drops its result if stream
  safe is enabled before it completes. The one-time reminder preference and
  account behavior are unchanged. Runtime sidecar build passed with 0 errors
  and 41 existing warnings; no tests, captures, or Unity/device review were
  run. See `docs/acceptance/PART9_STREAM_SAFE_DISPLAY.md`.

- 11 October: progression configuration now rejects XP reward sums and
  maximum-level thresholds that cannot fit the persisted `ProfileXp` integer,
  plus maximum credit summaries that exceed their integer range. Level
  calculation uses a 64-bit intermediate and accumulated XP saturates at the
  storage limit. Backend build passed with 0 errors and the existing NU1900
  vulnerability-feed warning; no tests or database integration checks were
  run. See `docs/acceptance/PART8_PROGRESSION_CONFIG.md`.

- 11 October: aligned the Performance draw-call reporting target with current
  Bible §7.3 (120); the 250k visible-triangle target remains intentionally below
  the Bible's 1.5M ceiling. Config defaults, generated config, serialized asset,
  and probe fallback now agree. This is report-only; device measurements remain
  open. See `docs/acceptance/PART7_PERFORMANCE_BUDGET_PROBE.md`.

- 11 October: radar pings now use cached project-original monochrome arrow,
  diamond, roundel, and triangle silhouettes for Go Here, Enemy Here, Group Up,
  and Pad Ready. Existing team/expiry filters, colors, text/audio cues, and
  reveal rules are unchanged. See `docs/acceptance/PART5_RADAR_TEAM_CUES.md`;
  grayscale, A/B/C, actual HUD-size and device rendering review remain pending.

Status recorded 10 October 2026. This is a working checklist, not a completion
claim. The latest saved PlayMode XML is from 6 October: 26/34 passed, with eight
failures. The tracker records a 7 October working-tree run as 26/36 and the
acceptance notes record targeted A06 passes plus a 35/36 full run, but no XML
artifact for those 7 October results is present, so their totals and failure
lists cannot be reconciled against the saved report. Those runs predate the
latest Party/settings/Challenge/Preparation/Results density reflow. No tests or
new captures were run after those source changes. The saved report's touch
failures predate the exit-transition raycast fix. Two objective-marker captures
failed against the saved preparation layout; the live objective marker now
suppresses itself when measured exclusions leave no collision-free slot, and
the behind-camera capture now targets the live HUD. Device builds have not been
run for this pass; platform-specific Android/iOS haptics and physical-device
behavior remain unverified. Prior 9 October build evidence records Unity runtime
C# compilation with 0 errors and 41 existing warnings, PlayMode test assembly
compilation with 0 errors and 0 warnings, Network project compilation with 0
errors and 121 Unity/Fusion/runtime warnings after serial restore, and a backend
build with 0 errors and one NuGet vulnerability-feed warning. On 10 October, a
Unity CLI batch launch timed out during Licensing initialization before C#
compilation; its log also records denied BIOS-serial access. No tests or device
checks were run for the reward reveal or 10 October config-validation changes.

- 10 October Switchyard spawn-capsule clearance: fail-closed validation checks
  the configured capsule diameter against the generated 4 m opening and checks
  the capsule top from every `SpawnExit` pose against the generated header.
  Offline generation and network map loading both call the check before using
  or mutating geometry. Shipped dimensions provide 0.55 m vertical clearance
  (capsule top ground+2.35 m; opening top ground+2.90 m) and 3.30 m horizontal
  width beyond the 0.70 m capsule diameter. Applied rules: Relay spawn-at-exit
  lifecycle and shared player capsule / generated opening configuration.
  Editor and Network project builds each passed with 0 errors and 41 existing
  Runtime warnings; no tests were added or run. Unmet: Unity collider and
  movement behavior, barrier passability, regenerated scene, dedicated server,
  multiplayer, device acceptance, and the previously noted ramp/cover and
  NavMesh traversal checks remain open. See
  `docs/acceptance/PART8_MAP_CONFIG_VALIDATION.md`.

- 10 October Switchyard spawn-room clearance: offline generation and network
  map loading now reject overlap between the two spawn-room clearance volumes
  and generated decks, ramp slabs, sightline blockers, or objective breakers.
  The roof slab thickness is a shared MapConfig constant used by generation and
  validation. The current room envelope is X ±10.25 m, Z 30.75…39.25 m
  (mirrored), Y ground…ground+3.5 m. The nearest deck is 0.75 m away in Z and
  18.75 m away in X; map geometry is unchanged. The clearance validator checks
  finiteness and footprints needed for its overlap calculations; full roof
  count/height/mirror rules are checked by the roof validator before both
  production call sites. Ramp footprint and clearance use one AABB helper,
  including rejection of zero/non-finite ramp lengths. Applied rule: keep
  generated gameplay collision geometry out of the solid team spawn-room
  volume. Network spawn-bounds validation uses the same 0.5 m wall allowance as
  offline generation. Deck thickness/top offset and blocker/breaker dimensions
  use shared MapConfig constants; breaker generation and validation now share
  one transform/AABB helper, while deck and blocker placement formulas remain
  independent. Editor build passed with 0 errors and 41 existing
  Runtime warnings; serial Network build passed with 0 errors and 0 warnings.
  No tests were added or run. Unmet: no egress/apron volume or minimum distance
  is enforced; an object just beyond an exit can obstruct traversal without
  overlapping the solid-room bounds. Deck and blocker AABB placement/orientation
  formulas remain independent of generation and can drift. Breaker position,
  rotation and AABB are supplied to generation and both validators by the
  shared MapConfig helper. Unity prefab/collider
  behavior, NavMesh, barrier activation, traversal, server, multiplayer and
  device acceptance remain open. Conservative ramp AABBs can report false
  positives near corners. A follow-up oriented-box source audit found four
  ramp/breaker intersections in the shipped layout, no ramp/ramp contact, and
  no design definition for cover clearance or the 4 m `minimumMainRouteWidth`
  path. Unity collision/traversal review and those owner decisions remain open.
  See
  `docs/acceptance/PART8_MAP_CONFIG_VALIDATION.md`.

- 10 October shared Switchyard outer-breaker geometry: added
  `MapConfig.TryGetSwitchyardOuterLaneBreakerGeometry` as the shared source of
  breaker transforms and rotated world-axis bounds for offline generation,
  objective footprint checks, and spawn-room clearance checks. Shipped centers,
  rotations, dimensions and layout are unchanged; the four yaw rotations give
  the same extents as the prior axis-aligned calculation. Applied rule: keep
  generated geometry and fail-closed config validation on one formula.
  Editor build passed with 0 errors and 41 existing Runtime warnings; Network
  build passed with 0 errors and 0 warnings. No tests were added or run.
  Independent read-only source review against HEAD found no regression and
  confirmed the transform order and rotated bounds math; it did not run a
  build/test or inspect Unity colliders or generated hierarchy. The serialized
  `MapConfig.asset` was separately checked and matches the documented defaults.
  Unmet: deck/blocker formulas still have independent copies; breaker/ramp
  contacts and cover/path owner decisions remain open. Unity collider, scene,
  traversal, server and device behavior are unverified. See
  `docs/acceptance/PART8_MAP_CONFIG_VALIDATION.md`.

- 10 October Switchyard presentation and sightline audit (source-only status
  superseded by the Unity sample recorded below): source contains the
  three named collider-free landmarks, zone-specific A/B/C colors and one/two/
  three ring ticks. Landmark placement is concentrated west/north; no spec
  target defines quadrant coverage. West/East hues are weakly separated in
  profiles A/C and East is near-neutral in B; no numeric hue-spacing target is
  authored. A source-only §4.2 follow-up found a likely 60 m edge ray from
  (45, 1.44, -30) to (45, 1.44, 30) that clears the generated static gameplay
  colliders. No Unity raycast was run, so engine confirmation remains open.
  Applied rules: preserve the named landmarks, zone tokens, independent ring
  shape cues, and collider-free art.
  Unmet at that audit: Unity legibility/sign-glow/CVD review, whether landmark
  distribution and profile B East are intended, roof-trim shape identity (the
  11 October follow-up below adds those cues; Unity visibility review remains
  open), the §4.2 reachability/cover definition, and full scoped long-sightline
  coverage.
  No code/assets changed and no tests/builds were run for this read-only audit.
  See `PART4_SWITCHYARD_LANDMARKS.md`,
  `PART2_SWITCHYARD_WORLD_TOKENS.md`, `PART2_OBJECTIVE_ZONE_SHAPE_CUES.md`,
  and `PART8_MAP_CONFIG_VALIDATION.md`.

- 11 October Switchyard §4.2 engine sample: an Editor-only report command
  generated only gameplay colliders in a temporary additive scene and queried
  Unity `Physics.RaycastAll` for the documented east-edge endpoints
  `(45, 1.44, -30)` to `(45, 1.44, 30)`, retaining hits from that scene only.
  Unity reported a 60 m unblocked candidate with no generated Ground, Cover,
  or SpawnBarrier collider intersection. The audit also checked 81 generated
  colliders and passed a Cover-layer positive control at X=0, which hit the
  generated south sightline blocker, and a downward Ground-layer positive
  control near the east map edge. The audit skips presentation, materials,
  zones, lighting, and profile subscriptions; it restores the original active
  scene after closing its temporary scene, with no map geometry change. A
  separate clean-scratch command saved and then removed its unique temporary
  scene asset; the scratch scene remained clean and the original active scene
  was restored. The direct command was also run with Main active. Its report
  sampled Main after cleanup, but Main was already marked dirty before the
  run, so a dirty-flag delta could not be determined. No scene was saved by
  the audit.
  This confirms only the static collider result for this one sample. Player
  reachability, dynamic
  obstruction, intended location/height scope, and the meaning of “breakable”
  remain unresolved. No tests or device checks were run. See
  `PART8_MAP_CONFIG_VALIDATION.md`.

- 10 October Switchyard roof access audit: the Bible §4.1 pad-plus-glide rule
  is not source-verifiable because the shipped player movement has no glide
  state, lift, or drag. Hand arithmetic shows a 14 m/s horizontal, 12 m/s
  upward pad launch under 20 m/s² gravity can cross a y=3 raised roof at
  roughly 4.83 m ascending or 11.97 m descending from a ground pad at y≈0.05;
  selected approaches look plausible, but only four lanes across three of
  twelve raised roofs were sampled and no Unity route was played. The apex
  clears raised decks by 0.65 m. Spawn-roof scope is undefined; its y=3.5 roof
  leaves only about 0.15 m apex margin, and no ramp reaches it. Applied rule:
  preserve the six raised roofs per half and do not invent a glide model.
  Unmet: define glide, decide if spawn roofs are included, and verify pad/glide
  access, collision clearance, route choice and timing in Unity. No code,
  values, tests, or builds changed for this source/docs audit. See
  `docs/acceptance/PART4_SWITCHYARD_ROOF_PLATES.md`.

- Gate E support/deletion source audit: the dated 28 September acceptance list
  was stale for Home > More > Support / Report and the web deletion flow.
  Current client source wires recent-match reporting and two-step in-app
  deletion to authenticated backend endpoints; the public form has
  single-use email verification and deletion completion in source. The Gate E
  correction records source evidence without claiming rendered/live API,
  publisher SMTP/domain, delivery, or real-account/device verification. No
  code, build, or tests changed for this documentation correction.

- 10 October body-rig localization follow-up: the Home runner label and the
  two rig-choice titles now use localized Male/Female names instead of reading
  the English-only `BodyRigs.DisplayName` value in player-facing UI. Added two
  stable English-table rows (`-760000000000002044` and `-760000000000002045`)
  while leaving rig storage and display-name API behavior unchanged. Applied
  rules: Bible v2.1's Male/Female choice and `AGENTS.md`'s player-facing text
  localization requirement. Source/table static checks only; Unity import,
  runtime, translation, layout, and device review remain open. No tests/builds
  were run. See `docs/acceptance/A7_LOCALIZATION_BODY_RIGS.md`.

- 10 October unused body-label cleanup: removed the no-longer-used
  `home.rig.presentationOne/Two` keys from `SetupLocalizationPilot` and both
  UIStrings tables. A scoped reference audit found no runtime, scene, prefab,
  or test consumers; design mockup labels and the distinct live Body
  Presentation category remain unchanged. Current tables each contain 1,454
  unique matching IDs; the complete seed audit plus these two removals gives
  1,448 unique seed keys, with the same six unrelated legacy rows unassigned.
  Applied rule: keep shipped player copy aligned with the configured
  localization seed/tables. Editor build passed with 0 errors and 0 warnings
  printed by the incremental build; no tests were run. Unity import/re-save, locale, translated layout,
  RTL, and device checks remain open. See
  `docs/acceptance/A7_LOCALIZATION_BODY_RIGS.md` and
  `docs/acceptance/A7_LOCALIZATION_TABLE_SEED.md`.

- 10 October Gate E requirement-map reconciliation: corrected the dated table's
  stale claims that Profile/Appearance, Party, Support/Report, and deletion
  screens were absent. Current source shows these Home-routed screens and the
  public email-verification completion path. Applied rule: Gate E status is
  based on current implementation evidence and separates source presence from
  runtime/service acceptance. Unmet: live API, production email, account,
  Unity/device, failure-injection, authored-rig-art, and release verification
  still prevent acceptance. Documentation/source cross-check only; no tests or
  builds were run. See `docs/acceptance/STEP_E.md`.

- 10 October Switchyard ramp headroom validation: each generated walkable ramp
  is checked against other raised decks, requiring clearance at least equal to
  the configured player capsule height from `MovementConfig`. Both the offline
  generator and network config loader validate before use; the current layout
  remains unchanged and has a source-calculated minimum 1.92 m headroom for a
  1.8 m-tall, 0.35 m-radius capsule. Applied rules: Bible §§4.1/5 roof
  play-space and player capsule, using live config values. Editor build passed
  with 0 errors and 41 existing Runtime warnings; Network build passed with 0
  errors and 0 warnings. No tests were run. Unmet: Unity collider,
  nav-mesh, traversal, ramp-ramp/cover and Unity collider/NavMesh spawn-room
  behavior, route timing, server runtime, and device acceptance remain open. See
  `docs/acceptance/PART8_MAP_CONFIG_VALIDATION.md`.

- Gate G pad-mesh slice: offline and network pad rendering now shares a
  project-original 16-segment beveled landing mesh in place of the graybox
  cylinder. The existing pad dimensions and gameplay colliders are preserved;
  the asset register and `PART0_LAUNCH_PAD_MESH.md` record provenance and
  limits. Editor build passed with 0 errors and 41 existing warnings; Network
  build passed with 0 warnings and 0 errors. No tests were run. Unity visual
  review and device render-cost acceptance remain open.

- 10 October Part 5 owner decisions pending (none resolved, no source change):
  (1) Cockpit Mode scope, Bible §5.2 "all non-essential HUD" versus the roster/
  radar list in `03_HUD_SPEC.md` (compass follows radar), with no DECISIONS.md
  entry; (2) the 2 s ping cooldown versus Bible §6.5 "no spam lockout needed";
  (3) "Mute team pings" copy says sounds only, but muted pings also hide world
  markers while the radar still shows them; (4) whether the enemy-here ping
  shape falls under the outlined enemy-diamond rule (it is solid today).
  Evidence and current behavior: `docs/acceptance/PART5_RADAR_TEAM_CUES.md`.

- 10 October Part 5 ping-kind identification acceptance: visible world markers
  carry localized kind text; radar markers use kind-specific shapes, but hidden
  world captions leave the radar shape plus a generic ping sound. Compliance
  with `03_HUD_SPEC.md`'s shape-and-text-or-sound rule is unverified in that
  state, especially at HUD size and in grayscale. No code/build/test/runtime
  change was made for this audit. See
  `docs/acceptance/PART5_RADAR_TEAM_CUES.md`.

- 10 October radar own-marker facing: the own-player arrow on the radar now
  rotates with camera heading in north-up mode (rotation `-(yaw - relativeYaw)`,
  the same sign convention as teammate arrows) and stays upright in player-up
  mode. Scale, enemy visibility, pings, and Cockpit scope are unchanged. Applied
  rule: `03_HUD_SPEC.md` map facing. Editor project build succeeded with 0
  errors and 41 existing warnings; no tests were run. Unmet rules: Unity/visual
  orientation check, 16:9/20:9 and device acceptance. See
  `docs/acceptance/PART5_NORTH_UP_RADAR.md`.

- 10 October radar map-footprint alignment: the configured arena footprint now
  uses an unclamped world-to-radar transform, while individual edge markers
  keep the existing clamp. This prevents the footprint from drifting away from
  player-relative objective, teammate, and ping markers when the player moves
  more than 45.2 m from map center. Scale, range, geometry, and both yaw modes
  are unchanged. Applied rule: `03_HUD_SPEC.md` map footprint and live marker
  positions share gameplay coordinates. Editor build passed with 0 errors and
  41 existing Runtime warnings; scoped `git diff --check` passed with only
  LF-to-CRLF warnings. No tests were run. Unmet: Unity visual/runtime, online,
  and device checks. See `docs/acceptance/PART5_NORTH_UP_RADAR.md`.

- 10 October Part 8 spawn/blocker config checks: the Switchyard validator now
  requires the north and south spawn centers to stay on their named sides and
  keeps both generated sightline-blocker volumes inside bounds and between
  Center and the spawn-room walls. Generator dimensions now share constants
  with the validator; checked-in values and geometry are unchanged. Applied
  rules: the `SpawnExit` facing contract and the existing map-config comment
  describing blocker placement. Editor build passed with 0 errors and 41
  existing Runtime warnings; Network build passed with 0 errors and 0 warnings.
  No tests were run. Unity import, dedicated-server load, spawn-to-spawn
  line-of-sight, collider, and device acceptance remain open. See
  `docs/acceptance/PART8_MAP_CONFIG_VALIDATION.md`.

- 10 October Part 8 follow-up: Switchyard config validation now checks all nine
  north-to-south spawn-exit centre segments against both sightline-blocker
  footprints and rejects any segment that misses both, enforcing the recorded
  no-open-exit-line decision while allowing shifted spawn centres only when
  blockers still cover every exit pair. Spawn-room walls, headers, roof, stripe
  and sightline blockers now follow finite `groundLevelY`; Switchyard objective
  centres must use that same Y so rings, markers, triggers and zone data stay
  aligned. The shipped X = 0 spawn centres and ground Y = 0 geometry are
  unchanged. Editor build passed with 0 errors and 41 existing Runtime
  warnings; Network build passed with 0 errors and 0 warnings. No tests were
  added or run. Source validation covers exit centres only; arbitrary/airborne
  line-of-sight, Unity geometry/import, server load, nav bake, runtime and device
  acceptance remain open. See `docs/acceptance/PART8_MAP_CONFIG_VALIDATION.md`.

- 10 October Part 8 sightline coverage follow-up: validation now requires the
  full 4 m width of each generated spawn opening to fit inside both central
  sightline blockers' shared X span, with the existing geometry tolerance.
  The generator and validator share the opening-width, wall-height and header-
  height constants; validation also requires blockers to cover the complete
  clear doorway height. Shipped geometry is unchanged. With blocker Z
  placement also validated, this covers straight lines between points in
  opposing doorway apertures. Editor build passed with 0 errors and 41
  existing warnings; Network build passed with 0 warnings and 0 errors. No
  tests were run. Unity scene/collider review and sightlines outside the
  apertures, including airborne shots, remain open. See
  `docs/acceptance/PART8_MAP_CONFIG_VALIDATION.md`.

- 10 October Part 8 cover/ramp source audit: static oriented-box analysis finds
  four ramp/breaker intersections in the shipped layout (two north, two
  mirrored south), no ramp/ramp contacts, and no ramp/breaker pairs closer than
  approximately 1.5 m beyond those four contacts. The north West pair's
  horizontal AABB overlap is about 2.6 m³; a sampled 3D overlap estimate is
  about 0.03 m³, with a ramp lip no more than about 0.071 m above the walking
  face over roughly 0.17 m of run. These volume/depth figures are static
  estimates, not analytic collider or traversal results. Both cubes retain
  their primitive colliders. The Bible does not define ramp-to-cover clearance
  or which path `minimumMainRouteWidth` (4 m) measures; the inner-flank gap is
  2 m at the ramp ground ends. No code or geometry changed pending those owner
  decisions. Unity collider, controller, NavMesh and traversal acceptance
  remain open. No tests or builds were run for this documentation-only audit.
  See `docs/acceptance/PART8_MAP_CONFIG_VALIDATION.md`.

- 10 October HUD north-up minimap option: the Accessibility tab now includes a
  localized NORTH-UP MINIMAP preference. North-up mode holds the map footprint,
  objective, teammates, facing arrows, and team pings in world-north orientation;
  the default preserves the existing player-up view. The saved option is applied
  on live HUD updates and returns to player-up on Accessibility reset. Two
  localization rows were added with stable IDs. Applied rule: Bible 5.2's
  minimap rotation and north-up Settings option. Unmet rules: Unity import,
  visual/runtime orientation review, 16:9/20:9 and device acceptance remain
  open; no tests were run. See `docs/acceptance/PART5_NORTH_UP_RADAR.md`.

- 10 October HUD compass strip: the radar now has a localized moving N/E/S/W
  compass tape with a fixed forward tick below the map. It follows camera
  heading in both north-up and player-up map modes; existing HDG and zone
  readouts remain. Four stable localization rows were added. Applied rule:
  Bible §5.2's compass strip beside the minimap. Unmet rules: Unity import,
  visual orientation/contrast, responsive layout, live network, and device
  acceptance remain open; no tests were run. See
  `docs/acceptance/PART5_COMPASS_STRIP.md`.

- 10 October HUD high-contrast objective marker option: the Accessibility tab
  now includes a localized HIGH-CONTRAST OBJECTIVE MARKER preference, off by
  default. When on, an opaque profile-background disc sits behind the existing
  gold objective ring, inside its 34 px footprint. Marker geometry, placement
  and visibility rules are unchanged. The option is saved in PlayerPrefs,
  counted by the Accessibility customized check, and turned off by
  Accessibility reset. Two stable localization rows were added
  (`-760000000000002042`, `-760000000000002043`). Applied rule: the requested
  accessibility setting, using an opt-in treatment because the spec did not
  define visual treatment, plus the HUD
  spec's bright-sky readability check. The Editor project build succeeded
  with 0 errors and 41 existing Runtime warnings; no tests were run. Unmet
  rules: Unity import,
  A/B/C contrast, bright/dark sky and grayscale visual review, 16:9/20:9 and
  device acceptance remain open. See
  `docs/acceptance/PART5_HIGH_CONTRAST_OBJECTIVE_MARKER.md`.

- 10 October Graphics settings follow-up: the existing reset action now reads
  AUTO DETECT and still applies the existing telemetry memory-tier
  recommendation. The three visible preset names are LOW/MEDIUM/HIGH and map
  to the unchanged Performance/Balanced/Cinematic presets. Localized helper
  copy explains the recommendation basis and states that runtime quality does
  not auto-fallback. Four English localization entries were added; the shared
  and English tables each contain 1,436 unique, aligned
  rows. The Editor project build passed with 0 errors and 41 existing Runtime
  warnings. Applied rule: expose the existing recommendation action from the
  Graphics settings and localize player-facing copy. Unmet rules: hardware-tier
  accuracy, Unity table import, visual review, and device acceptance remain
  open; no tests were run. See
  `docs/acceptance/A7_LOCALIZATION_SETTINGS.md`.

- 10 October Settings Audio label follow-up: the existing Voice-category
  volume slider is labeled ANNOUNCER VOLUME, matching the category's routed
  countdown, zone, victory, and defeat cues. The mixer category and saved
  preference key are unchanged; its English table row now uses the
  `settings.copy.announcer.volume` key. Applied rule: expose the Announcer
  slider specified by the Settings screen brief. Unmet rules: no visual or
  device review was performed; no tests were run. See
  `docs/acceptance/A7_LOCALIZATION_SETTINGS.md`.

- 10 October Settings Audio placement follow-up: MUTE TEAM PINGS moved from
  Accessibility to Audio, and RESET AUDIO now clears its existing mute
  preference. RESET ACCESSIBILITY no longer changes that audio preference.
  Applied rule: place the ping mute under Audio as specified by the Settings
  screen brief; existing preference identity and ping behavior are preserved.
  Unmet rules: no visual or device review was performed; no tests were run.
  See `docs/acceptance/A7_LOCALIZATION_SETTINGS.md`.

- 10 October Settings reset/save follow-up: Controls, Audio, and Accessibility
  now expose RESET TO DEFAULT; Graphics AUTO DETECT reapplies its existing
  memory-tier default. A reset confirmation opens only when stored values
  differ from that tab's defaults. Settings mutations show a transient,
  localized SAVED chip, including control-layout completion and gyro
  calibration. Six new localized strings were added and serialized; both
  localization tables contain 1,442 unique, aligned rows. The Customize
  Controls editor now commits on SAVE, rolls back on CANCEL, shows a safe-area
  guide and collision warning, and offers a left-handed layout toggle. Four
  localized strings were added for those controls; both tables contain 1,446
  unique, aligned rows. The Editor project build passed after this change with
  0 errors and 41 existing Runtime warnings. Applied rule:
  provide conditional confirmation, per-tab reset, and saved feedback. Unmet
  rules: Account & Support has no preferences to reset. The Customize Controls
  editor now masks live gameplay with a neutral token-colored preview of the
  movement stick, seven controls, and passive HUD center; preview accents refresh
  from active UI tokens across profiles A/B/C. Unity visual review and runtime
  confirmation of touch/camera isolation remain open; the camera input sink
  explicitly ignores aim and clears focus while editing. Dynamic resolution
  policy, visual review, and tests remain open. See
  `docs/acceptance/A7_LOCALIZATION_SETTINGS.md` and
  `docs/acceptance/PART7_TOUCH_LAYOUT_EDITOR.md`.

- 10 October Touch layout camera guard: `PlayerController.UpdateCameraInput`
  now ignores aim input and releases focus framing while the control editor is
  open, protecting the camera sink from stale aim deltas regardless of Unity
  component update order. The Editor project build succeeded with 0 errors and
  41 existing Runtime warnings; scoped diff check passed. Applied rule: editor
  UI gestures must not rotate the gameplay camera. Unmet rules: runtime/device
  confirmation and tests remain open; tests were not run. See
  `docs/acceptance/PART7_TOUCH_LAYOUT_EDITOR.md`.

- 10 October Customize Controls neutral preview: the editor now uses an opaque
  A/B/C token-colored backdrop and draws its static movement stick, seven input
  controls, and passive center copy from the live layout rectangles and opacity
  values. The Editor project build passed with 0 errors and 41 existing Runtime
  warnings; scoped diff check passed. Applied rule: provide a separate,
  neutral gameplay-layout preview. Unmet rules: Unity import and visual review
  across 16:9/20:9, larger text, and device target sizes remain open; no tests or
  device checks were run. See
  `docs/acceptance/PART7_TOUCH_LAYOUT_EDITOR.md`.

- 10 October localization inventory reconciliation: current-count statements in
  the design tracker, technical note, and A7 acceptance records now reflect the
  serialized table state. Static parsing found 1,446 shared keys and English
  rows with unique IDs/keys, identical ID order, and exact values for the four
  touch-layout editor additions. Applied rule: keep localization acceptance
  records aligned to current checked-in assets. Unmet rules: Unity import,
  compiled-source reflection, live locale switching, and translated wrapping
  remain unverified; no tests were run. See
  `docs/acceptance/A7_LOCALIZATION_TABLE_SEED.md`.

- 10 October damage feedback (Bible §2.5 "Damage"): directional
  hit arcs, a neutral low-HP vignette and a two-per-second camera-shake cap,
  with values in `MatchRulesConfig`. Offline Relay, tutorial and challenges
  raise the arc through a new `PlayerController.OnDamagedFrom`; online has the
  vignette and shake cap, and now a hit arc through replicated
  `DamageCounter`/`DamageSourceDirection`/`DamageEventSequence` fields (needs a server rebuild; unexercised).
  `DamageEventSequence` is player-object-lifetime and avoids stale/missed hit arcs across respawn/rematch while `DamageCounter`
  remains per-life for the avatar mark. `Hotdrop.Runtime` built with 0 errors and 41 existing
  warnings and `Hotdrop.Tests` with 0 errors; `DamageFeedbackTests` were
  written but not run. Unmet: Fusion weaving, server rebuild, multi-client check, Unity import and visual review,
  A/B/C and 16:9/20:9, grayscale, device. Follow-up: the low-HP cue now fades
  after 1.2 s and retriggers on damage while below threshold; Balanced/Cinematic
  apply desaturation plus vignette through the existing URP atmosphere volume.
  Performance now has an authored on-demand fullscreen desaturation path, and
  Unity has registered its material and saved-disabled renderer feature. Runtime
  code activates it only during the configured cue. Unity setup logged 0 errors;
  runtime pass activation and rendered output remain unverified. Runtime
  source compiled with 0 errors and 41 existing warnings; Network source compiled
  in a transient project with local Runtime/Fusion/Photon editor references (0
  errors, 0 warnings). Normal generated-project discovery, Fusion weaving,
  server rebuild, multi-client checks, Unity import and visual review remain
  open. See `docs/acceptance/PART2_DAMAGE_FEEDBACK.md`.

Recent implementation slices:

- 11 October Bible §6.1 weapon-distance mix: spatial ARC/ZIP/BRICK reports now
  play full body/mechanism/tail below 15 m, filtered body from 15 m to under
  40 m, then a short cue-specific directional report from 40 m through the
  existing 50 m linear-rolloff limit. Local non-spatial cues, captions,
  weapon cadence, and damage are unchanged. Runtime C# sidecar compilation
  passed with 0 errors and 41 existing warnings. Unmet: runtime listening,
  distance transitions, occlusion, voice-cap, and phone/headphone acceptance
  remain open. No tests were added or run. See
  `docs/acceptance/PART6_WEAPON_DISTANCE_MIX.md`.

- 11 October Bible §6.4 critical-caption follow-up: the local own-death drone
  now publishes the localized `ELIMINATED` caption through the existing
  gameplay-caption preference and priority-80 tier. Its source seed and both
  English UIStrings tables use stable ID `-760000000000002046`. Death audio,
  death routing, and remote elimination behavior are unchanged. Unmet: Unity
  asset import, runtime caption-priority observation, translated-locale and
  device review remain open. No tests were added or run. See
  `docs/acceptance/PART6_OWN_DEATH_CUE.md` and
  `docs/acceptance/PART6_CRITICAL_CUE_CAPTIONS.md`.

- 10 October Bible §2.5 pilot rim light: both the skinned operator and
  primitive fallback now use a project-original shared URP material variant
  with a subtle view-angle rim; the offline/online torso team tint keeps its
  existing profile-property-block behavior. The shader is one forward pass,
  uses existing geometry and lights, and adds no post effect or outline draw.
  Prototype intensity/falloff are 0.20/3 because the Bible leaves numeric
  styling open. Unmet: Unity shader import/compile, A/B/C art review, team-cue
  legibility and device-tier performance acceptance remain open. No tests were
  added or run. See `docs/acceptance/PART0_PILOT_RIM_LIGHT.md`.

- 10 October Bible §2.5 pad-launch feedback: the local offline launch event and
  local online `Launched` transition now produce the specified three-frame FOV
  pulse and a brief chromatic edge through `SwitchyardAtmosphere`. Defaults are
  provisional presentation estimates in `MatchRulesConfig` (2° FOV, 0.08
  chromatic intensity, 0.15 s fade). Reduced Motion suppresses both; the
  chromatic post effect is disabled on Performance tier to preserve its
  no-post-processing policy. Haptics remain unchanged. Transient Runtime source
  compilation passed with 0 errors and 41 existing warnings; transient Network
  source compilation passed with 0 errors and 0 warnings using local references.
  Network startup validates the new ranges, and offline effect entry points
  guard non-finite/malformed values.
  The ordinary generated Editor build is still blocked by a stale project file
  omitting the earlier flight-lines view. Fusion weaving, Unity import, visual
  review and device/network runtime checks remain open. No tests were added or
  run. See `docs/acceptance/PART6_PAD_LAUNCH_FEEDBACK.md`.

- 10 October Bible §2.5 flight speed lines: a 12-streak static perimeter cue now follows the
  local airborne player's planar speed in offline Relay, tutorial, offline challenges, and online
  Relay. Opacity reuses the flight-audio 4–18 m/s speed envelope and the configured maximum; no
  speedometer UI, translation, or scaling was added. Applied rule: show speed lines at the periphery
  during flight while wind audio remains the speedometer. Runtime source compile passed (0 errors, 41
  existing warnings); transient Network source compile passed with local references (0 errors, 0
  warnings). Unmet: no distinct glide state exists; Unity import, visual/aspect-ratio review, normal
  generated-project build, Fusion weaving, and live Fusion/device acceptance remain open. No tests
  were added or run. See `docs/acceptance/PART2_FLIGHT_SPEED_LINES.md`.

- 10 October A7 Party Room caller localization: `PlayerProductScreens.ShowPartyEntry`
  now passes the default roster label through the existing
  `partyRoom.seat.you` lookup instead of supplying an untranslated literal.
  Applied rule: player-visible Party Room copy uses configured localization.
  Unmet rules: locale-switch rendering, Unity asset import, translated layout,
  and device review remain open; tests were not run. See
  `docs/acceptance/A7_LOCALIZATION_PARTY_ROOM.md`.

- 10 October A7 Party Room placeholder localization: the visible join-code
  format example now reads through `partyRoom.pill.joinCodePlaceholder` and is
  serialized in the English table with the same `ABCD-EFGH` value. The
  eight-character alphanumeric input constraint is unchanged. Applied rule:
  route visible Party Room copy through the configured localization system.
  Unmet rules: Editor seed execution/import, locale switching, visual and
  device acceptance remain open; tests were not run. See
  `docs/acceptance/A7_LOCALIZATION_PARTY_ROOM.md`.

- 10 October Part 7 platform-conditional source builds: the current Runtime C#
  project compiled under `UNITY_ANDROID;DEVELOPMENT_BUILD` and `UNITY_IOS`, each
  with 0 errors and 41 existing warnings. Applied rule: compile the conditional
  Android/iOS C# paths. Unmet rules: these are not native player builds and do
  not verify plugins, haptics, layout, performance, or device runtime; tests
  were not run. See `docs/acceptance/PART7_PHYSICAL_UI_SCALE.md`.

- 10 October Part 7 graphics settings: Dynamic Resolution now defaults off in
  `GraphicsConfig`; the Graphics card shows a disabled, localized row explaining
  that adaptive scaling is not configured in this build. It does not claim or
  apply runtime scaling. Preset FPS and render-scale labels now read the same
  `GraphicsConfig` values applied by `GameBootstrap`. Runtime and settings
  fallback targets and the Editor config generator now share defaults from that
  config type; serialized target values are unchanged. The latest Editor project
  build succeeded with 0 errors and 41 existing Runtime warnings; no tests were
  run.
  The card also states that Performance disables shadows while Balanced and
  Cinematic enable them; no separate override conflicts with Gate G. Applied
  rule: expose the supported shadow behavior truthfully in the graphics
  settings. The localized policy label is statically seeded and serialized.
  Applied rules: unsupported settings need a truthful disabled state and
  reason; displayed targets must use their configured values. Unmet rules:
  adaptive-resolution policy, runtime scaling, and device performance
  acceptance remain open.
  See `docs/acceptance/PART7_PERFORMANCE_BUDGET_PROBE.md`.

- 10 October Unity compile repair for body-rig cosmetics: `CosmeticLook.Apply`
  renamed the inner visual-root transform local from `rig` to `rigRoot`, avoiding
  a CS0136 scope collision with the later selected-rig value. Selected-rig
  procedural fitting and the shared cosmetic/gameplay parity rule are unchanged.
  `dotnet build ProjectHotdrop/Hotdrop.Editor.csproj -m:1 -v:q` then succeeded
  with 0 errors and 41 existing Runtime warnings. Applied rule: rig-specific
  cosmetic fitting must not change shared gameplay behavior. Unmet rules:
  authored male/female character meshes, Gate G visual parity review, and Unity
  import/runtime/device acceptance remain open; tests were not run. See
  `docs/acceptance/STEP_E.md`.

- 10 October A7 pad-study facilitator localization: the hidden Home panel now
  routes its title, cohort controls, tester actions, data-export instruction,
  no-reward protocol notice, and changing status line through the existing
  `home.*` localization table. Cohort IDs, local preferences, telemetry, tutorial
  reset action, and the zero-reward instruction remain unchanged. Fifteen English
  keys are serialized with unique IDs; source/shared-data/English values were
  statically matched. Applied rule: localize authored Home/facilitator text while
  preserving study protocol and behavior. Unmet rules: Editor compilation,
  Unity import/re-save, translated layout, and device review remain open; tests
  were not run. See `docs/acceptance/A7_LOCALIZATION_HOME.md`.

- 10 October A7 Support localization follow-up: the Support footer's arena
  tagline now uses the existing `support.*` localization lookup and its English
  value is serialized with a stable ID in both checked-in UIStrings assets.
  Applied rule: authored Support UI copy goes through Unity Localization while
  retaining the existing English fallback and visible wording. Unmet rules:
  translated-locale wrapping, Unity import/re-save, and device review remain
  open; no tests were run. See `docs/acceptance/A7_LOCALIZATION_SUPPORT.md`.

- 10 October Part 8 server replay capture cadence: after a server tick exceeds
  the configured 100 ms sample interval, authoritative replay capture now emits
  at most one current-state snapshot and keeps only the fractional remainder.
  This avoids near-duplicate burst samples because historical poses are not
  available to interpolate. No dedicated sampler test exists and tests were not
  run. A later restore-enabled Editor project build compiled the current Runtime
  C# source with 0 errors and 41 existing warnings; six-client, performance, and
  device acceptance remain unverified. Applied
  rule: maintain the bounded 10 Hz server-owned replay sampling cadence. Unmet
  rules: persistent review/playback and anti-cheat remain gated on ownership,
  reviewer access, and retention decisions; multi-client/performance/device
  validation remains outstanding. See
  `docs/acceptance/PART8_SERVER_REPLAY_CAPTURE.md`.

- 10 October Amendment v2.1 body-rig follow-up: rig selections save through
  the existing authenticated profile equip slot. Startup fetches the account
  rig in the background; it hydrates before routing when possible or refreshes
  a still-open, untouched first-run choice. Local selections pending save take
  precedence. The existing single entitlement and SKU now carry a both-rig
  support mask in inventory and bundle config;
  body-style ownership is checked against the selected rig and procedural
  adornments fit the selected silhouette. The endpoint accepts only
  `rig_male` and `rig_female`. EF generated a default-both-rigs inventory
  migration; an idempotent PostgreSQL script was generated but not applied.
  Backend build passed with 0 errors and one NuGet vulnerability-feed warning.
  The initial Editor restore attempt at that time failed before generation due
  missing `WorkloadAutoImportPropsLocator` and `WorkloadManifestTargetsLocator`
  SDK components; a later restore-enabled build succeeded after the local-name
  collision fix documented above. No tests, live
  service/store calls, Unity imports, or
  device checks were run. Separate authored character meshes, Gate G visual
  parity evidence, production price parity, and live purchase behavior remain
  open; see `docs/acceptance/STEP_E.md`.

- 10 October Gate G Cinematic SSAO shader-variant follow-up: the shared renderer
  already had the feature saved inactive and the atmosphere enabled it only for
  Cinematic, matching the `02_ART_TOKENS.json` tier entry. The shared URP asset
  was stripping the Screen Space Occlusion, interleaved, and low-sample shader
  keywords required by that configured effect. `SetupMobilePresentation` and
  `WorldArtSetup` now retain the on/off variants and configured Cinematic
  keywords while continuing to strip unused SSAO variants. Editor C# build
  passed with 0 warnings and 0 errors. No tests or render captures were run.
  An Android Development Build attempt stopped before building after Unity
  Licensing timed out and failed to reconnect; the existing APK predates this
  change. Actual player output and device cost remain open. Dynamic resolution
  is still an independent open graphics
  settings requirement. See `docs/acceptance/PART0_SWITCHYARD_ATMOSPHERE_TIERS.md`
  and `docs/acceptance/PART7_PERFORMANCE_BUDGET_PROBE.md`.

- 10 October Switchyard texture-source follow-up: the two source-unknown roof
  color maps have been moved intact out of `Resources` into
  `Assets/Art/UnverifiedRooftopTextures`, and `SwitchyardGenerator` now loads
  two project-original generated replacements. The new maps have repeating,
  mipmapped import settings and Android ASTC 6x6 overrides. Their prompt and
  terms provenance is recorded in
  `docs/acceptance/PART0_SWITCHYARD_PROJECT_ORIGINAL_TEXTURES.md`. Runtime C#
  build passed with 0 errors and 41 existing warnings; Editor C# build passed
  with 0 errors and 80 existing Unity/Fusion/Photon warnings. No tests, Unity
  imports/renders, or device checks were run. Dedicated normal/roughness maps,
  in-engine material review, and uniqueness/legal clearance remain open.

- 10 October Gate G profile-sky follow-up: the arena now selects the matching
  A/B/C panoramic material, backed by the already-registered Poly Haven CC0
  sky assets; profile sky exposure remains controlled by the atmosphere grade,
  and the procedural sky remains a fallback. The previously empty material
  texture slots/keywords are assigned and `WorldArtSetup` writes matching
  settings. Static checks matched all three material texture references to the
  asset `.meta` GUIDs. Runtime build passed with 0 errors and 41 existing
  warnings; Editor build passed with 0 warnings/errors. See
  `docs/acceptance/PART0_SWITCHYARD_ATMOSPHERE_TIERS.md`. No tests, Unity
  captures, or device runs were performed. Horizon alignment and Gate G visual
  scoring remain open.

- 10 October Gate G atmosphere follow-up: Balanced and Cinematic graphics
  presets now explicitly enable Unity's full shadow quality instead of
  restoring the startup Performance level's disabled shadow setting.
  `SetupMobilePresentation` preserves shared URP HDR/soft-shadow support and
  writes the approved 45 m, two-cascade, 1024 map setup. The A/B/C
  `SwitchyardAtmosphere.Grade` values are now mirrored in
  `02_ART_TOKENS.json`. See
  `docs/acceptance/PART0_SWITCHYARD_ATMOSPHERE_TIERS.md`. Runtime C# build
  passed with 0 errors and 41 existing warnings; Editor C# build passed with
  0 warnings/errors; the token JSON parsed with all three profiles. No tests,
  Unity captures, or device runs were performed. Device performance and Gate G
  visual scoring remain open.

- 10 October pre-follow-up acceptance reconciliation: at that point the
  two-rig first-launch choice and Appearance change control were already
  present in `GameBootstrap`, `OfflineHomeScreen`, `PlayerProductScreens`, and
  `AppearanceLockerView`; existing PlayMode source checks cover selection
  timing and control reachability. That reconciliation identified local-only
  storage and missing variant metadata, both addressed in the later body-rig
  follow-up above. Authored mesh parity, captures, builds, and runtime evidence
  remain open. See `docs/acceptance/STEP_E.md`.

- 10 October audio-caption follow-up: remote player footstep transitions now
  publish localized, priority-20 captions within the existing 50 m spatial
  source range, at most once per 1.6 s; local-player step sounds are unchanged
  and do not caption themselves. The English localization seed/table then had
  1,411 entries. The focused static table checks and serial Network/Editor
  builds passed (Network: 0 errors, 121 existing warnings; Editor: 0 warnings,
  0 errors). Unity asset import, runtime caption behavior, audio playback, and
  device acceptance remain unverified. Tests were not run. See
  `docs/acceptance/PART6_CRITICAL_CUE_CAPTIONS.md` and
  `docs/acceptance/A7_LOCALIZATION_TABLE_SEED.md`.

- 10 October follow-up: `NetConfigs.Pad` now validates the full authoritative
  pad asset, including capacity counts, lifecycle, health, geometry, launch,
  placement, exclusion, and air-steering values, in addition to cooldowns.
  Existing balance values and separate arena/per-player placement caps are
  unchanged. Unity compile, malformed-config coverage, multiplayer, and device
  acceptance remain open; tests were not run. See
  `docs/acceptance/PART8_PAD_CONFIG_VALIDATION.md`.

- 10 October follow-up: the fixed-message quick-chat wheel is now hard-disabled
  until an approved per-account age-eligibility contract is implemented and
  enforced server-side. The client cannot open/select/display messages; the
  server clears the disabled input bit (so it cannot count as AFK activity) and
  rejects broadcasts; the mute row is hidden. Contextual ping remains available
  with its pre-wheel touch behavior. The age policy, runtime/touch acceptance,
  and multi-client delivery remain open; tests were not run. See
  `docs/acceptance/PART6_QUICK_CHAT.md`.

- 10 October follow-up: the authoritative match now loads `ProgressionConfig`
  before play and rejects missing, negative, non-finite, or overflowing AFK /
  disconnect substitution windows. AFK handling and disconnect takeover no
  longer fall back to server literals; the checked-in 30/15/5-second values and
  valid-config behavior are unchanged. Unity compile and runtime/multiplayer
  acceptance remain open; tests were not run. See
  `docs/acceptance/PART8_ABSENCE_CONFIG_VALIDATION.md` and
  `docs/acceptance/PART1_RECONNECT_POLICY.md`.

- 10 October follow-up: `NetConfigs.Map` now requires a structurally valid map
  asset before network play, including finite/in-order bounds, finite objective
  and spawn positions, valid spawn/platform dimensions, and a positive exit
  count. `MapConfig.SpawnExit` now derives wrapping from `exitsPerSpawn` while
  preserving the current three-exit layout. No geometry values changed. Unity
  compile, runtime spawn, and device verification remain open; tests were not
  run. See `docs/acceptance/PART8_MAP_CONFIG_VALIDATION.md`.

- 10 October follow-up: network weapon configs now fail closed on missing ARC,
  ZIP, or BRICK assets, slot/enum mismatches, malformed damage falloff ranges,
  invalid cadence/count fields, and non-finite or negative balance values and
  recoil keys. Existing assets are unchanged. Tests were not run; Unity script
  compilation remains blocked before C# compilation by licensing initialization.
  See `docs/acceptance/PART8_WEAPON_CONFIG_VALIDATION.md`.

- 10 October follow-up: `NetConfigs.Rules` now rejects a missing rules asset,
  non-finite or negative match-flow, scoring, ping, and endgame settings,
  malformed Relay zone schedules (null/empty, invalid boundaries or zone IDs,
  gaps/overlaps, or an end time that differs from regulation), and invalid
  shared aim distances. It preserves the current configured six-zone,
  180-second schedule and does not copy rotation values into code. Unity script
  compilation failed before C# compilation because Licensing initialization
  timed out in this environment; tests and multiplayer acceptance were not run.
  See `docs/acceptance/PART8_RELAY_SCHEDULE_VALIDATION.md` and
  `docs/acceptance/PART8_AIM_RAY_CONFIG.md`.

- 10 October follow-up: authoritative network config loading now fails closed
  when `MovementConfig` is missing, health/capsule dimensions are non-finite or
  non-positive, or movement, recovery, protection, and respawn values are
  non-finite or negative. Existing values are unchanged. A Unity CLI batch
  startup was attempted but licensing initialization failed before script
  compilation; the Unity log also records sandbox denial while reading BIOS
  serial information. Tests and multiplayer/device checks were not run. See
  `docs/acceptance/PART8_MOVEMENT_INPUT_SANITIZATION.md`.

- 10 October compile-evidence follow-up: after subsequent Switchyard validation
  changes, generated `Hotdrop.Editor.csproj` and `Hotdrop.Network.csproj`
  builds compiled shared Runtime sources with 0 errors and 41 existing Runtime
  warnings each. This closes generated-project source compilation for the
  movement-config follow-up; Unity Editor import/player compilation, runtime,
  tests, multiplayer and device checks remain open. The earlier Unity CLI
  Licensing failure is unchanged. See
  `docs/acceptance/PART8_MOVEMENT_INPUT_SANITIZATION.md`.

- 9 October follow-up: network pad config loading now fails closed when the
  required `PadConfig` is absent or its recharge, launch-weapon-lockout, or
  activation-lockout duration is negative or non-finite. Zero remains valid and
  configured balance values are not changed. `Hotdrop.Network.csproj` built
  with 0 warnings and 0 errors; tests and multiplayer acceptance were not run.
  See `docs/acceptance/PART8_MOVEMENT_INPUT_SANITIZATION.md`.

- 9 October follow-up: online queue, join, and reconnect continuations now carry
  a flow generation. Cancel/return-home/new journey invalidates stale network
  completions, late Fusion sessions are closed, and old screen, banner, and
  presenter callbacks cannot mutate a replacement journey. Reconnect shutdown
  waits stop when cancelled. The network project compiled after serial restore
  with 0 errors and 121 existing Unity/Fusion/runtime warnings; the incremental
  rebuild after the final guards had 0 warnings and 0 errors. Unity runtime
  acceptance is pending; tests were not run. See
  `docs/acceptance/PART1_QUEUE_ABANDONMENT_TELEMETRY.md`.

- 9 October follow-up: the existing Ping control still sends its contextual
  marker on tap and opens a localized eight-message squad quick-chat wheel on
  hold. Clients send only an allow-listed message index; the server supplies
  sender slot/team and broadcasts it to teammates. Added a persistent local
  quick-chat mute setting. Network build succeeded with 0 errors and 41
  existing Runtime warnings. The Editor build was reported by Claude Code as
  0 warnings/errors; a local recheck could not complete because Unity's
  generated `Temp/obj/Hotdrop.Editor/project.assets.json` disappeared between
  restore and build. Multi-client delivery, touch, and layout acceptance remain
  pending; the required age gate is not implemented because no approved
  age-eligibility contract exists, so the wheel must not ship before that gate
  is resolved. Tests were not run. See `docs/acceptance/PART6_QUICK_CHAT.md`.

- 9 October follow-up: public account-deletion confirmation now atomically
  claims its single-use token and runs the claim plus deletion in one database
  transaction. Authenticated in-app deletion also commits party cleanup, player
  data removal, and the retained-purchase audit record together. Production
  also requires a configured HTTPS public origin, so verification links do not
  depend on the incoming request Host header. Concurrent public submissions can
  no longer both consume the same link, and a failed deletion does not burn it.
  The email expiry text now uses the same clamped lifetime as the stored token.
  Deletion also removes matchmaking tickets, contract goals, Flight Seal grants,
  and mastery progress, which were previously left keyed to the deleted player.
  Backend and backend test-project builds succeeded
  with 0 errors; 12 NuGet-feed/nullable warnings were reported, including ten
  in unchanged auth/party test files. Tests and live PostgreSQL concurrency
  checks were not run. See
  `docs/acceptance/PART5_ACCOUNT_DELETION_EMAIL_DELIVERY.md`.

- 9 October follow-up: Production backend startup now rejects the local
  inspection-only email outbox, insecure SMTP, invalid sender/port settings, and
  partial SMTP credentials. If a public account-deletion confirmation email
  fails to send, the page now returns a generic 503 retry message instead of
  saying to check for an email. Publisher delivery configuration and live
  verification remain open; see
  `docs/acceptance/PART5_ACCOUNT_DELETION_EMAIL_DELIVERY.md`.

- 9 October follow-up: online match loading now shows the localized
  `SWITCHYARD · RELAY` venue/mode label alongside the joining state. Addressables
  is now present transitively for Localization and has generated local settings,
  but remote catalog building is disabled and the remote load path is undefined;
  there are no authored downloadable content groups or configured asset host.
  Download progress and resume behavior remain unavailable until those inputs
  exist. See `docs/acceptance/PART1_MATCHMAKING_STATE_TRUTH.md`.

- 9 October follow-up: matchmaking startup now validates the rating-window,
  bot-fill, and worker-poll settings. The widening calculation saturates at its
  configured cap without integer overflow during long waits; defaults are
  unchanged. See `docs/acceptance/PART1_MATCHMAKING_CONFIG.md`.

- 9 October follow-up: backend startup now trims and requires the allocator's
  configured Fusion region and requires a positive match-allocation capacity.
  A blank region could make the dedicated server and clients independently
  select different regions; zero capacity could leave an enabled queue unable
  to allocate. Pre-match measured selection remains open because only `eu` is
  provisioned; see
  `docs/acceptance/PART1_REGION_CONFIG.md`.

- 9 October follow-up: cancelled online sessions now retain `result=cancelled`
  in client completion telemetry instead of being counted as losses. The
  network project compiled with 0 errors and 121 Unity/Fusion warnings; tests
  and analytics ingestion/dashboard checks were not run. See
  `docs/acceptance/PART5_PRODUCT_LOAD_FAILURE_STATES.md`.

- 9 October follow-up: maintenance ETAs are now returned only when the
  operator value includes an explicit UTC zone and parses successfully; valid
  values are normalized before client display, while malformed/local-time
  values are omitted. Startup, Home, and Party Room status labels also render
  operator copy as plain text. Backend and runtime builds plus remaining
  live-service/device acceptance are recorded in
  `docs/acceptance/PART1_MAINTENANCE_NOTICE.md`.

- 9 October follow-up: the backend now rejects a supplied malformed
  `X-Client-Protocol` header instead of letting it skip the configured minimum
  protocol check, and it fails startup if the configured minimum is
  non-positive. Unity also treats a non-positive client protocol asset as
  unconfigured. Numeric outdated clients still receive the existing 426 and
  store-link response. Missing headers are rejected on player APIs; only
  authenticated dedicated-server routes and public browser account-deletion
  pages/forms are exempt. Build and route acceptance are recorded in
  `docs/acceptance/PART8_PROTOCOL_COMPATIBILITY.md`.

- 9 October follow-up: protocol-gated player APIs now reject an omitted
  `X-Client-Protocol` header, closing the bypass around the minimum-version
  check. The dedicated-server service routes and public browser account-
  deletion pages/forms keep their separate contracts. Backend compilation
  succeeded; automated tests and live route checks were not run. See
  `docs/acceptance/PART8_PROTOCOL_COMPATIBILITY.md`.

- 9 October follow-up: online Relay now plays the existing countdown and GO
  cues at the replicated phase transitions, matching offline Relay. When
  gameplay captions are enabled, both paths also publish the visible countdown
  number and localized GO label. Offline and online results publish one
  authoritative outcome cue/caption using the localized Results label; online
  victory is no longer played twice, and cancelled matches do not announce an
  outcome. Victory/defeat use the announcer priority tier. Builds and remaining
  runtime/device acceptance are recorded in
  `docs/acceptance/PART6_CRITICAL_CUE_CAPTIONS.md`.

- 9 October follow-up: replicated online pad placement now plays both spatial
  deployment and charging cues on clients, matching offline placement. Existing
  caption range, localization, and settings behavior apply to the charging cue;
  headless servers still skip client audio. Network build and device acceptance
  are recorded in `docs/acceptance/PART6_CRITICAL_CUE_CAPTIONS.md`.

- 9 October follow-up: Party Room followers now enter the existing online
  join flow when their shared ticket is either `searching` or already
  `matched`. The leader's start-search action is disabled during maintenance,
  queue pause, unknown state, or a failed status check, and is restored only
  after a successful `idle` response. Operator-provided maintenance details are
  shown when present. Network build and live handoff acceptance are recorded
  in `docs/acceptance/PART1_MATCHMAKING_STATE_TRUTH.md`. Network compiled with
  0 errors and 43 Unity/runtime warnings; the PlayMode test assembly compiled
  with 0 errors and 41 warnings. No tests or live session were run.

- 9 October follow-up: Production backend startup now requires a complete
  `GameServer:ServerKeys` map with a unique secret per server; the legacy shared
  key is accepted only outside Production. This prevents a production deploy
  with missing per-instance credentials from silently falling back to one
  shared key. The compose deployment now requires an operator-managed key map
  file. Backend build and deployment acceptance are recorded in
  `docs/acceptance/PART8_SERVER_IDENTITY.md`; tests, compose validation,
  secret-manager, live server-pool, and rotation checks remain open.

- 9 October follow-up: the Development Build performance probe now samples
  Android process PSS once per second and reports the highest observed sample
  for each measurement window against the existing Performance memory target.
  Unsupported platforms report PSS as unavailable; device, iOS, and full-matrix
  memory acceptance remain open. See
  `docs/acceptance/PART7_PERFORMANCE_BUDGET_PROBE.md`.

- 9 October follow-up: gameplay captions now receive the same existing priority
  tier as their audio cue. A lower-priority caption cannot replace an active
  higher-priority threat caption; equal or higher-priority cues still update
  it. No caption timing, audio mix, or gameplay rule changed. Editor build and
  remaining in-game/device acceptance are recorded in
  `docs/acceptance/PART6_CRITICAL_CUE_CAPTIONS.md`.

- 9 October follow-up: the live radar now renders living teammates with an
  ally chevron rotated to their facing direction and an up/down badge when
  their vertical separation reaches half the configured Switchyard platform
  height. Networked facing yaw is replicated by state authority; teammate-only
  filtering and ping/reveal rules are unchanged. Network, Editor, and PlayMode
  test assemblies compile with 0 errors. No tests, in-game capture, live
  network session, or device checks were run. See
  `docs/acceptance/PART5_RADAR_TEAM_CUES.md`.

- 9 October follow-up: Cockpit Mode now fades only the radar panel and roster
  for combat/endgame, leaving adjacent latency, performance, and identity
  readouts visible. The five-second roster idle fade applies independently of
  Cockpit Mode. Editor compilation succeeded with 0 errors and 41 existing
  Runtime warnings; no tests or visual/device checks were run. See
  `docs/acceptance/PART9_ENDGAME_HUD_TRIM.md`.

- 9 October follow-up: offline ARC, ZIP, and BRICK shots now use the same
  spatial layered mixer as networked shots, including the existing mechanism
  transient and environment tail. Editor compilation is recorded in
  `docs/acceptance/PART6_OFFLINE_WEAPON_AUDIO_LAYERS.md`; device audio and mix
  acceptance remain open.

- 9 October follow-up: online pad-study conditions can now be selected per
  dedicated-server process from the existing cohort labels, replicated into
  match state, enforced by server pad validation, and attached to authenticated
  pad fact rows. Assembly build results and remaining deployment/KPI/retention
  inputs are recorded in
  `docs/acceptance/PART8_TRUSTED_PAD_STUDY_COHORT.md`.

- 9 October follow-up: Relay personal-best counters now use separate local
  `Best.Offline.*` and `Best.Online.*` preference keys. `ResultsTrustState`
  chooses the namespace, so practice stats cannot set the online local best or
  vice versa. Ambiguous legacy unscoped values remain stored and are not
  classified. Offline challenge records remain device-local and version,
  control, and difficulty scoped. The Editor build succeeded with 0 errors and
  41 existing warnings; no tests or live matches were run. See
  `docs/acceptance/PART1_PERSONAL_BEST_CONTEXT.md`.

- 9 October follow-up: the Development Build performance probe now captures
  peak Unity draw-call and rendered-triangle profiler counters and compares them
  to the existing Performance/Low config targets. The targets remain
  report-only, and no device measurements or thermal run were performed.
  Normal and `DEVELOPMENT_BUILD` Editor compilations succeeded; see
  `docs/acceptance/PART7_PERFORMANCE_BUDGET_PROBE.md`.

- 9 October follow-up: Offline Challenge results now capture timeout,
  elimination, accuracy-threshold failure, and objective counts in the result
  data. The result screen explains those causes and shows objective progress;
  accuracy thresholds are visible wherever configured. Previous-best delta
  comparisons remain context-matched by challenge version, difficulty, and
  controls, and unsuccessful attempts remain unsaved. Successful results show
  a small badge using the active presentation profile's objective color.
  Outcome-matched actions remain first in order, and the final action returns
  to the library. Three localization keys and their English table rows were
  added. Editor build and source/seed/table audits are recorded in
  `docs/acceptance/A7_LOCALIZATION_OFFLINE_RUNTIME.md`; no result capture,
  tests, or device checks were run.

- 9 October follow-up: a second Part 5 copy audit found functional Contracts
  and Mastery labels still exposed fake terminal jargon and backend terms. The
  78 updated messages now name match requirements, actions, rewards, progress,
  and badge visibility directly; contract cards render sentence-case names,
  and the active-count label matches the existing single-active-contract rule.
  Preview/unavailable copy remains honest, and the progression and reward
  catalog gates remain closed. Editor build and focused source/ID/table audit
  are recorded in `docs/acceptance/A7_LOCALIZATION_CONTRACTS_MASTERY.md`;
  Unity import, visual fit, translations, and device acceptance remain open.

- 9 October follow-up: Profile's visible terminology now uses plain language as
  required by Bible Part 0 §0.3, retaining the private rating, verified match
  record, and permanent progression rules. Runtime fallbacks and the English
  seed now use matching, direct labels for profile identity, match count,
  cosmetics, and details. Chapter names also match between Profile and the
  offline challenge library. The Editor build succeeded with 0 errors and 41
  existing warnings; a focused static audit found all 24 modified localization
  entries matching their serialized table values and preserving unique IDs.
  Editor import/re-save, visual wrapping, tests, locale switching, and device
  checks remain open. See
  `docs/acceptance/PART5_PROFILE_HIERARCHY.md`.

- 9 October follow-up: Support navigation now labels its existing destinations
  and back action as Back, Appearance, Profile, and Goals instead of Operations,
  Armory, Telemetry, and Pass. Navigation callbacks are unchanged. The localized
  seed and fallbacks were updated and statically matched at all four existing
  key IDs. The restore-enabled Editor build passed with 0 errors and 121
  Unity/Fusion/runtime warnings. Unity Editor import and visual fitting remain
  unverified because licensing initialization exits before the seed command.
  See `docs/acceptance/A7_LOCALIZATION_SUPPORT.md`.

- 9 October follow-up: inspection of the saved A/B/C profile captures found the
  permanent progression guarantee text drawing below its fixed card and the
  accreditation heading colliding with the phase label; expanded metric footer
  labels also crossed adjacent cards. The guarantee now uses TMP's wrapped
  preferred height to size its container and progression card; the following
  summary row starts after the tallest card. Heading, phase, alias, XP, credit,
  and metric copy are clipped to their allocated widths, and metric tag boxes
  no longer overlap. Unity Editor C# build succeeded with 0 errors and 41
  existing warnings. No tests, fresh captures, or device checks were run. See
  `docs/acceptance/PART5_PROFILE_TYPE_SCALE.md`.

- 9 October follow-up: typed `BackendResult<T>` responses now retain stable
  backend `error` codes across parsing and session-error remapping. Party
  endpoints now return stable codes for missing parties, membership, and
  non-leader queue attempts while keeping the existing HTTP statuses. Profile
  lookup and cosmetic equip now
  return stable missing-profile and unowned-cosmetic codes while preserving
  404/403. Other authenticated missing-profile responses reuse the same code;
  maintenance and capacity use existing localized messages. Recognized party,
  matchmaking, account, support-report, cosmetic, contract, and gated commerce
  codes use seeded localized messages; unknown codes retain the generic
  HTTP-state message.
  Unity Editor build
  succeeded with 0 errors and 41 existing warnings; the backend build succeeded
  with 0 errors and one NuGet feed warning. Static audit found 19 backend-code
  keys with no missing seeds. Editor table seeding, live error simulation,
  translated layout, and device acceptance remain open. No tests were run. See
  `docs/acceptance/A7_LOCALIZATION_BACKEND_CODES.md`.

- 9 October follow-up: `PlayerProductScreens` now routes its authored page
  titles, party/profile/appearance/account states, recovery and reporting
  feedback, and inventory controls through `product.*` localization lookups.
  Dynamic account/cosmetic data stays dynamic and service-provided messages
  remain untouched. English seed coverage passed for 105 literal and
  enumerated keys; Editor build succeeded with 0 errors and 41 existing
  warnings. Unity table seeding, translated layout review, and device
  acceptance remain open. No tests were run. See
  `docs/acceptance/A7_LOCALIZATION_PRODUCT_SCREENS.md`.

- 9 October follow-up: Home's authored navigation, More menu, Home goal
  progress, runner-choice text, and shared `NextGoal` copy now use
  `home.*`/`home.goal.*` localization keys. The live account/network state and
  offline/tutorial routes are unchanged. Static audit found 82 expected keys
  with no missing/duplicate seeds; Editor build succeeded with 0 errors.
  At that stage, operator-provided connection detail and the Unity table seed,
  translated locale review, and device acceptance remained open. See
  `docs/acceptance/A7_LOCALIZATION_HOME.md`.

- 9 October follow-up: runner-card venue mirroring and runner offset now use
  explicit presentation identity instead of checking localized caption text.
  This keeps translated copy from changing the preview. Editor build succeeded
  with 0 errors and 41 existing warnings; no tests or visual/device checks were
  run. A static table audit found 146 serialized English keys versus 1,245
  current seed keys at that point; the follow-up table-seed step is documented
  in `docs/acceptance/A7_LOCALIZATION_TABLE_SEED.md`.

- 9 October follow-up: all 1,407 runtime English localization seed entries are
  now serialized in the shared-data and English table assets. All keys and IDs
  align, and values match after YAML line-continuation folding against the
  compiled `SetupLocalizationPilot` entries; existing
  IDs were preserved and collision-free negative IDs were added for new keys. The
  Unity Editor batch process stalled in its Licensing Client reconnect loop,
  so import/re-save and runtime validation remain open. See
  `docs/acceptance/A7_LOCALIZATION_TABLE_SEED.md`.

- 9 October follow-up: Match HUD score, zone, roster, pad, result and critical
  sound-caption copy use the configured localization table; touch controls,
  layout-editor captions, pad previews, ping markers, and offline-challenge HUD
  state are localized as well. Initial scene zone labels are also seeded.
  Touch geometry, editor action order and gameplay state are unchanged. The
  Editor build succeeds with 0 errors and 41 existing Runtime warnings. The
  Unity Editor seed command still cannot update the table here; locale/device
  review remains open. No tests were run. See
  `docs/acceptance/A7_LOCALIZATION_HUD.md`.

- 9 October follow-up: Match Search's queue/reservation/reconnect journey now
  localizes authored states and maps stable `BackendError` and Fusion shutdown
  values to seeded keys. State transitions, timings, cancellation, and
  operator-supplied maintenance wording are unchanged. Editor build result and
  remaining acceptance are recorded in
  `docs/acceptance/A7_LOCALIZATION_ONLINE_JOURNEY.md`.

- 9 October follow-up: the teammate roster now fades after five seconds without
  player input and returns on the existing 200 ms fade when input resumes. The
  five-second threshold is the HUD spec's estimate and still needs in-game
  tuning. Runtime compilation succeeded; no tests, visual captures, or device
  checks were run. See `docs/acceptance/PART9_ENDGAME_HUD_TRIM.md`.

- 10 October follow-up: the Cockpit/HUD fade tuning values (30 m combat range,
  15% faded opacity, 200 ms fade, 5 s roster idle threshold) moved from
  `UiuxHud` literals into `MatchRulesConfig` with unchanged defaults; the HUD
  reads active match rules with a resource fallback and clamps unsafe values.
  They remain estimates pending in-game measurement. No build, tests, visual
  captures, or device checks were run. See
  `docs/acceptance/PART9_ENDGAME_HUD_TRIM.md`.

- 9 October follow-up: the nine-step guided training card, step titles and
  instructions, contextual Show Me hints, completion label, and skip
  confirmation now use seeded `tutorial.*` keys. Step progression, actions,
  and timing are unchanged. Build and key-audit results are recorded in
  `docs/acceptance/A7_LOCALIZATION_TUTORIAL.md`.

- 9 October follow-up: the purchase-gated cosmetic preview now localizes its
  bundle titles/themes, concept contents, guard, transparency note and return
  action. No purchase action or billing behavior was added. Build and key-audit
  results are recorded in
  `docs/acceptance/A7_LOCALIZATION_SHOP_PREVIEW.md`.

- 9 October follow-up: authored Contracts and Mastery UI plus client-owned
  loading/status messages now use `goals.*` localization keys. Contracts remain
  read-only while the progression feature gate is off; reward catalog and
  server authority rules are unchanged. Static audit found 155 expected keys
  with no missing/duplicate seeds; Editor build succeeded with 0 errors. Unity
  table seeding, translated locale review, and device acceptance remain open.
  See `docs/acceptance/A7_LOCALIZATION_CONTRACTS_MASTERY.md`.

- 9 October follow-up: Support's authored navigation, identity/recovery,
  report form, compliance links, and two-step deletion copy now use
  `support.*` localization keys; dynamic account values remain data and the
  confirmation token stays `DELETE`. At that time the static audit found 109
  expected lookup keys with no missing/duplicate seeds; the later footer copy
  follow-up raises this to 110. Backend free-form status messages,
  Unity English-table seeding, translated locale review, and device acceptance
  remain open. Editor build succeeded with 0 errors and 41 warnings. See
  `docs/acceptance/A7_LOCALIZATION_SUPPORT.md`.

- 9 October follow-up: Offline Challenges now route authored library, detail,
  practice, pause, objective, tracker, result, save-recovery, and medal copy
  through the configured localization system. The 12 current challenge title
  and description pairs have config-ID-based English keys. All identified
  lookup keys are seeded. No tests, locale switch, visual review, or device
  acceptance was performed. See
  `docs/acceptance/A7_LOCALIZATION_OFFLINE_RUNTIME.md`.

- 9 October follow-up: Startup (`StartupConnectionScreen.cs`) now routes
  client-authored branding, build/frame labels, protocol guidance, connection
  states, and recovery actions through `UiDraw.L`; dynamic maintenance details
  remain service-supplied. The 18 fixed lookup keys are present in
  `SetupLocalizationPilot.cs`. `Hotdrop.Editor` compiled with 0 errors and
  41 existing Runtime warnings. The Unity seed command did not update the
  string table; no tests, locale switch, visual review, or device/network
  acceptance was performed. See
  `docs/acceptance/A7_LOCALIZATION_STARTUP.md`.

- 7 October follow-up: Preparation (Screen 07, `PreparationView.cs`) now
  routes its authored labels, objective-zone names, weapon names/roles/tips,
  stat labels, ammo units, and countdown/auto-confirm templates through
  `UiDraw.L`. Weapon descriptions use the existing config copy as fallback;
  the existing firing rules and timings are unchanged. Matching
  `preparation.*` English keys were added to `SetupLocalizationPilot.cs`. The
  Editor seed command did not refresh the table, so these keys currently rely
  on their inline fallback. No tests, locale switch, visual review, or device
  check was performed. See
  `docs/acceptance/A7_LOCALIZATION_PREPARATION.md`.

- 7 October follow-up: Results (Screen 11, `ResultsView.cs`) is now migrated
  through `UiDraw.L` for authored outcome/trust states, score and contribution
  labels, roster roles, progression notes, reward receipt formatting, and
  actions. The online receipt polling messages use the same keys. English
  fallbacks preserve current copy and behavior. The matching `results.*` seed
  entries were added to `SetupLocalizationPilot.cs`, and the localization
  prerequisite was corrected to reflect the configured Unity Localization
  package/table. The Runtime/Network/Editor assemblies compiled with 0 errors
  and 41 existing Runtime warnings. The Unity Editor seed attempt exited with
  no log and left the table unchanged; no tests, locale switch, visual review,
  or device acceptance was performed. Remaining A7 work is listed in
  `docs/acceptance/A7_LOCALIZATION_RESULTS.md`.

- 7 October follow-up: Party Room (Screen 05, `PartyRoomView.cs`) is migrated
  to `UiDraw.L` lookups, the next screen in the A7 "migrate it screen by
  screen" sequence after Match Search. Header, code-pill labels, the no-party
  entry state, in-party footer status/actions, and every seat-card string
  (role, readiness badge, description, footer role, ready-toggle) now read
  through localization keys with their existing English copy as fallback, so
  rendered text is unchanged. `SetupLocalizationPilot.cs` gained the matching
  `partyRoom.*` seed entries. `dotnet build Hotdrop.PlayModeTests.csproj`
  compiled with 0 errors and the existing 41 warnings; `dotnet build
  Hotdrop.Editor.csproj` compiled with 0 errors. The Unity Editor seed
  command, PlayMode tests, and visual/device review were not run. See
  `docs/acceptance/A7_LOCALIZATION_PARTY_ROOM.md`.
- 7 October follow-up: a state-honesty audit of the hierarchy and empty/
  loading/error/offline states across Startup, Home, Party, Profile,
  Appearance, Support/Account, Contracts/Mastery, Offline Challenges, Match
  Search/queue, in-match HUD, online disconnect/reconnect recovery, and
  Results found those screens already satisfy the criteria used throughout
  this project (truthful loading/empty/unavailable copy, a reachable retry,
  and no silent success/failure claims); no behavior change was needed there.
  The one open, concretely implementable gap found was migration of literal
  UI copy to the already-configured Unity Localization system
  (`docs/design/06_TECH.md` "Localization prerequisite", and the A7 row
  below): Match Search (Screen 06) is now migrated to `UiDraw.L` lookups
  following the same pattern as Profile/Appearance, with its English copy
  added to the `SetupLocalizationPilot` seed list. `dotnet build
  Hotdrop.PlayModeTests.csproj` and `dotnet build Hotdrop.Editor.csproj` both
  compiled with 0 errors (41 and 80 pre-existing warnings respectively); the
  Unity Editor seed command, PlayMode tests, and visual/device review were
  not run. See `docs/acceptance/A7_LOCALIZATION_MATCH_SEARCH.md`.
- Queue analytics now emits `queue_started` after an accepted or followed
  ticket, then `queue_abandoned` with reason and elapsed seconds when the user
  cancels, switches to practice, or the attempt fails before `match_joining`.
  The backend allowlist and daily session activity set include the event.
  Network and backend assemblies compile; no tests or live matchmaking run was
  performed. Sudden process termination cannot emit a client abandonment event.
  See `docs/acceptance/PART1_QUEUE_ABANDONMENT_TELEMETRY.md`.
- Unity's global real-voice limit now matches the bible's 24-voice audio budget;
  the runtime one-shot pool was already capped at 24. The project audio setting
  now also bounds voices created by gameplay sources outside that pool. No
  runtime/device mix or voice-stealing acceptance was run; see
  `docs/acceptance/PART6_AUDIO_VOICE_BUDGET.md`.
- Settings now has reset actions for Controls, Graphics, Audio, and Accessibility.
  The Accessibility reset restores its switches and Hybrid presentation profile;
  Graphics reset no longer changes larger-text preference. The Unity runtime and
  PlayMode assemblies compile with 0 errors and 41 existing runtime warnings;
  no tests or device checks were run. See
  `docs/acceptance/PART5_SETTINGS_TAB_RESETS.md`.
- Gyro Sensitivity now changes gyro input independently, persists on-device, and
  respects its existing 0.50x–2.00x slider range. Controls reset disables gyro,
  restores 1.00x, clears calibrated bias, and restores vibration. Unity runtime
  and PlayMode assemblies compile with 0 errors and 41 existing runtime
  warnings; physical gyro behavior remains unverified. See
  `docs/acceptance/PART7_GYROSCOPE_SENSITIVITY.md`.
- Look and Focus Aim sensitivity now use ten discrete saved positions over the
  specified 0.50x–2.50x range, with a selectable 1.00x Standard anchor. Existing
  continuous saved values migrate to their nearest step on load. Unity runtime
  and PlayMode assemblies compile with 0 errors and 41 existing warnings; no
  tests or device checks were run. See
  `docs/acceptance/PART7_SENSITIVITY_STEPS.md`.
- First-launch graphics selection now maps the existing low/mid/high system
  memory tiers to Performance/Balanced/Cinematic and persists the recommendation;
  manual choices remain saved, and Reset Graphics reapplies that recommendation.
  It compiled in the same Unity build. Device performance checks remain open;
  see `docs/acceptance/PART7_GRAPHICS_PRESET_RECOMMENDATION.md`.
- Objective markers are suppressed when measured panel and ping exclusions leave
  no collision-free slot; the preparation header already names the opening zone.
  The behind-camera visual capture now targets active gameplay, when the marker
  has readable HUD space. Existing PlayMode coverage was updated but not run.
- ADS sight pictures and first-person reload beats (mag-out, mag-in, bolt) are
  blocked by missing assets, not by missing code: there is no first-person view,
  no equipped weapon mesh (weapon art is 2D UI portraits plus one shared
  procedural primitive gun), no animation clip or Animator, and no per-beat
  reload audio (reload is one synthesized two-pulse cue). No beat timing is
  decided, so none was invented. No placeholder art was added. Exact blockers are
  recorded in `docs/acceptance/PART3_ADS_RELOAD_BEATS_BLOCKERS.md`.
- 7 October re-check (same day, second pass): re-inspected weapon/camera/input/
  asset code against the current working-tree diff (Focus Aim FOV blend,
  non-bot flight-mix guards, new local damage/hit/elimination captions) and
  re-ran the mesh/animation/audio asset enumeration; both are unchanged from
  the prior pass, so the ADS/reload-beat blockers above still stand and no
  new attempt was made to fill them. `dotnet build
  ProjectHotdrop/Hotdrop.PlayModeTests.csproj` still compiles 0 errors.
- Screen 08's active Relay HUD uses the exact 12/17/22 dp typography tokens
  across HUD panels, touch-control captions, world objective text, and
  live/death feedback. Acceptance and remaining visual review are recorded in
  `docs/acceptance/PART5_HUD_TYPE_SCALE.md`.
- Match Search now uses 12/17/22 dp text roles for all search-screen copy while
  retaining current queue content and actions. Its fairness/venue footer uses
  the shared safe-area insets after saved 16:9/20:9 fixtures showed edge
  clipping. Post-fix captures and device safe-area review remain open;
  acceptance is recorded in
  `docs/acceptance/PART5_MATCH_SEARCH_TYPE_SCALE.md`.
- Preparation now uses the same 12/17/22 dp roles for weapon cards, live
  objective, countdown, squad chips, and safety copy. Config-sourced weapon
  values remain unchanged; its captures and stat-containment check were not
  run. See `docs/acceptance/PART5_PREPARATION_TYPE_SCALE.md`.
- Results now uses only its 12/17/30 dp caption, value, and outcome roles;
  scoreboard values and player names remain legible at the configured body
  size. Existing journey/session-lifetime coverage was read but not run;
  16:9/20:9 review remains open. See
  `docs/acceptance/PART5_RESULTS_TYPE_SCALE.md`.
- Appearance Locker text now uses only the 12/17/30 dp caption, UI, and title
  roles while retaining existing inventory, equip, retry, and disclosure
  behavior. Saved 7 October A/B/C captures exposed header chip/title and stage
  equipment-label collisions; header regions are now reserved and stage text
  is truncated within two columns. Editor build succeeded with 0 errors and
  41 existing warnings; tests and fresh captures were not run, so 20:9 and
  palette-contrast review remain open. See
  `docs/acceptance/PART5_APPEARANCE_TYPE_SCALE.md`.
- The training instruction card now uses 12/17/22 dp caption, body, and title
  roles. Existing tutorial progression, capture, and physical-size coverage
  was read but not run; in-game occlusion and safe-area review remain open.
  See `docs/acceptance/PART5_TUTORIAL_TYPE_SCALE.md`.
- Contracts and Mastery now use only 12/17/30 dp caption, body/UI, and title
  roles, including removing inline size overrides. Feature-gate/read-only and
  16:9/20:9 capture coverage was read but not run. Reward-catalog enablement
  remains gated. See `docs/acceptance/PART5_GOALS_TYPE_SCALE.md`.
- Offline Challenges now use only 12/17/30 dp caption, body/UI, and title
  roles across the library, detail, local record, and practice panels. Existing
  local-save and runtime-flow coverage was read but not run; layout review is
  open at both landscape ratios. See
  `docs/acceptance/PART5_OFFLINE_CHALLENGE_TYPE_SCALE.md`.
- Profile summary and expanded details now use 12/17/30 dp roles. The level
  label has no inline font override; its permanent-guarantee body is 17 dp
  with room to wrap. Navigation/capture coverage was read but not run. See
  `docs/acceptance/PART5_PROFILE_TYPE_SCALE.md`.
- Party Room entry and active-room text now use only 12/17/22 dp caption,
  body/action, and compact-title roles. Invite-code, readiness, membership,
  and leader queue behavior are unchanged. Existing entry/layout/physical-size
  coverage was read but not run; 16:9/20:9 fit and A/B/C contrast remain open.
  See `docs/acceptance/PART5_PARTY_TYPE_SCALE.md`.
- Shared muted-text UI now reads the selected palette's semantic muted role,
  matching values used when recoloring existing UI during A/B/C changes. A's
  muted token derives from its secondary/background colors; B/C keep their
  configured values. Runtime dependency compilation succeeded in the serial
  Network build (0 errors); cross-profile contrast and live-switch visual
  acceptance remain open. See `docs/acceptance/PART2_PROFILE_PALETTE.md`.
- Profile-switch review found four derived semantic tones re-derived at their call
  sites rather than owned by the palette, so an already-open graphic kept the
  previous profile's pressed or mixed hue after an A/B/C switch. They now live on
  `UiPresentationPalette` and are covered by the switch remap. The same review
  fixed the Mastery permanence-note wrap defect and confirmed no other long-copy
  label leaves a multi-line box unwrapped; the settings presentation card's copy
  was shortened in the working tree and no longer overflows. Runtime build 0
  errors/41 warnings; forced Network rebuild 0 errors/121 warnings; no tests or
  Unity visual review were run. See
  `docs/acceptance/PART2_PROFILE_PALETTE.md` and
  `docs/acceptance/PART2_GOALS_PALETTE.md`.
- Support, account recovery, reports, compliance, and account deletion now use
  named 12/17/22 dp roles, including density-aware input text. The two-step
  deletion and report actions are unchanged. Navigation/deletion and physical
  size coverage was read but not run; 16:9/20:9 layout and A/B/C contrast
  review remain open. See `docs/acceptance/PART5_SUPPORT_TYPE_SCALE.md`.
- Support now distinguishes account-service and wallet-load failures from
  loading, provides an in-page retry, and withholds guest/link/recovery actions
  until profile state is known. Reporting and web deletion stay available.
  Runtime C# compilation succeeded; no tests or device/network checks were run.
  Unity layout and A/B/C review and localization remain open. See
  `docs/acceptance/PART5_SUPPORT_ACCOUNT_LOAD_STATES.md`.
- Appearance now labels equipment and level data as unverified when inventory
  loaded but the profile lookup failed; the displayed choice is identified as
  the last locally saved appearance. Filter controls now separate the leading
  label and move the earned-only disclosure below the chips when the row is
  too narrow. A capture showed the prior collision; post-fix Unity visual review
  remains open. See
  `docs/acceptance/PART5_APPEARANCE_ACCOUNT_LOAD_STATE.md`.
- Profile and Appearance failed loads now show a reachable 52 px footer RETRY
  (the shared retry row was hidden under their full-panel visuals), and Profile
  placeholders read "unavailable" rather than "loading". Profile identity
  footer labels now divide the row evenly after an existing 16:9 capture showed
  overlap; a fresh Unity capture remains open. Results now receive a
  typed trust state for practice, cancellation, pending, eligible verified,
  recorded-ineligible, unavailable, and development sessions without a receipt ID; only a recorded eligible receipt shows
  "server verified." Match-cancellation is explicitly passed via ResultsTrustState from authoritative callers rather than inferred from footer text. Dev sessions without a match ID fallback to truthful unverified status signals without perpetual waiting loops. Ineligible/unavailable receipts and pending states update
  the banner, progression heading, and save note together. Party Room labels a
  failed background refresh as stale until it recovers. Runtime compilation
  succeeds with 0 errors and 41 existing warnings; tests, Unity captures, live
  receipt/network checks, and device checks were not run. Contracts/Mastery
  initial-load failures now have an in-page unavailable state with a sticky
  retry and back footer; the feature-on recovery flow still needs acceptance
  while progression is disabled in this build. See
  `docs/acceptance/PART5_PRODUCT_LOAD_FAILURE_STATES.md`.
- Home, the first-run rig choice, startup, and settings/pause generators now
  use 12/17/22 dp roles. The graphics-note auto-size cap observes the 12 dp
  floor and the passive-HUD sample no longer uses inline size markup. The
  existing 16:9 accessibility capture showed two A/B/C profile-card copy lines
  clipped; those strings were shortened and a post-fix capture remains open.
  Physical-size source was read but not run; 16:9/20:9 reflow, A/B/C contrast,
  and larger-text visual review remain open. See
  `docs/acceptance/PART5_HOME_STARTUP_SETTINGS_TYPE_SCALE.md`.
- A level-5-or-higher guest now sees a one-time account-link prompt in More
  when Google Play linking is available, routed to the existing Account flow.
  Account-state navigation coverage was read but not run; Android/device
  verification remains open. The first-purchase trigger is gated while
  purchases are disabled. A failed profile lookup now says eligibility could
  not be checked and allows the next More open to retry. See
  `docs/acceptance/PART1_ACCOUNT_LINK_REMINDER.md`.
- Disconnect-to-bot grace now comes from `ProgressionConfig` with the existing
  five-second value unchanged. Existing reconnect authority coverage was read
  but not run; multi-client and mobile background/reconnect acceptance remain
  open. See `docs/acceptance/PART1_RECONNECT_POLICY.md`.
- Backend commerce remains a scaffold: `/api/commerce/verify-purchase` records
  only `pending_verification`, does not validate a store receipt or grant an
  item, and purchases default disabled. Receipt acceptance needs configured
  publisher verification and store/legal release setup; do not enable the
  purchase flag before that lifecycle exists.
- 7 October follow-up: Results `DEFEAT` uses the active profile's warning role,
  keeping enemy coral for enemy identity and damage cues. Runtime build passed
  with 0 errors and 41 warnings. The Part 2 source acceptance records the
  grounded profile enemy-token contrast values and remaining visual review.
- 7 October follow-up: the in-match death panel and elimination-confirmation
  toast resolve participant names from the live match view while visible, so a
  Stream-Safe preference change does not leave a cached alias on the HUD. The
  runtime build passed with 0 errors/41 warnings and the serial network build
  passed with 0 errors/80 warnings. Unity toggle/capture review remains open;
  see `docs/acceptance/PART9_STREAM_SAFE_DISPLAY.md`.
- 7 October follow-up: the backend client now rejects unreadable 2xx response
  bodies and converts Unity request exceptions/data-processing failures into
  failed results, so product screens cannot report success without a usable
  response. Home now distinguishes a saved account session from a device with
  no account, and labels failed profile lookup as unavailable. Google Play
  sign-in now distinguishes provider exceptions from user cancellation.
  Runtime build passed with 0 errors/41 warnings; the Network build passed with
  0 errors/121 warnings. No simulated outage/empty-body or native SDK result
  check was run; see
  `docs/acceptance/PART5_PRODUCT_LOAD_FAILURE_STATES.md`.
- 7 October follow-up: startup availability failures and unknown service states
  now open the existing Retry / Play Offline recovery state instead of leaving
  the launch screen labeled as synchronizing when the result arrives before the
  screen advances. The brief automatic advance remains for a pending check so
  slow service does not block local play. Runtime build passed; no outage
  simulation or device capture was run. See
  `docs/acceptance/PART1_MAINTENANCE_NOTICE.md`.
- 7 October follow-up: a Part 5 state-honesty audit of player-facing screens
  outside the already-closed Profile/Appearance/Support/Results/Party/
  Contracts-Mastery gaps found one concrete mismatch: Offline Challenges'
  result screen could show "NOT SAVED — storage unavailable" over a medal
  record that actually saved (or vice versa), because
  `OfflineChallengeController.CompleteChallenge` read the shared, overwritten
  `LocalChallengeSave.LastSaveSucceeded` flag a second time after an unrelated
  snapshot-clear save instead of keeping the record save's own outcome. Fixed
  by capturing each save's outcome separately. Match Search/queue failure
  handling (`OnlinePlay.cs`), the startup connection screen, and the Home
  identity strip were read against the same hierarchy/empty/loading/
  unavailable/error/offline/retry criteria and already satisfy them (covered
  by `PART1_MATCHMAKING_STATE_TRUTH.md`, `PART1_MAINTENANCE_NOTICE.md`, and
  the existing Home identity fix above); no change was made there. Runtime
  build: 0 errors, 41 existing warnings. See
  `docs/acceptance/PART5_OFFLINE_CHALLENGE_TYPE_SCALE.md`.
- 7 October follow-up: matchmaking now stops when party status cannot be read
  instead of silently presenting an empty party roster; only the backend's
  `searching` queue state receives search copy. Unknown states show a failure
  with Home / Practice actions. Runtime and Network builds passed; live queue
  acceptance remains open. See
  `docs/acceptance/PART1_MATCHMAKING_STATE_TRUTH.md`.
- 9 October follow-up: Offline Challenges now rebuilds its library layout when
  the screen metrics or measured content width changes, preserving the current
  scroll position so an initially measured narrow/wide canvas cannot keep stale
  card widths. Runtime build passed with 0 errors and 41 existing warnings;
  Unity resize captures and device verification remain open. See
  `docs/acceptance/PART5_OFFLINE_CHALLENGE_TYPE_SCALE.md`.
- 9 October follow-up: the in-match HUD's next-objective rotation countdown,
  current-zone label, and teammate distance-to-objective status now use the
  active profile's secondary-text role instead of muted structural text. This
  keeps live gameplay/team information legible across A/B/C without changing
  palette values or adding color-only cues. The Runtime build passed with 0
  errors and 41 existing warnings; no tests or Unity overlay/device contrast
  review were run. See `docs/acceptance/PART5_RADAR_TEAM_CUES.md` and
  `docs/acceptance/PART2_PROFILE_PALETTE.md`.
- 9 October follow-up: Match Search's live `YOUR SQUAD (N/3)` occupancy heading
  now uses the active profile's secondary-text role. Empty-seat placeholders
  and decorative venue metadata remain muted. Queue behavior is unchanged. The
  Runtime build passed with 0 errors and 41 existing warnings; no tests or
  in-game profile review were run. See
  `docs/acceptance/PART2_MATCH_SEARCH_PALETTE.md`.
- 9 October follow-up: Focus Aim now enforces its spec-authored 20–55 degree
  FOV range at runtime, bounded by the current base-camera FOV. The specified
  40-degree initial value and camera/control-only behavior are unchanged. The
  Runtime build passed with 0 errors and 41 existing warnings; no tests or
  device review were run. First-person ADS and reload-beat assets/timing remain
  blocked as recorded in `docs/acceptance/PART3_ADS_RELOAD_BEATS_BLOCKERS.md`.
- 9 October follow-up: the touch-layout editor now creates its dimming scrim
  from the active profile background token, preserving the existing opacity.
  This removes a Hybrid-only literal on first open under A/B. Runtime build
  passed with 0 errors and 41 existing warnings; profile-switch visual review
  remains open. See `docs/acceptance/PART2_NATIVE_PALETTE_ALIASES.md`.
- 9 October follow-up: readiness and browser account-deletion fallback 503
  Problem Details now carry stable machine codes while preserving status and
  existing English detail text. Backend build passed with 0 errors; the NuGet
  vulnerability feed warning remains. No outage simulation or tests were run.
  The browser fallback still needs localized web content; see
  `docs/acceptance/A7_LOCALIZATION_BACKEND_CODES.md`.

| Area | Current implementation | Remaining work |
|---|---|---|
| Part 0 / A0 identity | A, B, and C selectable UI plus procedural Switchyard world profiles; independent world concrete, structure, and safety colors match their profile tokens. Switching updates role-based arena and avatar team tints, profile-specific Poly Haven CC0 sky panorama, ambient light, sun, team-neutral West/Center/East zone-ring and roof-trim colors, landmark glazing/sign color, and profile grade without changing gameplay geometry or collision. Performance preserves sky, fog, and trilight ambient while disabling shadows/HDR/post; Balanced and Cinematic enable soft shadows, HDR, and the post stack; Cinematic enables high-quality bloom filtering. Atmosphere values and sky material identity mirror `02_ART_TOKENS.json`. Switchyard concrete and cladding clone registered Poly Haven CC0 URP materials with matched albedo, normal, and packed maps; four aligned roof/surface atlas variants share those maps and apply profile tint through material roles. Six individually sourced CC0 Poly Haven props dress spawn-room roofs and outer walls; before replacing placeholders, the runtime requires the complete active collider-free set and enabled mesh geometry, and checks each spawn again. Earlier project-original roof maps are retained outside Unity Assets. See `docs/acceptance/PART2_SWITCHYARD_WORLD_TOKENS.md`, `docs/acceptance/PART0_SWITCHYARD_ATMOSPHERE_TIERS.md`, `docs/acceptance/PART0_SWITCHYARD_PROJECT_ORIGINAL_TEXTURES.md`, and `docs/acceptance/PART0_SWITCHYARD_CC0_PROP_PROVENANCE.md`. | Run three-profile render captures at the required viewpoints and score against Gate G; run `DevicePerformanceProbe` for each tier. Confirm CC0 prop placement and materials, seam, scale, tint, and appearance in Unity; runtime props are not covered by the edit-mode geometry fixture. Production world mesh replacements, B/zone art and color-vision review remain open. AI-output uniqueness/legal clearance and archived unknown-texture cleanup remain open. Blood/gore remains gated by its separate review. |
| Part 1 / A5 lifecycle | Existing Home, queue/practice, match, result, and account flows remain. Bible text now identifies its ready-check/fly-through/team-intro phases as proposed rather than shipped. Current Relay respawns are documented from code/config: individual 4-second delay, team spawn exit, dead-state weapon selection for next spawn, and up to 1.5 seconds' protection ending on fire or leaving the spawn room. Relay local best counters are now separated into Offline/Online preference scopes (`docs/acceptance/PART1_PERSONAL_BEST_CONTEXT.md`); offline challenge records remain device-local and scoped by challenge version, controls, and difficulty. The backend publishes protocol and availability status; startup UI reads minimum-version and maintenance/queue-pause signals; Home polls availability every 30 seconds and labels states separately even for guests. Home now shows operator-supplied maintenance/queue-pause copy, UTC ETA, and optional compensation note in its status card (`docs/acceptance/PART1_MAINTENANCE_NOTICE.md`). Queue polling retries transient errors with capped exponential backoff; Practice While Waiting now retains the live queue ticket, runs local Relay practice with a Return to Queue / Cancel Search banner, and stops the local drill before online ticket handoff. The banner uses tokenized text, A/B/C colors, density-aware control floors, and is positioned clear of the score/zone stack (`docs/acceptance/PART1_QUEUE_ABANDONMENT_TELEMETRY.md`). Maintenance prevents new tickets and allocations while already matched tickets can join. A 426 response now carries a platform-specific configured HTTPS store link; startup shows an update action when present and leaves offline play available. Queue status shows the allocator-assigned region; Fusion RTT is measured only after joining. | Live backend acceptance is still needed for queue retention, cancel races, server allocation handoff, and failure recovery while the local drill is active. Release Android/iOS store URLs are not yet configured; optional resumable download requires authored downloadable groups, enabled remote catalog, non-empty remote build/load paths, and asset hosting (Addressables is a transitive dependency and its local settings are generated, but the remote-content pipeline is not configured); pre-match measured region selection/manual override needs additional regions; season rollover has no season service yet; age and ban flows need approved product/legal policy. The Level 5 guest-link reminder is implemented; the first-purchase trigger is dormant while purchases are gated. The allocator config and backend expose only one operational Fusion region (`eu`), so a working pre-match selector needs additional provisioned regions and regional allocation pools. The server reconnects a reserved account to the same avatar until match completion (or final AFK removal) and substitutes a labeled bot after the configured 5-second grace; each 90-second join JWT is refreshed and does not define the reconnect window. Mobile background and network behavior remain unverified. |
| Part 2 / A2 palette | UI token roles split primary action from ally and support three selectable profiles. Stylized Flight secondary text now matches the Bible's `#8A94A6`; its muted role derives from the profile secondary/background tokens. Queue-seat and party readiness, room status, seat guidance, and match-search fairness text now use the Bible's secondary-text role rather than muted text. Legacy hex-defined screens plus Match Search, Preparation, Appearance Locker, Contracts, Mastery, Offline Challenges, Support, Party Room, Results, Profile, and status/error surfaces map semantic colors to the active A/B/C palette; `UiDraw.Hex` resolves literal semantic roles from all profiles and live TMP rich-text tags remap on profile changes. Destructive actions use neutral surfaces with warning framing; enemy coral remains for enemy identity and damage feedback. Neutral blueprint/weapon/operative/challenge image tints and bronze/silver/gold award markers remain content-specific. Derived semantic tones (pressed action/enemy, action-objective mix, dimmed warning) are palette-owned and remap on a live switch; the Mastery permanence note wraps its two-line copy instead of overflowing its column. | Verify cross-profile contrast and color-vision distinction, especially destructive controls, offline/error states, and defeat presentation. Static muted-role contrast stays under 4.5:1 on the selected panel for all three profiles, so no colour was changed without art review. The owner-approved world-art spec authorizes production world-asset work; profile visual acceptance, Gate G art scoring, and device performance remain open. Source-role audit acceptance is recorded in `docs/acceptance/PART2_ENEMY_SIGNAL_ROLE.md`; profile palette acceptance is recorded in `docs/acceptance/PART2_PROFILE_PALETTE.md`. |
| Part 3 / A3 combat | Pad-launch weapon lockout is configurable and applied offline and online. ARC, ZIP, and BRICK use authored eight-shot recoil curves in config; offline and online local camera feedback read the same per-weapon data. Offline and authoritative fire classify grounded moving spread state using the shared 0.5 m/s MovementConfig threshold. Headshot and elimination cues have distinct marker shapes. The three imported CC0 weapon models and their base body/metal materials are selected on offline avatars and follow replicated `WeaponIndex` on online avatars; active wraps survive weapon switches (`docs/acceptance/PART3_NETWORK_WEAPON_VISUALS.md`). | Tune recoil in playtests. First-person viewmodel and ADS sight alignment plus first-person reload beats (mag-out, mag-in, bolt) remain incomplete: sight-picture details and beat timing are undecided, weapon FBXs are static meshes, and no per-beat audio assets exist. Focus Aim remains a camera/control feature, not a sight picture. The 40 m third-person silhouette needs visual review. Remaining: production reload animation/audio, blood levels and regional enforcement. See `docs/acceptance/PART3_ADS_RELOAD_BEATS_BLOCKERS.md`. |
| Part 4 map | Current Relay Switchyard and map config remain the baseline; West, Center, and East objective rings/beacons plus north/south roof-edge strips use profile-specific team-neutral tones and update when the presentation profile changes. Procedural collider-free landmark prototypes add a slow rotating crane, an arena-facing “SWITCHYARD” sign, and a translucent skybridge silhouette beyond the north boundary; these do not alter gameplay geometry, collision, or navigation. | Replace prototype landmark visuals with reviewed production art; verify color/contrast, landmark visibility, transparency, safe-area composition, reachability, and spawn/rotation timing in map playtests; fall/out-of-bounds punishment remains an undecided gameplay rule. |
| Part 5 / A8 UI | Ally chevron and enemy diamond cues are shape-distinct; core tokens now carry corrected type, radius, motion, and action/ally roles. Online, practice, and training body-hit, headshot, and elimination feedback use X, Material Symbols diamond, and star markers respectively. Every touch action hit region now retains the 48 dp floor even when a player customizes button scale; drawn controls keep their design size. Cockpit Mode fades roster/radar during nearby combat; teammate roster chips show each ally's configured pad-charge readiness/recharge state and fade after five seconds of inactivity (estimated, in-game tuning pending); fade range, opacity, duration, and idle threshold are in MatchRulesConfig. Radar now draws map bounds, spawn/building footprints, elevated platform cues, and validated team pings; ping lifetime/cooldown are in MatchRulesConfig. Bible 5.2's north-up minimap option is implemented with a persistent Accessibility preference while player-up remains the default (`docs/acceptance/PART5_NORTH_UP_RADAR.md`). Teammate pad readiness is recorded in `docs/acceptance/PART5_HUD_TEAM_PAD_STATUS.md`. Part 5.1 copy pass: `//` separators and lore jargon removed from player-facing strings across Contracts, Mastery, Profile ("Your pilot record", "Pilot name"), Appearance ("Cosmetic only. Zero gameplay effect."), Shop (red guard banner → neutral "Preview" strip, "Own it forever. No duplicates."), Results, Search, Preparation, Support, offline challenges and pause settings; the HUD armor label now reads "Suit Integrity" (3.4). Results now count up authoritative XP/credits, animate the per-level XP meter from persisted pre/post match bounds, and play one skippable ring burst without delaying the sticky Play Again action. The Appearance locker now distinguishes loading, unavailable, and empty inventory; with no owned wraps it shows the next server-configured public-match level unlock instead of a dead-end count. Shared MenuLayout and the settings console now use 200 ms ease-out parallax entry/exit; Reduce Motion selects opacity-only transitions. Product, Home/startup, runner-choice, online-status, settings, offline-challenge, Preparation, Results, and Match Search entry/exit motion are implemented; reusable roots restore correctly when reopened during an exit. | Profile hierarchy now presents the level and two career summary stats first; remaining stats, credits, offline accomplishments, loadout, and record expand on demand. A `profile_details_toggled` event records only expanded state. Toggle settings use 56 px rows with sentence-case names, one-line descriptions, right-aligned switches, and ellipsized overflow while preserving the 48 px switch hit area. Appearance technical claims now sit behind a 48 px disclosure; the cosmetic-only guarantee and equipped item remain visible. `appearance_tech_specs_toggled` records only expanded state. The UI-script typography migration is token-driven with no numeric font-size arguments, direct numeric TMP font-size assignments, or inline size tags found in the audit. The documented hierarchy and truthful empty/loading/error/offline states are implemented; inspect hierarchy, disclosure, icon rendering, and contrast in Unity at 16:9 and 20:9. Contracts/Mastery and Appearance, Results, and Training type-scale changes are recorded in `docs/acceptance/PART5_*_TYPE_SCALE.md`. A contract-specific wrap card still awaits an enabled contract reward catalog (`ContractsMasteryEnabled` is false and `rewardCatalogComplete` is false); do not promise contract rewards until backend configuration provides it. Unity inspection of hierarchy, transitions, settings rows, appearance disclosure, icon rendering, and contrast remains unrun at 16:9/20:9. Match Search refreshes its timer, region, party labels, and actions in place without restarting entry motion. See `docs/acceptance/PART5_MENU_SCREEN_MOTION.md`. |
| Part 6 audio | Master, Effects, Music, and Voice levels are independently adjustable; the previous Announcer preference migrates to Voice. Current Voice-category match direction, zone and result callouts are synthesized tones; spoken voice assets are not present. ARC, ZIP, and BRICK reports now layer each weapon's generated body/transient clip with a distinct synthetic mechanism transient and environment tail on one spatial source; spatial shots use the Bible's under-15 m full mix, 15-to-under-40 m filtered body, and 40-to-50 m distant report while retaining linear rolloff and occlusion. In-match captions announce nearby pad launch, arm, charging, deployment and destroy cues, objective-zone activation/contest/score milestones, rate-limited distant ARC/ZIP/BRICK fire with clear line of sight, flight entry, damage taken, low health, confirmed hits, confirmed eliminations, and the local own-death cue. Relay countdown numbers, GO, and authoritative victory/draw/defeat outcomes also publish the existing visible localized labels in both offline and online matches. Caption events carry their audio priority; the HUD keeps a higher-priority caption visible when lower-priority cues arrive. Those player-critical audio cues occupy the existing priority-20 threat tier, above launch/landing (40), objective/announcer (80), and UI (160). New local non-spatial wind and cloth loops smoothly follow offline and networked local planar/vertical velocity, increase level with speed, raise pitch during dives, and honor Effects volume. Unity's project audio configuration now caps real voices at 24, matching the bible budget and runtime one-shot pool. Mono output is selectable through Unity's `AudioSettings.Reset` configuration; haptic intensity is adjustable and routed to Android amplitude control and iOS intensity feedback with an older-iOS vibration fallback. | Replace generated weapon layers and synthetic callout tones with reviewed production assets; add contextual quick-chat auto-lines and reviewed regional voice packs if approved; complete runtime critical-caption coverage acceptance, distance/mix tuning, and reviewed production versions of adaptive stems. Pulse/rhythm/lead layering now follows shared offline and online match phases, with the estimated timing and +4 BPM endgame change config-backed (`docs/acceptance/PART6_ADAPTIVE_MUSIC.md`). Mono, haptic strength, loop mix, adaptive-music masking, and device behavior need physical-device acceptance. Voice stealing/mix behavior still needs runtime profiling at the 24-real-voice cap. Audio caption acceptance is recorded in `docs/acceptance/PART6_CRITICAL_CUE_CAPTIONS.md`; voice budget change is recorded in `docs/acceptance/PART6_AUDIO_VOICE_BUDGET.md`. |
| Part 7 accessibility/performance | Three frame/render-scale presets; Performance applies configured Bloom, Motion Blur, and Depth of Field overrides, one-step global texture mip reduction, reduced LOD bias, a lower maximum LOD level, Gate G per-tier shadow policy (Performance off; Balanced/Cinematic on), and per-effect particle caps plus shorter/smaller procedural particles; the Development Build probe reports peak draw calls/triangles against the config targets (`docs/acceptance/PART7_PERFORMANCE_BUDGET_PROBE.md`). Persistent Reduce Motion removes weapon camera kick and active shake while retaining manual look input; larger text applies to active labels and screens opened afterward via shared UI, HUD, control-caption, and concept-shop text factories; aim-assist Settings disclose the exact enabled slowdown (0.70x, 3-degree cone, 25 m, clear line of sight, no snap/tracking) and OFF state; look and Focus Aim sensitivity each offer ten discrete saved positions from 0.50x to 2.50x with a selectable 1.00x Standard value; camera shake, ping mute, gameplay captions, cockpit, handedness, vibration, mono output, and adjustable Android/iOS haptics are exposed. Design dp now converts to canvas units using current canvas scale and display density; shared text, selectable floors, and generated UI reapply the 12 dp / 48 dp requirements on the active device scale and nested transform scale. Runtime C# compiles in default, Android, and iOS conditional builds. Home header, `MenuLayout` top bar, Pause console footer, Party header/join pill/footer, Challenges footer, Preparation footer (and its touch-cluster reservation) and Results action bar grow to keep floored controls in their bars; Home and Party re-fit reused roots on density changes, Pause refreshes its bars while open, and Challenge and Preparation reflow their footer/viewport while open; Results also rebuilds its measured-width page on width changes and restores scroll/receipt state. Settings tabs use a horizontally scrollable strip. Runtime and PlayMode test-assembly compilation succeeded with 0 errors and 41 existing runtime warnings after these source changes; no tests were run. | Run the A06 physical-size acceptance after the latest Party/settings/Focus Aim changes and inspect 16:9/20:9 layouts on devices; no visual capture review or phone check was done for this pass. Full content-width reflow after rotation remains to be reviewed. Verify frame/memory and audio/haptic behavior on devices; native iOS plugin build remains unverified. Cinematic SSAO is wired in the shared renderer and controlled by the atmosphere profile; configured shader variant retention has been repaired, but player-build and render acceptance remain open (`docs/acceptance/PART0_SWITCHYARD_ATMOSPHERE_TIERS.md`). Baked-lighting authoring and colorblind visual review remain. A true one-handed layout needs an owner-defined interaction model because current input requires simultaneous movement, aim, and fire across separate zones; detail is recorded in `PART7_ONE_HANDED_CONTROL_DECISION.md`. An aim-assist strength range remains undefined and unimplemented. |
| Part 8 / A4 tech | Fusion and dedicated-server baseline; server owns damage, match facts, and backend grants. Fusion project config currently sets 150 ms hitbox history, below the proposed 200 ms cap. Backend enforces minimum client protocol, rejects missing/malformed player protocol headers, and exposes client availability; maintenance pauses queue allocation server-side. Online matches now sample measured Fusion RTT every two seconds and report bounded latency-bucket counts with completion telemetry. Movement intent rejects non-finite axes before the authoritative movement simulator and clamps finite input to unit magnitude. Shared server fire cadence rejects non-finite or non-positive simulation and weapon intervals. Player/pad damage and related elimination/pad-fact mutations guard state authority, runner ownership, and finite positive damage; server pad-event facts are exposed read-only, and respawn/substitution/weapon-time updates guard authority. The 60-per-player match placement safeguard is explicit in PadConfig, separate from the simultaneous arena pad cap. Client camera aim-ray distance and server submitted-point clamp now share MatchRulesConfig values. A server-only replay foundation samples authoritative position, velocity, alive state, weapon, shot/hit/headshot counters, and sanitized inputs at 10 Hz, bounded by configured match duration plus maximum overtime (`docs/acceptance/PART8_SERVER_REPLAY_CAPTURE.md`). Performance reduces texture mip and mesh detail and applies config-driven particle caps/lifetime/size reductions as low-tier measures. | The 1.5 GB target remains unverified; measure on representative 3–4 GB phones and tune from measured frame/memory data. Analyze accumulated RTT distributions before changing the rewind window; network spike/release checks, integrity/emulator controls, and receipt-validation acceptance remain. Replay persistence, reviewer access, kill-cam/highlight playback, and anti-cheat tooling need replay data ownership, access, and retention rules before extension. Damage authority guards and acceptance are recorded in `docs/acceptance/PART8_DAMAGE_STATE_AUTHORITY.md`; movement-input sanitization acceptance is recorded in `docs/acceptance/PART8_MOVEMENT_INPUT_SANITIZATION.md`; progression configuration fails fast on invalid thresholds, negative rewards, and malformed level unlocks (`docs/acceptance/PART8_PROGRESSION_CONFIG.md`); concurrent duplicate match-result conflicts return success only after all participant receipts are confirmed durable (`docs/acceptance/PART8_MATCH_RESULT_IDEMPOTENCY.md`); the per-player pad placement safeguard is separately configured (`docs/acceptance/PART8_PAD_PLACEMENT_CAP.md`); the shared camera aim-ray limits are config-backed (`docs/acceptance/PART8_AIM_RAY_CONFIG.md`); offline/network grounded weapon spread-state classification reads one threshold (`docs/acceptance/PART8_SHARED_WEAPON_MOVEMENT_STATE.md`). Server-owned pad placement, use, and destruction facts now retain random match-local pad IDs, map name, coordinates, and match time through result outbox delivery; offline practice records the same spatial fields locally. `tools/analytics/pad_heatmap.py` pairs offline study facts into configurable map/cohort bins using checked-in map geometry. Trusted online cohort labels now flow from the dedicated-server process to pad fact rows; deployment routing and balancing, contact-event semantics, and durable online rollups remain specified gaps (`docs/acceptance/PART8_PAD_HEATMAP_FACTS.md`, `docs/acceptance/PART8_TRUSTED_PAD_STUDY_COHORT.md`). Automated regression and concurrent database coverage for these items was not added or run. |
| Part 9 no distractions | Cockpit Mode fades radar and roster near combat and through the configured final regulation minute and overtime; combat range, faded opacity, fade duration, and roster idle threshold are config-backed estimates (`docs/acceptance/PART9_ENDGAME_HUD_TRIM.md`). Persistent Stream-Safe Display masks account aliases, progression/currency, participant names, party invite codes, account deletion email, and Support guest/linked/recovery state across Home, account/profile/support, party/search, preparation/HUD/results and report UI; account email, Support status, and the visible party code/member aliases respond to live preference changes while their surfaces are open. Network/account identity and backend records remain unchanged. Current party flow is invite-code entry only: there is no incoming social-invite channel or mid-match social popup to defer. No app-wide notification feed/badge system currently exists, so the two-badge cap and expiring-only notification rule have no consumer to apply to. Results use one 1.15 s skippable reward reveal; Play Again remains available throughout. | If incoming social invitations are added, queue them silently through results. If a notification feed is added, permit badges only for items expiring within 24 hours and cap visible badges at two app-wide. Verify account-email, Support status, party aliases/code, and all capture surfaces during live Stream-Safe toggles in game; validate reward motion in game. |
| Part 10 monetization | Store and ads are gated in current build; the concept shop's localized trust row states “ZERO RANDOMNESS” alongside cosmetic-only, no-timer, and no-power-modifier guarantees. The Foundry concept describes its pad item as FX, not a gameplay pad type. Server commerce code exists. Purchase tokens have a unique database index and concurrent duplicate pending submissions recover the existing transaction idempotently; verify-purchase rejects blank fields and product IDs outside the three-item catalog and requires a persisted linked Google identity; no entitlement is granted by the scaffold. | Owner business confirmation on no ads and legal review of billing/regions. Google Play Developer API verification, acknowledgement, refunds/revocation, and release acceptance remain open; do not enable purchase behavior on this pass. |
| Part 11 delivery gates | Implementation order is represented by the specs and this tracker. | Team/budget plan, playtest gates, six-client network run, phone journey, signed build, and soft-launch baselines. |
| Part 12 / A6 agent inputs | Root `AGENTS.md` plus 00–06 focused design specs added. | Acceptance and telemetry are defined before profile, settings-row, and appearance-disclosure work. A/B/C palette acceptance records are defined for Match Search, Preparation, Appearance Locker, Contracts/Mastery, Offline Challenges, Support, Party Room, Results, Profile, and native palette aliases in `docs/acceptance/PART2_*.md`; density-aware physical UI sizing is recorded in `docs/acceptance/PART7_PHYSICAL_UI_SCALE.md`. Repeat this gate for future feature work. |
| A7 localization | Unity Localization package, `LocalizationSettings`, English `Locale`, and the `UIStrings` String Table Collection are present under `ProjectHotdrop/Assets/Localization`; `UiDraw.L(key, fallback)` is the shared lookup. Profile, Appearance, Match Search (`docs/acceptance/A7_LOCALIZATION_MATCH_SEARCH.md`), online queue/reconnect (`docs/acceptance/A7_LOCALIZATION_ONLINE_JOURNEY.md`), Party Room (`docs/acceptance/A7_LOCALIZATION_PARTY_ROOM.md`), Results (`docs/acceptance/A7_LOCALIZATION_RESULTS.md`), Preparation (`docs/acceptance/A7_LOCALIZATION_PREPARATION.md`), Startup (`docs/acceptance/A7_LOCALIZATION_STARTUP.md`), guided training (`docs/acceptance/A7_LOCALIZATION_TUTORIAL.md`), gated cosmetic shop preview (`docs/acceptance/A7_LOCALIZATION_SHOP_PREVIEW.md`), product-screen controller states (`docs/acceptance/A7_LOCALIZATION_PRODUCT_SCREENS.md`), recognized backend error codes (`docs/acceptance/A7_LOCALIZATION_BACKEND_CODES.md`), Offline Challenges (`docs/acceptance/A7_LOCALIZATION_OFFLINE_RUNTIME.md`), authored Support (`docs/acceptance/A7_LOCALIZATION_SUPPORT.md`), Contracts/Mastery (`docs/acceptance/A7_LOCALIZATION_CONTRACTS_MASTERY.md`), authored Home/next-goal/facilitator copy (`docs/acceptance/A7_LOCALIZATION_HOME.md`), settings (`docs/acceptance/A7_LOCALIZATION_SETTINGS.md`), and Match HUD/touch/ping/cue copy (`docs/acceptance/A7_LOCALIZATION_HUD.md`) and shared connection-state overlays (`docs/acceptance/A7_LOCALIZATION_CONNECTION_OVERLAYS.md`) are migrated. `SetupLocalizationPilot` emits 1,470 unique English keys; all are present in the shared table. The shared and English assets each contain 1,470 aligned unique IDs with no orphaned serialized rows; the static source/key/value comparison confirms all current values match after whitespace normalization (`docs/acceptance/A7_LOCALIZATION_TABLE_SEED.md`). | Urdu RTL shaping and Urdu/Devanagari fonts/fallbacks are still absent. RTLTMPro is not accepted for Urdu based on current upstream support evidence; Magicar is an unvalidated v0.1 candidate, and Devanagari still needs complex-script shaping. A stable shaper and TMP integration are prerequisites before script assets/locales. Unity Editor asset import/re-save and live locale switching, translated wrapping, and device acceptance remain open. See `docs/acceptance/A7_LOCALIZATION_FONT_PIPELINE.md` and `docs/acceptance/A7_LOCALIZATION_TABLE_SEED.md`. |

Localization scope check: `Results.Problem` is used only by the operational
`/health/ready` endpoint and the external browser fallback for the account-
deletion page; neither response is consumed by the Unity API client. Remaining
unmapped JSON error codes are analytics or server-internal failures, not player
UI messages. Operator-authored maintenance copy still has no locale-specific
contract. See `docs/acceptance/A7_LOCALIZATION_BACKEND_CODES.md`.

### Contracts/Mastery progress-fact status (C04)

The dedicated server now emits the bounded, server-owned occupation, team-pad-use, and majority-weapon facts; the backend binds results to the allocated server and exact ticket roster, then applies facts once for fresh eligible receipts. The full-match occupation cap is configurable and currently 210 seconds (180 regulation + 30 maximum overtime). The feature remains disabled because `rewardCatalogComplete` is false and the configured reward sets are empty. Staging/result-pipeline, live UI, and device acceptance remain open; see `docs/acceptance/PART8_CONTRACTS_MASTERY_FACTS.md`.

### 10 October follow-up: profile-specific Home rooftop art

Added three project-original, AI-assisted rooftop illustrations and routed all runtime
UI uses through `UiPresentationProfile.LoadRooftopArtwork()`. A/B/C selection updates
already-open rooftop images, and the previous user-supplied reference stays recoverable
but is no longer selected when profile art is available. Importer policy sets sRGB,
mipmaps, clamp, no NPOT scaling, and Android ASTC 6x6 at 1024. Applied and unmet rules,
output provenance, and acceptance limits are recorded in
`docs/acceptance/PART0_HOME_ROOFTOP_PROFILE_ART.md` and
`ProjectHotdrop/ASSET_LICENSE_REGISTER.md`. Similarity/legal clearance, Unity import and
16:9/20:9 captures, Gate G scoring, and device performance remain open; no tests or
device/network/platform verification were run for this slice.

### 10 October follow-up: distinct UI feedback cues

Added generated soft-rising positive and low-flat negative cues without changing
existing `GameCue` indexes. Discrete settings save confirmations use the
positive cue (slider drags stay silent; a reset with nothing customized plays
only its button tap and shows SAVED); pad placement rejection and
rate-limited ping rejection use the negative cue. The latest Unity Editor C#
build, which includes the slider-cue suppression and no-op reset correction,
passed with 0 errors and 41 existing warnings; no tests were added or run, and
no audio playback, Unity runtime, network, or device verification was done.
Acceptance and remaining listening/runtime limits are recorded in
`docs/acceptance/PART6_UI_FEEDBACK_TICKS.md`.

### 10 October follow-up: gentle own-death cue

Following Bible §6.4, the local player's own death now plays a separate
generated `OwnDeath` cue. It is a short, low, soft drone with no noise,
appended to `GameCue` without changing existing indexes. It is SFX/Effects
routed with the same priority as `Elimination` (80) and has no caption. Online, only the
input-authority avatar hears it, non-spatially. Remote avatars keep the
spatial `Elimination` cue. Offline, `PlayerController.Die` plays it for the
local human instead of the spatial death clip, and bots keep that clip. Death
mechanics, kill-confirmation cues, and captions are unchanged. No audio playback,
Unity runtime, network, or device verification was done for this slice. The
Editor compile result is recorded below. Acceptance and listening limits are recorded in
`docs/acceptance/PART6_OWN_DEATH_CUE.md`.

Review tuning: at 0.55 s the drone was longer than the 0.45 s `Elimination`
cue, which does not read as "short" for a frequent event. `OwnDeath` is now
0.38 s. The 110→98 Hz glide, 25 ms attack (`attack = 40`), decay exponent 0.8,
gain 0.26, and zero noise are unchanged. No enum values, priorities, routing,
or config settings changed. Review confirmed by source inspection that each
own death plays once: offline through `PlayerController.Die`, online through
`NetPlayer`'s edge-triggered dead transition. Remote deaths still play the
spatial `Elimination` cue. The Editor compile passed with 0 errors and 41
existing warnings; no tests, runtime, or listening review were run.

### 10 October follow-up: objective zone shape cues

West, Center, and East now use one, two, and three small radial ticks on their
existing objective rings, supplementing the Bible's team-neutral zone hues with
a color-independent identity cue. The ticks derive placement from the existing
ring radius and add no colliders or gameplay geometry. The Editor build passed
with 0 errors and 41 existing warnings; no tests, runtime, or device visual
review were performed. Applied rules and remaining visual acceptance limits
are recorded in `docs/acceptance/PART2_OBJECTIVE_ZONE_SHAPE_CUES.md`.

### 10 October follow-up: Switchyard landmarks

The runtime CC0-prop branch no longer skips the Bible-required SWITCHYARD sign
and glass skybridge; the crane, sign, and bridge are generated together whether
the approved prop set is available or not. Sign lettering enables the bundled
TMP SDF glow using the active profile's window token. Skybridge supports now
meet the deck underside. Landmark parts remain collider-free and outside the
playable footprint. See `docs/acceptance/PART4_SWITCHYARD_LANDMARKS.md`. Editor
build passed with 0 errors and 41 existing warnings. No tests, Unity runtime
captures, or device visual review were performed.

### 11 October follow-up: project-original Switchyard landmarks

The crane's solid jib block is now a two-sided truss, and the skybridge's
center glass wall is now segmented glazing on both long sides with mullions and
top rails. The static geometry is combined by material for each assembly and
keeps shadow casting off; primitive source parts remain collider-free through
`PresentationPart`. No imported assets, gameplay bounds, objective/spawn
geometry or balance values changed. The crane mast moved west so its full
rotating jib stays beyond the map edge. A temporary Runtime sidecar build that
included the existing `FlightSpeedLinesView.cs` compiled with 0 errors and 41
existing warnings. The generated Runtime project by itself still fails because
it omits that source file. No tests or Unity/device visual review were run;
transparent sorting, profile appearance, player-view landmark readability and
Gate G acceptance remain open. See
`docs/acceptance/PART4_SWITCHYARD_LANDMARKS.md`.

### 10 October follow-up: Switchyard roof-plate count

The configured map now has six raised roof plates on each side of Z=0, plus
each team's spawn-roof position. New flank decks keep the original platform
indices stable, preserve the mirror layout, and use the existing two inward
ramps per platform. The MapConfig resource and creation defaults match; the
existing exact config-value assertion was updated. Editor build passed with 0
errors and 41 existing warnings. No tests or Unity map playtest were run. The
Bible's pad-plus-glide access remains unmet: source has a pad ballistic arc but
no distinct glide mechanic, and only four pad approach lanes across three
raised roofs received hand estimates. Spawn-roof scope is also unspecified.
The high-risk-roof versus safe-Relay-touch decision and route timing remain
open pending playtest. See
`docs/acceptance/PART4_SWITCHYARD_ROOF_PLATES.md`.

Geometry correction: the four inner flank decks moved from Z=±8 to Z=±12 (X=±34) because their opposing Z ramps overlapped; ground ends are now 2 m apart. The generated ramp count is 24 (12 platforms × 2), and the `OfflineSliceSmokeTest` expectation was updated from 8 to 24. No tests, builds, or Unity playtests were run for this correction; earlier limitations remain, and new deck/ramp footprint clearances against the outer flank and (±20, 14) decks are unverified.

Headroom correction: a 3D source audit found the inner X ramp under the (±20, 14) decks and the outer Z ramp under the inner deck. Inner flank decks are now at (±36, 3, ±12) and outer flank decks at (±34, 3, ±26). Inner X-ramp overlap with the (±20, 14) decks is x 24…25 (max surface 0.43 m, 2.07 m headroom against the 2.5 m deck underside); outer Z-ramp overlap with the inner deck is z 15…16 (same figures). Extents stay inside ±48 × ±40; the 24-ramp expectation is unchanged. Source arithmetic only: Unity playtest, nav-mesh, pad routes, defined glide behavior and route timing remain unverified, and no tests or builds were run.

Fail-closed roof validation: `NetConfigs.Map` now rejects Switchyard configurations that violate the Bible's 6–10 raised-roof count per Z half, platform height, bounds, non-overlap, or X/Z mirror rules. Ramp bounds reproduce the generated oriented slab, including its 0.15 m thickness; the existing 7 m run and 4 m width are shared `MapConfig` values across code defaults, serialized asset and editor defaults. `SwitchyardGenerator` rejects a missing or differently named map config before generating geometry, while the reusable validator skips non-Switchyard configs. Editor compilation passed with 0 errors and 41 existing warnings; Network and Tests project compilation passed with 0 errors and 0 warnings. No tests were run. Unity asset import/runtime validation, nav-mesh coverage, actual headroom, spawn clearance, pad routes, the undefined glide requirement, route timing, and device acceptance remain open. See `docs/acceptance/PART8_MAP_CONFIG_VALIDATION.md`.

Spawn-geometry bounds validation: `MapConfig.ValidateSpawnGeometry` (not Switchyard-specific, no new tunables) rejects non-finite or unordered bounds, invalid `spawnRoomSize`/`exitsPerSpawn`, non-finite spawn centres, a north or south room footprint outside the map bounds, and a first- or last-exit `SpawnExit` position that is non-finite, outside its own room, or outside the bounds. `NetConfigs.LoadMapConfig` runs it with no wall allowance for every map name; `SwitchyardGenerator.GenerateMap` runs it with the 0.5 m wall thickness after the name guard and before the roof validator and before any output is cleared or built. The roof validator is unchanged and still skips non-Switchyard names. No values or layout changed. Editor compilation passed with 0 errors and 41 existing warnings; Network compilation passed with 0 errors and 0 warnings. No tests were added or run, and no Unity, server or device verification was done. Open: the generator's hard-coded three-exit room is not tied to `exitsPerSpawn`, the 0.5 wall thickness is duplicated, and spawn clearance against decks, blockers and breakers is unchecked. See `docs/acceptance/PART8_MAP_CONFIG_VALIDATION.md`.

Switchyard spawn-layout alignment: `MapConfig.ValidateSwitchyardSpawnLayout` (Switchyard only; the generic spawn-bounds validator is unchanged) now requires `exitsPerSpawn` = 3, spawn-centre Y = 0, and the three `SpawnExit` lateral offsets to equal the generated opening centres -6/0/+6 m for both teams (which also needs `spawnRoomSize.x` ≥ 18). `NetConfigs.LoadMapConfig` and `SwitchyardGenerator.GenerateMap` both run it before the roof validator, the latter before anything is cleared or built. `CreateSpawnRoom` now reads shared constants for the exit count, the 6 m pitch and the 0.5 m wall/header depth instead of repeating literals; geometry and current values are unchanged. Editor compilation passed with 0 errors and 41 existing warnings; Network compilation passed with 0 errors and 0 warnings. No tests were added or run and nothing was verified in Unity, on a server or on a device. Still open: the 4 m opening width, 0.12 m barrier depth, stripe depth and roof thickness literals, spawn clearance against decks, blockers and breakers, and all runtime acceptance. This supersedes the earlier spawn-validation entry's note about the hard-coded three-exit room and duplicated wall thickness. See `docs/acceptance/PART8_MAP_CONFIG_VALIDATION.md`.

Spawn points strictly inside the room: `MapConfig.ValidateSpawnGeometry` now rejects a first- or last-exit `SpawnExit` point that is on or beyond the room boundary (offset must be at most half the room size minus the existing 0.001 m tolerance on X and Z), for generic and Switchyard configs, so a room 2 m deep or shallower can no longer put a spawn on the back wall. No new margin was added and current 20 x 8 m rooms and spawn positions are unchanged. Editor compilation passed with 0 errors and 41 existing warnings; Network compilation passed with 0 errors and 0 warnings. No tests were added or run and nothing was verified in Unity, on a server or on a device. See `docs/acceptance/PART8_MAP_CONFIG_VALIDATION.md`.

Objective-zone, visual-marker, and lane-breaker footprint validation: `MapConfig.ValidateObjectiveZones` requires finite, ordered bounds, a positive finite `zoneRadius`, finite `centerZone`, `westZone` and `eastZone` values, and each map's trigger-radius box inside bounds. For Switchyard the checked footprint includes the 0.07 m line half-width and the 0.5 m center-marker radius; objective centre Y must equal `groundLevelY` within 0.001 m; all four generated outer-breaker AABBs per zone must fit inside bounds. Dimensions/count/offset are shared constants with the generator. Other map names keep the generic zone-footprint rule. `NetConfigs.LoadMapConfig` and `SwitchyardGenerator.GenerateMap` (before any output is cleared or built) both run it. Current zones span x −28…28 and z −6…6; their rings reach x ±28.07 and z ±6.07; outer breakers reach no farther than x ±32.5 and z ±10.5, so shipped geometry is unchanged. Editor compilation passed with 0 errors and 41 existing warnings; Network compilation passed with 0 errors and 0 warnings. No tests were added or run and nothing was verified in Unity, on a server or on a device. Not covered: breaker overlap with decks, ramps, spawn rooms, other breakers or zones, and objective tuning. See `docs/acceptance/PART8_MAP_CONFIG_VALIDATION.md`.

Generated ground matches the configured map rectangle: `SwitchyardGenerator.GenerateGround` now centres the ground cube at the midpoint of `boundsMin`/`boundsMax` (`MapConfig.BoundsCenter`) instead of the origin; the current symmetric ±48 × ±40 bounds give the same (0, 0) placement and size. New generic `MapConfig.ValidateBounds` (shared by the roof, spawn and objective validators and called explicitly by `NetConfigs.LoadMapConfig` and `SwitchyardGenerator.GenerateMap` before any output) also rejects finite endpoints whose width, depth or centre overflows. Editor compilation passed with 0 errors and 41 existing warnings; Network compilation passed with 0 errors and 0 warnings. No tests were added or run and nothing was verified in Unity, on a server or on a device. Not covered: presentation code that may assume an origin-centred map, and the pad, bot and HUD bounds consumers. See `docs/acceptance/PART8_MAP_CONFIG_VALIDATION.md`.

Asymmetric map bounds across consumers: the rooftop presentation (deck seams, parapet and cap spans, tower facade and window spans, distant-city start) now centres on `MapConfig.BoundsCenter`, and `OfflineNavigation` builds its nav region from the configured bounds plus a 2 m border with its vertical centre at `groundLevelY`. At the current ±48 × ±40 bounds every coordinate and the 100 × 20 × 84 region are unchanged; materials and colours were not touched. At this audit, sightline blockers remained at fixed X = 0; the 12 October follow-up below supersedes that condition by deriving their X from the configured spawn centres. The fixed distant-city, parapet-height and radar-scale literals remain. Editor compilation passed with 0 errors and 41 existing warnings; Network compilation passed with 0 errors and 0 warnings. No tests were added or run; no Unity nav-mesh, bot, presentation, collider or device verification was done. See `docs/acceptance/PART8_MAP_CONFIG_VALIDATION.md`.

Final map-config source compilation and audit (10 October): after the bounds, objective, spawn, ramp and presentation changes, incremental no-restore builds of `Hotdrop.Editor.csproj` and `Hotdrop.Network.csproj` both succeeded with 0 errors and 0 warnings printed. `Hotdrop.Tests.csproj` also compiled with 0 errors and 41 existing Runtime warnings; no test cases were executed. The final source audit found no behavioral defect and corrected a stale ramp-thickness comment. Unity asset import, EditMode/PlayMode execution, dedicated-server loading, nav-mesh baking, gameplay, collider, visual, playtest and device acceptance remain open. See `docs/acceptance/PART8_MAP_CONFIG_VALIDATION.md`.

Ballistics audit: offline fire in `PlayerController.ProcessPellet` and authoritative fire in `NetMatch.ResolveShots` both use straight raycasts. Weapon assets have no trajectory parameters. The Bible's 25 m hitscan transition is an estimate, and `docs/design/04_COMBAT.md` requires balance review before changing live weapon settings; projectile speed/drop and curved-path lag-compensation behavior are undefined. Ballistics remains unimplemented pending a balance decision rather than adding guessed behavior. Source inspection only; no code, assets, tests, or builds changed. See `docs/acceptance/PART3_BALLISTICS.md`.

Impact feedback: added a local surface response using authored collision layers: Ground emits short concrete chips; Cover, SpawnBarrier, and Pad emit sparks. Offline impacts and the owning client's predicted online impacts also leave non-colliding procedural surface marks (12-second lifetime, 32-mark cap). Added a collider-free visor crack/chest scuff that persists for the player's life after any damage and clears on respawn; online clients read the replicated per-life damage counter. `NetMatch.ResolveShots` damage authority and hitboxes are unchanged. Runtime sidecar compilation passed with 0 errors and 41 existing warnings; Network sidecar compilation passed with 0 errors and 0 warnings. No tests were run. The player hit puff remains rating-gated; whether predicted world-impact effects replicate to observers is unspecified and remains for runtime review. Unity appearance, network reconciliation, and device performance remain unverified. See `docs/acceptance/PART3_IMPACT_FEEDBACK.md`.

### 11 October 2026 follow-up: faint city-rush flight layer

`GameAudio` now adds a faint generated city-rush bed beneath the existing local wind and cloth loops while airborne. It follows the shared planar-speed envelope, gains slightly during a dive, fades while grounded, and honors Effects volume; movement and pad authority are unchanged. Bible §6.2 and `05_AUDIO.md` do not define altitude-linked mix behavior, so that response remains a design choice rather than an implementation gap. Runtime sidecar compilation passed with 0 errors and 41 existing warnings. No tests, Unity listening, network session, or device checks were run. Glide control/physics and production audio review remain open. See `docs/acceptance/PART6_FLIGHT_CITY_RUSH.md`.

### 11 October 2026 follow-up: Switchyard roof-trim shape cues

North/south West, Center, and East trim bands now carry one/two/three collider-free dark ticks centered on each configured objective X. `ZoneVisual.IdentityTickCount` is shared by the ground-ring and roof-trim cues, preserving consistent non-color identity across all A/B/C profiles without changing geometry or collision. Runtime sidecar compilation passed with 0 errors and 41 existing warnings. No tests or Unity visual/CVD review were run; mark visibility and profile readability remain open. See `docs/acceptance/PART2_SWITCHYARD_WORLD_TOKENS.md`.

### 11 October 2026 follow-up: spawn-room presentation dimensions

The existing 0.12 m exit-barrier depth, 0.5 m collider-free team-stripe depth,
and 2 m SpawnExit setback are named `MapConfig` constants. One shared
front-wall offset now drives the room generator and `SpawnExit`; map validation
checks the existing setback. The 4 m opening and 0.5 m roof thickness were
already shared. All dimensions and positions are unchanged. Runtime sidecar
compilation passed with 0 errors and 41 existing warnings. No tests or
Unity/runtime validation were run. No separate apron or barrier-clearance rule
is specified by the design. See
`docs/acceptance/PART8_MAP_CONFIG_VALIDATION.md`.

### 11 October 2026 follow-up: shared spawn-room box geometry

`MapConfig.TryGetSwitchyardSpawnRoomGeometry` now supplies floor, stripe,
wall, pier, barrier, header, and roof descriptors. Spawn-room generation and
clearance bounds consume those same descriptors. Barrier ownership/visibility,
stripe collider removal, and current layout values are preserved. Runtime
sidecar compilation passed with 0 errors and 41 existing warnings; Network
sidecar compilation passed with 0 errors and 121 warnings. Unity collider
hierarchy, barrier activation, NavMesh, traversal, server, and device
acceptance remain open. No tests were run. See
`docs/acceptance/PART8_MAP_CONFIG_VALIDATION.md`.

### 11 October 2026 follow-up: Support match-ID localization row

The English table had serialized `support.identity.matchIdTemplate` as an
unquoted YAML scalar, causing the `#` to start a comment and drop `{0}` during
YAML parsing. The row is now quoted; its shared key and ID remain unchanged.
Static YAML parsing confirms the complete template. This is not a full
source-value audit or Unity Editor import check. No tests were run. See
`docs/acceptance/A7_LOCALIZATION_TABLE_SEED.md`.

### 11 October 2026 follow-up: muted UI state and action labels

The muted-role source audit found actionable/current-state copy in startup
connectivity status, Tutorial's `SHOW ME`, runner-choice guidance and selectable
framing labels, Preparation's `UNLOCKED`, Mastery's `LOCKED`, Profile's `HIDDEN`,
and the Results receipt/save note. Those labels now use the active profile's
secondary-text role. Remaining muted uses are metadata, inactive rows,
placeholders, disabled controls, and decorative outlines. The Runtime C# sidecar
build passed with 0 errors and 41 existing warnings. No tests, Unity visual
review, or device checks were run. Cross-profile rendered contrast review is
still open. See `docs/acceptance/PART2_PROFILE_PALETTE.md`.

### 11 October 2026 follow-up: pad-aware muzzle obstruction

`MuzzleObstruction.GeometryMask` now includes Pad; the network blocked-shot preview reads that shared mask, so offline fire, server buried-muzzle validation, and client blocked-shot feedback check the same static blocker layers. Network shot raycasts continue to stop on wall and pad hits. Runtime sidecar compilation passed with 0 errors and 41 existing warnings; Network sidecar compilation passed with 0 errors and 0 warnings. No tests, Unity Physics, live network, or device checks were run. Embedded-muzzle and six-client cover/elevation acceptance remain open. See `docs/acceptance/PART3_MUZZLE_OBSTRUCTION_PARITY.md`.

### 11 October 2026 follow-up: concrete localization lookup audit

Touch controls no longer double-prefix HUD keys, Settings copy uses the direct
configured localization lookup, and the two external countdown captions resolve
the existing `hud.countdown.go` row. English fallbacks and copy are unchanged;
no keys or IDs were added. Static call-site/table matching found no missing
concrete direct `UiDraw` key or invalid literal HUD wrapper prefix. No tests were
run. Unity import, translated layout, visual, and device acceptance remain open.
See `docs/acceptance/A7_LOCALIZATION_HUD.md`.

### 11 October 2026 follow-up: character silhouette rim prototype

The existing CharacterRimLit URP shader adds a view-dependent Fresnel edge
light to both imported Operator and procedural character materials through
cached variants. Team cues, gameplay geometry, and hit detection are unchanged.
The rim uses provisional neutral-blue values because exact hue and intensity
are not specified. Runtime sidecar compilation passed with 41 existing
warnings; Network compilation passed without warnings. Unity shader import,
rendered A/B/C review, and Android performance remain open. No tests were run.
See `docs/acceptance/PART0_PILOT_RIM_LIGHT.md`.

### 11 October 2026 follow-up: radar-only ping kind cues

Radar/world pings retain their current shapes and visibility, while each kind
now plays a distinct generated cue and publishes its existing localized label
through the standard sound-caption path. Team-only sound, mute behavior, ping
cooldown/lifetime, and enemy reveal rules are unchanged. Runtime compilation
passed with 41 existing warnings and Network compilation with 0 warnings. No
tests or Unity/audio/multi-client checks were run; device intelligibility and
overlap remain open. See docs/acceptance/PART5_RADAR_TEAM_CUES.md.

### 11 October 2026 follow-up: shared Switchyard geometry helpers

Deck, ramp, and sightline-blocker transforms and bounds now come from shared
`MapConfig` helpers consumed by both `SwitchyardGenerator` and the applicable
fail-closed geometry checks. Existing layout values and colliders are
preserved. Runtime sidecar compilation passed with 0 errors and 41 existing
warnings; Network sidecar compilation passed with 0 errors and 121 warnings.
No tests were run. Unity collider hierarchy, NavMesh, traversal, dedicated-server,
multiplayer, and device acceptance remain open. See
`docs/acceptance/PART8_MAP_CONFIG_VALIDATION.md`.

### 11 October 2026 follow-up: transactional Switchyard prop atlas

Unity's `Build Switchyard Prop Atlas` command committed build
`5958db476eb8df25` with eight verified material mappings across five source
prefabs. The immutable candidate and its three shared atlas textures/materials
remain under `Assets/Generated/PropAtlas/Builds/5958db476eb8df25`; the stable
`Resources/PropAtlas/Active` directory contains only the five prefab handoffs
and manifest. The prefabs reference their versioned generated material/texture
dependencies. `Resources/PropAtlas/active.json` was switched after the staged
and published sets passed validation. Original source prefabs/materials remain
the fallback; opaque opacity-linked variants and the crate remain excluded.
The Editor source compiled with 0 errors and 125 existing warnings. No tests
were run. Runtime resource dependency resolution, alpha/visual parity, distant
mip behavior, Frame Debugger/Profiler results, player shader retention, and
representative-device performance remain unverified. This applies Bible §8.1's
prop-atlas requirement for the eligible set and preserves the presentation-only
rule in `docs/design/07_WORLD_ART.md`. See
`docs/acceptance/PART8_SWITCHYARD_PROP_INSTANCING.md`.

### 11 October 2026 follow-up: weapon portraits

Generated ARC, ZIP, and BRICK transparent portraits from the registered CC0
weapon prefabs into `Assets/Resources/UIUX/WeaponPortraits/`. The UIUX
postprocessor now applies portrait-specific no-mipmap, bilinear, clamp,
uncompressed, 1024 maximum-size imports; checked asset metadata matches. This
applies `04_COMBAT`'s matching weapon portrait rule, `00_PILLARS.md`'s approved
free-asset and no-manufacturer-mark rules, and the `07_WORLD_ART.md` asset
provenance gate. Source-alpha bounds and a synthetic 256×128-per-image charcoal
preview were inspected. Unity 6.3's Inspector confirmed all three generated
textures imported with alpha transparency, mipmaps off, Clamp/Bilinear,
1024 maximum size, and no compression after the portrait-specific importer
rule. A source review found a 2:1 generated portrait being stretched in 2.4:1
RawImage slots. The shared `UiDraw.FitRawImage` helper now fits generated and
legacy fallback textures to preparation-card and fire-control portrait slots
without changing semantic tints, and the fire control starts with the generated
ARC portrait. No tests were run. Unmet: Unity HUD/phone capture, grayscale and
all three presentation directions, runtime Resources loading, and device
verification remain open.
`Hotdrop.Editor.csproj` builds with 0 errors; the earlier full build reported
124 warnings and the latest build reported 0 warnings. The existing fallback
PNG provenance is unverified. See
`docs/acceptance/PART3_ADS_RELOAD_BEATS_BLOCKERS.md` and
`ProjectHotdrop/ASSET_LICENSE_REGISTER.md`.

### 11 October 2026 follow-up: repeating surface-atlas gutters

The Switchyard surface-atlas baker now fills each 64 px gutter by wrapping to
the opposite edge of the source tile, matching the source textures' Repeat
mode. The Unity surface-atlas command completed and updated the albedo, normal,
and packed-mask atlas PNGs. A source-resolution pixel comparison found zero
mismatches across 5,308,416 texels per map, including all four tile interiors
and gutters. The atlas importer retains mip generation and the existing color,
normal, linear-mask, and Android ASTC roles. This applies Bible §8.1's shared
surface-atlas requirement without changing UV repeat counts, geometry,
colliders, or gameplay. Mip-level bleed and distant rendered appearance,
Frame Debugger/Profiler behavior, player dependency/shader retention, and
representative-device checks remain open. No tests were run. See
`docs/acceptance/PART8_SWITCHYARD_PROP_INSTANCING.md`.

### 11 October 2026 follow-up: ground atlas UV phase

The atlas subdivider aligns fixed-repeat tile boundaries to the original
primitive face UV origin. This removes the half-tile shift on the odd five-repeat
axis of the ground and originally also applied to the spawn-room floors. The
follow-up below changes those floors to metric UVs, so fixed-repeat phase
alignment now applies to the ground. Geometry, colliders, and gameplay are
unchanged. `Hotdrop.Editor.csproj` built with 0 errors and 44 existing warnings.
No tests or Unity rendered comparison were run. This applies the
`07_WORLD_ART.md` presentation-only and no-stretched-textures rules while
preserving the ground's existing player-facing surface behavior. Distant mip
bleed and the rendered visual comparison remain open. See
`docs/acceptance/PART8_SWITCHYARD_PROP_INSTANCING.md`.

### 11 October 2026 follow-up: spawn-floor tile aspect

Switchyard spawn-room floors now use the existing three-metre concrete scale
used by their walls, keeping square tiles across the 20×8 m floor. The arena
ground retains its 6×5 repeat. A dedicated floor material preserves equivalent
metric tiling in atlas and source-material fallback paths. Editor generation
uses source materials with the matching primitive UV scales because atlas UV
remapping occurs at runtime. Missing wall-source fallback keeps rooftop
cladding on the wall-family repeat path. Geometry, colliders, transforms, and
gameplay are unchanged. This applies `07_WORLD_ART.md`'s no-stretched-textures
and presentation-only rules. No tests or Unity visual comparison were run;
displayed scale and distant mip behavior remain open. See
`docs/acceptance/PART8_SWITCHYARD_PROP_INSTANCING.md`.

### 11 October 2026 follow-up: bounded surface-atlas mip investigation

Unity 6000.3 supports explicit mip-count texture construction and per-mip pixel
writes, and its editor compression API accepts ASTC for NPOT mipmapped textures.
The compression call accepts a single format and does not expose the
`TextureImporter` per-platform overrides used by the current PNG atlas path.
No asset format or atlas files were changed. The wrapped 64 px gutters remain
active; isolation below mip 6 and distant rendered appearance remain open. A
platform-aware import/build route and Unity plus target-device rendering checks
are required before adopting custom mip data. No tests were run. See
`docs/acceptance/PART8_SWITCHYARD_PROP_INSTANCING.md` for the Unity API
references.

### 11 October 2026 follow-up: prop audit reconciliation

Current source uses explicit `Graphics.RenderMeshInstanced` submissions and
releases prop material clones with the final Switchyard cache lease. All 39
prop texture sidecars currently match their intended sRGB/NormalMap/linear
roles and override Android to ASTC 6×6 at 512 px. A recheck of the 45 local
prop-file SHA-256 entries found no missing files or mismatches. These checks
do not prove runtime draw formation, player shader retention, source retrieval,
or device performance. The six official model pages and Poly Haven's license
page were rechecked on 2026-10-11 and show/state CC0; original retrieval dates
and historical page snapshots remain unknown. See
`docs/acceptance/PART8_SWITCHYARD_PROP_INSTANCING.md`.

### 11 October 2026 follow-up: A/B/C tint routing for generated props

Generated rooftop props now receive the current profile's `WorldEquipment`
token, with `WorldWindow` for glass, at the existing 45% PBR tint blend.
Instanced props use uniform runtime material colors, while the ordinary renderer
path uses its property block. Prop glass keeps its source alpha multiplier and
render mode. This makes profile selection affect the authored Switchyard
dressing without changing geometry, colliders, or gameplay.
`Hotdrop.Editor.csproj` built with 0 errors and 44 existing warnings. No tests
or Unity visuals were run; A/B/C distinction, glass appearance, and device
readability remain open. See
`docs/acceptance/PART8_SWITCHYARD_PROP_INSTANCING.md`.

### 11 October 2026 follow-up: source-safe surface atlas bake

The atlas baker decodes each source image from file bytes and no longer
force-reimports those inputs or temporarily changes importer readability.
This removes unnecessary importer churn without changing source pixels,
atlas output, materials, or runtime behavior. **Spec applied:** Bible §8.1's
atlas requirement; `07_WORLD_ART.md`'s presentation-only constraint is
preserved. **Unmet:** the Unity menu action, mip isolation, rendered appearance,
and device behavior remain unverified. `Hotdrop.Editor.csproj` built with 0
warnings and 0 errors. No tests were added or run. See
`docs/acceptance/PART8_SWITCHYARD_PROP_INSTANCING.md`.

### 12 October 2026 follow-up: Results width reflow

Results now rebuilds its measured-width content when the live root width changes,
restoring the vertical scroll position and carrying forward the current trust,
receipt, save-note, and reward state. A reward reveal already in progress settles
to its final receipt during the width rebuild rather than restarting. At this
stage, density-only changes updated only the footer/viewport floor; that gap was
closed by the later Results scoreboard line-height and live-eligibility
follow-up below. This applies `PART7_PHYSICAL_UI_SCALE.md`'s responsive-width
requirement. The Runtime
project build passed with 0 errors and 44 existing warnings. No tests or Unity
captures were run. Narrow/wide layout, report-action touch targets, scroll
restoration, and reward state after rotation remain visual/device acceptance
items. See `docs/acceptance/PART7_PHYSICAL_UI_SCALE.md` and
`docs/acceptance/PART5_RESULTS_TYPE_SCALE.md`.

### 12 October 2026 follow-up: Results report target geometry and session lifetime

The compact Results threshold now evaluates the scoreboard width in dp instead
of canvas units. Report buttons are authored at 60 × 48 dp, their rows maintain
physical vertical separation, and compact metrics reserve the action's full
width. Reportable wide rows use the action in place of the redundant role value,
preventing overdraw. The label uses the existing Support localization key;
compact labels ellipsize, and disconnected status prefixes the name to survive
truncation. Report callbacks capture the match ID and fall back to the persisted
RecentMatch roster after session teardown, so resize rebuilds keep the same
eligible players. Runtime build passed with 44 existing warnings; Network and
Editor builds passed with 0 warnings/errors. No tests or Unity/device captures
were run. Touch separation, text fit, and report activation remain device/live
backend acceptance items. See `docs/acceptance/PART6_REPORT_FROM_SCOREBOARD.md`
and `docs/acceptance/PART5_RESULTS_TYPE_SCALE.md`.

### 12 October 2026 follow-up: compact Results header line height

Compact Results name, metric, team-heading, and points-chip boxes now derive
their height from the existing 17 dp and 12 dp typography tokens using the
larger-text scale and live canvas density. Team-dot alignment and compact
header spacing follow those heights; copy, type roles, and scoreboard data are unchanged. This applies
`PART5_RESULTS_TYPE_SCALE.md` and `PART7_PHYSICAL_UI_SCALE.md`. Runtime sidecar
build passed with 0 errors and 44 existing warnings. No tests or Unity/device
captures were run. Localized expansion, narrow/wide rendered balance, and
physical-device acceptance remain open; see
`docs/acceptance/PART5_RESULTS_TYPE_SCALE.md`.

### 12 October 2026 follow-up: Results contribution-card text scaling

Contribution and match-mode caption boxes and the three contribution-tile
lines now size from the existing caption/value typography through the larger
text scale and current canvas density. Tile heights, following callout
position, and match-mode row height grow with the measured line boxes. Match
facts, copy, color roles, and the 12/17/30 dp scale are unchanged. Runtime
sidecar build passed with 0 errors and 44 existing warnings. No tests were run.
Unity rendering, localization expansion, and device acceptance remain
unverified.
See `docs/acceptance/PART5_RESULTS_TYPE_SCALE.md`.

### 12 October 2026 follow-up: Results callout text scaling

Personal-best and next-goal callout title/tag lines and wrapped body now size
from current density and larger text. Callout height and following card
position use the actual body line height. Existing copy, facts, colors, and
typography roles are unchanged. Runtime sidecar build passed with 0 errors and
44 existing warnings; no tests were run. Unity localization expansion,
aspect/profile rendering, and device
acceptance remain open. See `docs/acceptance/PART5_RESULTS_TYPE_SCALE.md`.

### 12 October 2026 follow-up: progression receipt and save-note reflow

Results progression receipt and save-note text now wrap and size to the 12 dp
caption role at current density and larger-text settings. Receipt/note updates
reflow the meter, reward burst, note row, card, and page height together. The
server-receipt source, reward animation, and sticky actions are unchanged.
Runtime sidecar build passed with 0 errors and 44 existing warnings. No tests
were run. Runtime animation, locale expansion, and Unity/device acceptance
remain open. See `docs/acceptance/PART5_RESULTS_TYPE_SCALE.md`.

### 12 October 2026 follow-up: Results outcome-banner text scaling

The outcome banner eyebrow, 30 dp title, score pill, and player/weapon line
boxes now size from the current density and larger-text setting. Banner height
grows with the title and player card, and the page uses that height when placing
following Results sections. Match facts, copy, colors, and behavior are
unchanged. Runtime sidecar build passed with 0 errors and 44 existing warnings;
no tests were run. Unity localization, rendered profile/aspect, and device
acceptance remain open. See
`docs/acceptance/PART5_RESULTS_TYPE_SCALE.md`.

### 12 October 2026 follow-up: Results sticky-action label sizing

Home, Play Again, and Ready Status line boxes now use the existing 12/17 dp
text tokens through current density and larger-text settings. Ready Status
uses its measured width. Action hit targets and sticky-bar behavior are
unchanged. Runtime sidecar build passed with 0 errors and 44 existing warnings.
No tests were run. Localization expansion, narrow-layout spacing, and
Unity/device acceptance remain open. See
`docs/acceptance/PART5_RESULTS_TYPE_SCALE.md`.

### 12 October 2026 follow-up: Results trust-state header reflow

After the authoritative Results trust state changes its localized eyebrow,
the plate width and following mode/arena metadata positions are recomputed from
the new text width. Status semantics and match data are unchanged. Runtime
sidecar build passed with 0 errors and 44 existing warnings; no tests were run.
Expanded-locale and Unity visual acceptance remain open. See
`docs/acceptance/PART5_RESULTS_TYPE_SCALE.md`.

### 12 October 2026 follow-up: Results progression-heading reflow

The Progression heading's 12 dp line height now follows density and larger
text, and receipt positioning starts below that measured heading after state
changes. Runtime sidecar build passed with 0 errors and 44 existing warnings.
No tests were run. Localized line expansion and Unity/device acceptance remain
open. See `docs/acceptance/PART5_RESULTS_TYPE_SCALE.md`.

### 12 October 2026 follow-up: Results scoreboard line-height and live eligibility

The Results scoreboard now sizes ellipsis boxes to 1.32 em plus padding, the
largest line extent among its bundled fonts, and grows compact rows and team
headers around those lines. Wide player names and subtitles use a vertical
stack that fits the lines, including Practice results. The display watcher now
rebuilds after display-metric, viewport-width, or larger-text changes, restoring
scroll and the active trust/receipt/reward snapshot. Live report eligibility
shares `RecentMatch`'s `[BOT]` alias filter, avoiding a temporary report button
for aliases omitted from the saved roster. Runtime build passed with 0 errors
and 44 existing warnings; Network build passed with 0 warnings/errors. No tests
were run. Unity font rendering, rotations, live report submission, and device
acceptance remain unverified. The wide scoreboard column heading now reads
ROLE / REPORT for the eligible-row action and normal role labels. See
`docs/acceptance/PART6_REPORT_FROM_SCOREBOARD.md`.

### 12 October 2026 follow-up: Results role/report column heading

The wide roster's last column now uses the localized `ROLE / REPORT` heading,
which describes both the ally/rival role text and eligible-row report button.
Added `results.roster.roleOrReport` with stable shared/English ID
`-760000000000002055`. Static audit confirms 1,458 unique shared/English IDs,
identical ID sets, and one matching key/value row. Runtime build passed with 0
errors and 44 existing warnings; Editor build passed with 0 warnings/errors.
No tests were run. Unity table import, translated fit, and rendered device
review remain open. See
`docs/acceptance/PART6_REPORT_FROM_SCOREBOARD.md` and
`docs/acceptance/A7_LOCALIZATION_TABLE_SEED.md`.

### 12 October 2026 follow-up: Results rebuild and measured text fit

The end-of-match personal-best result is now recorded once in `MatchHUD` and
passed to both Results presentations; density, width, and text-scale rebuilds
reuse it from the Results snapshot. Outcome metadata truncates inside the space
left of the capped player card. Contribution headings, tiles, callout title/tag,
and match-mode text stay within their assigned widths; callout icon and copy
align in a density-aware header row, and tile value/subtitle spacing is explicit.
Runtime build passed with 0 errors and 44 existing warnings; Editor build passed
with 0 warnings/errors. No tests were run. Unity import/render, localized
expansion, and device review remain open. See
`docs/acceptance/PART5_RESULTS_TYPE_SCALE.md`.

### 12 October 2026 follow-up: Results Stream-Safe redaction

Results now rebuilds when Stream-Safe Display changes, retaining scroll and the cached result snapshot. The contribution card also checks the live preference before drawing the cached personal-best callout, preventing a later resize or text-scale rebuild from revealing it. Runtime build passed with 0 errors and 44 existing warnings. No tests were run. Unity toggle/capture and device acceptance remain open. See `docs/acceptance/PART9_STREAM_SAFE_DISPLAY.md` and `docs/acceptance/PART5_RESULTS_TYPE_SCALE.md`.

### 12 October 2026 follow-up: Results final source review

Fixed in `ResultsView.cs`/`MatchHUD.cs`: wide-row header/chip/label/cell line heights now use the 1.32 em box; the wide header note no longer overlaps the points chip; the outcome eyebrow/meta refit from the authored size; the DIRECT IMPACT label aligns with the tile edge and the MATCH MODE row is width-bounded; callout body follows the scaled icon/title column; personal best is now always recorded once at match end (Stream-Safe only hides it, no lost record). Runtime build 0 errors / 44 existing warnings; Network and Editor 0/0. No tests were run. Unity renders, locale expansion, narrow-phone column split, and device acceptance remain unverified. See `docs/acceptance/PART5_RESULTS_TYPE_SCALE.md` and `docs/acceptance/PART6_REPORT_FROM_SCOREBOARD.md`.

### 12 October 2026 follow-up: responsive Results report action label

The REPORT button now uses density-aware inner padding and sizes its localized label to the available width before ellipsizing. The 60 × 48 dp target remains unchanged. Runtime build passed with 0 errors and 44 existing warnings; Network and Editor builds passed with 0 warnings and errors. No tests were run. Unity/device text and touch acceptance remain open. See `docs/acceptance/PART6_REPORT_FROM_SCOREBOARD.md`.

### 12 October 2026 follow-up: spawn-centred Switchyard sightline blockers

The shared blocker geometry now derives its X centre from both configured spawn-room centres; the fail-closed validator still checks every exit opening and map footprint through the same helper. Shipped X=0 geometry is unchanged. Runtime build passed with 0 errors and 44 existing warnings; Network and Editor builds passed with 0 warnings and errors. No tests were added or run. Asymmetric Unity geometry, collider, NavMesh, server, traversal, and device checks remain open. See `docs/acceptance/PART8_MAP_CONFIG_VALIDATION.md`.

### 12 October 2026 follow-up: single-owner online pad-launch haptics

`NetPlayer` no longer emits a local haptic from its avatar render callback; `NetClientPresenter` is the single owner of the local haptic and camera cue on the local replicated launch transition. Per-avatar spatial launch audio remains in `NetPlayer`. Network build passed with 0 errors and 44 existing Runtime warnings; Editor build passed with 0 warnings and errors. No tests were added or run. Unity/online transition timing and Android/iOS haptic behavior remain unverified. See `docs/acceptance/PART6_PAD_LAUNCH_FEEDBACK.md`.

### 12 October 2026 follow-up: development-build thermal timeline

`DevicePerformanceProbe` now samples raw OS thermal state once per second on Android API 29+ and iOS 11+ development builds, emitting platform, availability, sample count, and a comma-separated raw timeline per 30-second performance window. Unsupported APIs and failed samples are explicit as unavailable/`-1`; the probe does not make a Gate G pass/fail decision. Added an iOS native bridge and updated the Gate G ledger and probe acceptance record. Runtime/Network/Editor and conditional Android/iOS C# builds passed with 0 errors; no tests were added or run. The iOS native bridge was not compiled into an Xcode player, and no device thermal traces were collected; Gate G remains open. Android/iOS device sampling and OS enum interpretation remain to be confirmed on target devices. See `docs/acceptance/PART7_PERFORMANCE_BUDGET_PROBE.md` and `docs/acceptance/GATE_G_READINESS_INPUTS.md`.

### 12 October 2026 follow-up: narrow Results card width allocation

Replaced the fixed 4/12 contribution and 8/12 roster split with physical-width
allocation: preserve 360 dp for three contribution tiles and 360 dp for the
compact roster, then stack both cards when the available width cannot satisfy
those minima plus their gap. The stacked view uses the existing vertical
scrolling page; wider viewports keep a side-by-side layout and give the roster
the remaining width. Match facts, report action eligibility, and sticky
controls are unchanged. Runtime, Network, and Editor builds passed with 0
errors; no tests were added or run. Unity render, localized expansion,
ellipsis behavior, and narrow/wide device acceptance remain open. See
`docs/acceptance/PART5_RESULTS_TYPE_SCALE.md`.
