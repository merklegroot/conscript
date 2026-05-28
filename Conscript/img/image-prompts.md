# Image prompts log

This file tracks the exact prompts used to generate `Conscript/img/*.png` so we can easily regenerate/tweak later.

## Format

- **asset**: destination file under `Conscript/img/`
- **date**: ISO-8601 local date
- **model**: the Cursor chat model used for the `GenerateImage` call (best-effort)
- **tool**: `GenerateImage`
- **prompt**: exact `description` string passed to the tool
- **notes**: install script / wiring details

---

## `warehouse-14-ambush.png`

- **asset**: `Conscript/img/warehouse-14-ambush.png`
- **date**: 2026-05-28
- **model**: Composer 2.5 Fast
- **tool**: `GenerateImage`
- **prompt**:

```text
Cinematic photorealistic nighttime scene at a Soviet-era warehouse loading bay in Ulan-Ude, Republic of Buryatia, early autumn, heavy rain. Ground-level view on wet concrete near an old delivery truck (edge of open truck door and a sliver of cab interior framing the right side), looking toward a half-open corrugated roll-up door spilling warm industrial light. Floodlights cut through rain, long reflections in puddles, scattered pallets and metal barrels, chain-link fence and shadowy yard beyond. Ominous tension: two indistinct threatening silhouettes standing near the doorway in deep shadow (no clear faces, no readable patches, no logos). Moody gritty post-Soviet atmosphere, subtle film grain, warm/cool mixed lighting, deep shadows. Wide landscape 3:2, exactly 1536×1024. No readable text, no signage, no gore, no prominent weapons.
```

- **notes**:
  - Installed via `scripts/install_scene_image.py` to enforce 1536×1024.
  - Wired in `Conscript/Game.cs` as the background for `Phase.WarehouseAmbush` (fallback to `warehouse-14.png`).

