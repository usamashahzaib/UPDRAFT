"""Build side-by-side reference/current comparison pages for the HOTDROP UI parity loop.

Stdlib only (no PIL in this environment). Copies each reference `screen.png` next to the
matching Unity runtime capture into ProjectHotdrop/UIEvidence/compare/ and writes one
`NN.html` per screen so the Preview tab can show them at full width.
"""
from __future__ import annotations

import base64
import shutil
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
UIUX = ROOT / "UIUX"
EVID = ROOT / "ProjectHotdrop" / "UIEvidence"
OUT = EVID / "compare"

# ref: path relative to UIUX, cur: path relative to ProjectHotdrop/UIEvidence ('' = no capture yet)
SCREENS = [
    ("01", "Startup / connection",
     "stitch_project_continuation_hub (1)/stitch_project_continuation_hub/screen_01_startup_connection_normal_state/screen.png",
     "journey_2026-10-02/01_startup_1920x1080.png"),
    ("02", "First-launch appearance choice",
     "stitch_project_continuation_hub (1)/stitch_project_continuation_hub/screen_02_first_launch_appearance_choice/screen.png",
     "journey_2026-10-02/02_runner_choice_1920x1080.png"),
    ("03", "Home / lobby",
     "stitch_project_continuation_hub/screen_3_home_lobby_production_clean/screen.png",
     "journey_2026-10-02/03_home_1920x1080.png"),
    ("04", "Guided first-run training",
     "stitch_project_continuation_hub (1)/stitch_project_continuation_hub/screen_04_guided_first_run_training_step_5_of_9/screen.png",
     "parity_2026-10-03/04_training_1920x1080.png"),
    ("05", "Party room",
     "stitch_project_continuation_hub (2)/stitch_project_continuation_hub/screen_05_party_room_production_state/screen.png",
     ""),
    ("06", "Matchmaking search",
     "stitch_project_continuation_hub (2)/stitch_project_continuation_hub/screen_06_matchmaking_search_state/screen.png",
     ""),
    ("07", "Loadout / preparation",
     "stitch_project_continuation_hub (2)/stitch_project_continuation_hub/screen_07_loadout_preparation_8s_countdown/screen.png",
     "parity_2026-10-03/07_preparation_1920x1080.png"),
    ("08", "Gameplay HUD",
     "stitch_project_continuation_hub (3)/stitch_project_continuation_hub/screen_08_gameplay_hud_classic_preset/screen.png",
     "playable_2026-10-03/08_playable_relay_1920x1080.png"),
    ("09", "Launch pad placement feedback",
     "stitch_project_continuation_hub (3)/stitch_project_continuation_hub/screen_09_launch_pad_placement_feedback/screen.png",
     "parity_2026-10-03/09_pad_feedback_1920x1080.png"),
    ("10", "Match menu / settings",
     "stitch_project_continuation_hub (3)/stitch_project_continuation_hub/screen_10_match_menu_settings_online_live/screen.png",
     "journey_2026-10-02/10_settings_1920x1080.png"),
    ("11", "Match results",
     "stitch_project_continuation_hub (4)/stitch_project_continuation_hub/screen_11_match_results_verified_contribution/screen.png",
     "parity_2026-10-03/11_results_1920x1080.png"),
    ("12", "Contracts / flight seals",
     "stitch_project_continuation_hub (4)/stitch_project_continuation_hub/screen_12_contracts_flight_seals/screen.png",
     ""),
    ("13", "Flight log / mastery",
     "stitch_project_continuation_hub (4)/stitch_project_continuation_hub/screen_13_flight_log_mastery_tracks/screen.png",
     ""),
    ("14", "Offline travel challenges",
     "stitch_project_continuation_hub (5)/stitch_project_continuation_hub/screen_14_offline_travel_challenges/screen.png",
     "challenges_2026-10-03/14_detail_1920x1080.png"),
    ("15", "Appearance locker",
     "stitch_project_continuation_hub (5)/stitch_project_continuation_hub/screen_15_appearance_locker/screen.png",
     "parity_2026-10-03/15_appearance_1920x1080.png"),
    ("16", "Operative profile",
     "stitch_project_continuation_hub (5)/stitch_project_continuation_hub/screen_16_operative_profile/screen.png",
     "parity_2026-10-03/16_profile_1920x1080.png"),
    ("17", "Account recovery / support",
     "stitch_project_continuation_hub (6)/stitch_project_continuation_hub/screen_17_account_recovery_support_reporting/screen.png",
     "journey_2026-10-02/17_support_1920x1080.png"),
    ("18", "Future shop (gated)",
     "stitch_project_continuation_hub (6)/stitch_project_continuation_hub/screen_18_future_shop_concept_disabled_from_main_flow/screen.png",
     ""),
]

PAGE = """<!doctype html>
<html><head><meta charset="utf-8"><style>
html,body{{margin:0;background:#000;color:#eee;font:12px/1.35 monospace}}
.wrap{{width:98vw;margin:0 auto;padding:4px}}
.bar{{display:flex;justify-content:space-between;background:#161616;padding:3px 6px}}
.bar b{{color:#7CFC00}}
.bar span{{color:#FFD166}}
img{{display:block;width:100%;image-rendering:pixelated}}
.miss{{padding:24px;border:1px dashed #666;color:#f66;text-align:center}}
</style></head><body><div class="wrap">
<div class="bar"><b>REFERENCE — {ref_label}</b><span>{title}</span></div>
<img src="{ref_src}">
<div class="bar"><b>CURRENT — Unity runtime capture</b><span>{cur_name}</span></div>
{cur_img}
</div></body></html>
"""


def data_uri(path: Path) -> str:
    """Inline the PNG so the page works when the preview serves only this one file."""
    return "data:image/png;base64," + base64.b64encode(path.read_bytes()).decode("ascii")


def main() -> None:
    OUT.mkdir(parents=True, exist_ok=True)
    index = ["<html><head><meta charset=utf-8><style>body{background:#000;color:#eee;font:13px monospace}a{color:#7CFC00}</style></head><body>"]
    for num, title, ref_rel, cur_rel in SCREENS:
        ref = UIUX / ref_rel
        if not ref.exists():
            print(f"!! missing ref {num}: {ref}")
            continue
        shutil.copyfile(ref, OUT / f"{num}_ref.png")
        cur_img = '<div class="miss">no runtime capture yet</div>'
        cur_name = "—"
        if cur_rel:
            cur = EVID / cur_rel
            if cur.exists():
                shutil.copyfile(cur, OUT / f"{num}_cur.png")
                cur_img = f'<img src="{data_uri(cur)}">'
                cur_name = cur.name
            else:
                print(f"!! missing capture {num}: {cur}")
        (OUT / f"{num}.html").write_text(
            PAGE.format(ref_label=ref_rel, ref_src=data_uri(ref), title=f"SCREEN {num} — {title}",
                        cur_img=cur_img, cur_name=cur_name), encoding="utf-8")
        index.append(f'<p><a href="{num}.html">Screen {num} — {title}</a> ({cur_name})</p>')
        print(f"{num}: {title} -> {cur_name}")
    index.append("</body></html>")
    (OUT / "index.html").write_text("\n".join(index), encoding="utf-8")


if __name__ == "__main__":
    main()
