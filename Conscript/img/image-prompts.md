# Image prompts log

This file tracks the exact prompts used to generate `Conscript/img/*.png` so we can easily regenerate/tweak later.

## Format

- **asset**: destination file under `Conscript/img/`
- **date**: ISO-8601 local date
- **model**: generator model (e.g. Composer 2.5 Fast for GenerateImage; `grok-imagine-image-quality` for Grok)
- **tool**: `GenerateImage` or `Grok` (`scripts/generate_grok_image.sh` / `image_gen`)
- **prompt**: exact `description` string passed to the tool
- **notes**: install script / wiring details

**Inferred prompts** for rooms without a log entry live in `image_gen/lib/room-prompts.ts` and appear on each room page in the image_gen app (`/rooms` → room → **Generate with Grok**).

---

## `warehouse-14-interior.png`

- **asset**: `Conscript/img/warehouse-14-interior.png`
- **date**: 2026-05-28
- **model**: Composer 2.5 Fast (GenerateImage)
- **tool**: `GenerateImage` + `scripts/install_scene_image.py`
- **source**: `warehouse-14-interior-draft.png`
- **prompt**:

```text
Cinematic photorealistic interior of a Soviet-era warehouse hangar at night, Ulan-Ude industrial yard, early autumn. View from inside looking down a concrete aisle between tall metal shelving and stacked wooden pallets, shrink-wrapped cargo, rusted forklift in shadow. Cold fluorescent tubes overhead, one flickering, deep shadows, wet floor reflecting light. At the far end, a partially open corrugated roll-up door with rain and orange firelight glow from outside (aftermath of explosion beyond). Moody gritty post-Soviet atmosphere, subtle film grain. Wide landscape 3:2, exactly 1536×1024. No readable text, no people, no logos.
```

- **notes**:
  - `Phase.WarehouseInterior` — entered after keypad code `4237` on the aftermath bay lock.

## `folded-paper-note.png`

- **asset**: `Conscript/img/folded-paper-note.png`
- **date**: 2026-05-28
- **model**: Composer 2.5 Fast (GenerateImage)
- **tool**: `GenerateImage`
- **prompt**:

```text
A worn half-sheet of ledger paper photographed flat on a dark surface, post-Soviet warehouse mood. Cream-yellowed paper with grease smudges, torn edge, folded crease lines. Handwritten block letters in dark ink, clearly legible, centered on the paper exactly as written:

For the last time, Vitya,
Tomorrow is the day.
Treason or not, it will be done.
Severe consequences will follow.

No other text. Photorealistic, soft side lighting, subtle grain. Portrait orientation roughly 3:4 aspect ratio. No people, no hands, no UI frames.
```

- **notes**:
  - Shown full-size in `FoldedPaperReaderDialog` when the player presses READ on Folded Paper.
  - Also used as the backpack/item icon for `GameItems.FoldedPaper`.

## `truck-gas-gauge-face.png` / `truck-gas-gauge-needle.png`

- **assets**: `Conscript/img/truck-gas-gauge-face.png`, `Conscript/img/truck-gas-gauge-needle.png`
- **date**: 2026-05-29
- **model**: Composer 2.5 Fast (GenerateImage)
- **tool**: `GenerateImage`
- **prompt (face)**:

```text
Round analog fuel gauge face ONLY, no needle, no pointer, empty dial. Soviet-era truck dashboard style, amber orange backlit markings with E at lower left and F at lower right, black face, weathered rusty metal bezel ring. Photorealistic close-up, square 1:1 composition, gauge centered and fills most of frame. Solid black background outside the circular bezel. No people, no hands, no needle.
```

- **prompt (needle)**:

```text
Single thin illuminated orange fuel gauge needle pointer ONLY, pointing straight up (12 o'clock), pivot hub at the very bottom center of the needle. Transparent background. Post-Soviet truck dashboard style, photorealistic, glowing amber-orange metal. Tall narrow composition, needle is isolated with no dial, no bezel, no other parts.
```

- **notes**:
  - Procedural gauge in `GasGaugeViewerDialog` when clicking the fuel gauge hotspot in `Phase.DeliveryTruck` (`delivery-truck-cab.png`).
  - Five discrete levels (empty → full); ◀ ▶ arrows rotate the needle between E and F.

## `warehouse-closed-door.png`

- **asset**: `Conscript/img/warehouse-closed-door.png`
- **date**: 2026-05-29
- **model**: Composer 2.5 Fast (GenerateImage)
- **tool**: `GenerateImage`
- **prompt**:

