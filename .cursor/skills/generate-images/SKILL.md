---
name: generate-images
description: >-
  Generate Conscript scene background PNGs with the GenerateImage tool, match
  the post-Soviet cinematic style, crop to 1536×1024, and install under
  Conscript/img/. Use when the user asks to create, regenerate, or replace game
  background art, district images, phase photos, or similar embedded textures —
  not for GeoPandas maps (use generate-maps) or tiny item icons.
---

# Generate Images (GenerateImage → game asset)

## Critical rule

**You have the `GenerateImage` tool. Use it.**

When the user asks for a new or updated scene background, do **not**:

- Only describe what the image should look like
- Search the web for stock photos
- Draw placeholders with matplotlib/PIL from scratch
- Copy an unrelated existing PNG without generating a new one

**Do** call `GenerateImage`, then install the result into `Conscript/img/`.

## When to use this skill

| Request | Skill |
|---------|--------|
| Town / industrial / commercial / forest scene photo | **This skill** |
| Sidebar `region-map.png` | [generate-maps](../generate-maps/SKILL.md) |
| Steam caps, app icons | `scripts/generate_steam_assets.py` |
| Small item icons (`items/*.png`) | GenerateImage if asked; often need transparent PNG — see [reference.md](reference.md) |

## Workflow checklist

Copy and complete:

```
- [ ] 1. Identify target file (grep Game.cs or user request)
- [ ] 2. Read scene narrative in Game.cs (*Narrative constants)
- [ ] 3. Read 1–2 reference PNGs in Conscript/img/ for style
- [ ] 4. Call GenerateImage with a detailed prompt (below)
- [ ] 5. Install: scripts/install_scene_image.py <generated> <dest>
- [ ] 6. Visually verify (Read tool on output PNG)
- [ ] 7. dotnet build conscript.slnx — embedded textures require rebuild
```

### Step 1 — Target file

Scene backgrounds load from `Conscript/img/` in `Game.Run()` (`EmbeddedTextureLoader`). Full mapping: [reference.md](reference.md).

Common filenames: `town.png`, `industrial.png`, `commercial.png`, `forest-entry.png`, `trees.png`, `forest-stream.png`, `store.png`, `tent-interior.png`, `apartment-inside.png`, `apartment-outside.png`.

### Step 2 — Narrative drives content

In `Conscript/Game.cs`, find the `*Narrative` string for the phase (e.g. `IndustrialDistrictNarrative`). **Translate narrative details into the image prompt** — warehouses, rail yard, neon shopfronts, etc.

Setting: **Ulan-Ude, Republic of Buryatia** — post-Soviet Siberian town, early autumn, on-the-run mood.

### Step 3 — Style anchors

Before prompting, **Read** at least one existing scene PNG (usually `town.png` or the asset being replaced). Match:

- Cinematic **photorealistic** night (or interior for store/tent)
- Wet asphalt / heavy atmosphere when outdoors
- Warm sodium streetlights, deep shadows, cool window accents
- Gritty urban decay; **no people**, **no readable text/logos**
- Landscape **3:2**, eye-level or slight low angle down a street/path

Do **not** clone another district’s subject matter (e.g. don’t reuse a residential courtyard for `industrial.png`).

### Step 4 — GenerateImage call

Use `GenerateImage` with:

- **description**: Full prompt (subject, lighting, palette, composition, exclusions). Include `3:2 aspect ratio` and `photorealistic`.
- **filename**: Short slug, e.g. `industrial-district-draft.png` (saved under Cursor assets; path returned in tool result)

**Prompt skeleton:**

```text
Cinematic photorealistic [interior|night photograph] of [SPECIFIC SCENE from narrative],
post-Soviet Siberian town (Ulan-Ude), early autumn, on-the-run atmosphere.
[KEY OBJECTS AND ARCHITECTURE from narrative and phase name.]
[MATCH STYLE: wet pavement / sodium streetlights / deep shadows / film grain — or fluorescent interior for store.]
Wide landscape 3:2 composition, no people, no text, no logos.
```

Regenerate if the result is wrong genre (residential blocks for industrial), daytime when night was intended, or cluttered with people/signage.

### Step 5 — Install at game resolution

All standard scene backgrounds are **1536×1024** (center-crop to 3:2, LANCZOS resize):

```bash
python3 scripts/install_scene_image.py \
  /path/to/generated.png \
  Conscript/img/industrial.png
```

Requires Pillow (`pip install Pillow` or `scripts/requirements.txt`).

### Step 6 — Verify in game

`DrawSceneBackground` scales the texture with `DrawTexturePro` into the central art panel; time-of-day tints apply outdoors. Rebuild, run, travel to the phase (e.g. Town → INDUSTRIAL DISTRICT).

## Troubleshooting

| Issue | Fix |
|-------|-----|
| Agent “forgot” to generate | Re-read this skill; **must** call `GenerateImage` |
| Image looks like wrong district | Regenerate; tighten prompt with narrative specifics |
| Stale texture in game | `dotnet build` after PNG change |
| Map markers wrong | Use **generate-maps**, not this skill |
| User wants data chart / diagram | Do not use GenerateImage; use code or canvas |

## Additional resources

- Asset table, narratives, item-icon notes: [reference.md](reference.md)
- Map pipeline: [generate-maps](../generate-maps/SKILL.md)