```text
Photorealistic closed corrugated steel roll-up warehouse door, Soviet-era industrial hangar at night in heavy rain. Dark gray-blue metal with horizontal ribs, wet surface catching cold white floodlight and faint warm glow from nearby fire (no flames visible through door). Door fully lowered and sealed, weathered with light rust streaks. Front-facing, fills entire frame edge to edge. Cinematic gritty post-Soviet mood, subtle film grain. Portrait orientation roughly 5:6 aspect ratio. No people, no text, no logos, no open gap.
```

- **notes**:
  - Drawn over `warehouse-14-aftermath.png` in the door hotspot `(0.282, 0.297)–(0.557, 0.632)` while the keypad lock is not unlocked.
  - Hidden once the player enters the correct code and the door is open.

## `crate-note.png`

- **asset**: `Conscript/img/crate-note.png`
- **date**: 2026-05-29
- **model**: Composer 2.5 Fast (GenerateImage)
- **tool**: `GenerateImage`
- **prompt**:

```text
A worn half-sheet of ledger paper photographed flat on a dark surface, post-Soviet warehouse mood. Cream-yellowed gridded paper with grease smudges, torn left edge, folded crease lines. Handwritten block letters in dark ink, clearly legible, centered on the paper exactly as written:

Boris,
I've been patient.
It's time for you to deliver the product.
Meet me at the border.

No other text. Photorealistic, soft side lighting, subtle grain. Portrait orientation roughly 3:4 aspect ratio. No people, no hands, no UI frames. Match the style of a threatening handwritten note on aged Soviet ledger paper.
```

- **notes**:
  - Shown full-size in `FoldedPaperReaderDialog` when the player presses READ on the crate `Note`.
  - Also used as the backpack/item icon for `GameItems.Note`.

## `warehouse-14-aftermath.png`

- **asset**: `Conscript/img/warehouse-14-aftermath.png`
- **date**: 2026-05-28
- **model**: Composer 2.5 Fast (GenerateImage)
- **tool**: `GenerateImage`
- **source**: `warehouse-14-aftermath-draft.png`
- **prompt**:

```text
Cinematic photorealistic nighttime scene at a Soviet-era warehouse loading bay in Ulan-Ude, Republic of Buryatia, early autumn, heavy rain. Same composition as a warehouse ambush scene: ground-level view on wet concrete near an old green delivery truck with open cab door on the right, corrugated roll-up door ahead — but NOW showing violent explosion aftermath. Roiling orange fire and black smoke billowing from the doorway, scorched and blackened metal door, shattered windows, burning pallets, charred barrels, rain hissing on embers. Two indistinct motionless silhouettes of men sprawled on the wet concrete near the flames (no clear faces, no gore). Floodlights still cutting through rain and smoke, long reflections in puddles, chain-link fence beyond. Moody gritty post-Soviet atmosphere, dramatic firelight vs cool rain, subtle film grain. Wide landscape 3:2, exactly 1536×1024. No readable text, no signage, no logos.
```

- **notes**:
  - Installed via `scripts/install_scene_image.py`.
  - Wired in `Conscript/Game.cs` as the background for `Phase.WarehouseAftermath` (fallback to `warehouse-14-ambush.png`).

## `warehouse-14-ambush.png`

- **asset**: `Conscript/img/warehouse-14-ambush.png`
- **date**: 2026-05-28
- **model**: `grok-imagine-image-quality`
- **tool**: `Grok` (`image_gen` web app)
- **source**: `generated_images/20260528-161231-d34e8d.jpg` (+ `20260528-161231-d34e8d.json`)
- **prompt**:

```text
Cinematic photorealistic nighttime scene at a Soviet-era warehouse loading bay in Ulan-Ude, Republic of Buryatia, early autumn, heavy rain. Ground-level view on wet concrete near an old delivery truck (edge of open truck door and a sliver of cab interior framing the right side), looking toward a half-open corrugated roll-up door spilling warm industrial light. Floodlights cut through rain, long reflections in puddles, scattered pallets and metal barrels, chain-link fence and shadowy yard beyond. Ominous tension: two indistinct threatening silhouettes standing near the doorway in deep shadow (no clear faces, no readable patches, no logos). Moody gritty post-Soviet atmosphere, subtle film grain, warm/cool mixed lighting, deep shadows. Wide landscape 3:2, exactly 1536×1024. No readable text, no signage, no gore, no prominent weapons.
```

- **notes**:
  - Installed via `scripts/install_scene_image.py` (JPEG → 1536×1024 PNG).
  - Wired in `Conscript/Game.cs` as the background for `Phase.WarehouseAmbush` (fallback to `warehouse-14.png`).

