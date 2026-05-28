# Generate Images — reference

## Grok vs GenerateImage

| | Grok | GenerateImage |
|---|------|----------------|
| Skill | [generate-grok-images](../generate-grok-images/SKILL.md) | [generate-images](../generate-images/SKILL.md) |
| Command | `./scripts/generate_grok_image.sh -p "..."` | Cursor `GenerateImage` tool |
| Draft output | `generated_images/<id>.*` + JSON metadata | Cursor assets path |
| Game install | `install_scene_image.py` (same) | `install_scene_image.py` (same) |
| Prompt log **tool** | `Grok` | `GenerateImage` |

## Scene background inventory

| PNG | Phase / use | Narrative constant (`Game.cs`) |
|-----|----------------|--------------------------------|
| `apartment-inside.png` | Opening | `OpeningNarrative` |
| `apartment-outside.png` | Outside (courtyard) | `OutsideNarrative` |
| `town.png` | Town | `TownNarrative` |
| `industrial.png` | Industrial District | `IndustrialDistrictNarrative` |
| `cafe.png` | Кафе (from industrial) | `CafeNarrative` |
| `cafe-owner-portrait.png` | Boris dialog portrait (3:4) | `CafeOwnerDialog` |
| `commercial.png` | Commercial District | `CommercialDistrictNarrative` |
| `store.png` | Convenience store | `StoreNarrative` |
| `forest-entry.png` | Forest entry | `ForestEntryNarrative` |
| `trees.png` | Forest | `ForestNarrative` |
| `forest-stream.png` | Forest stream | `ForestStreamNarrative` |
| `tent-interior.png` | Tent interior | `TentNarrative` |

**Not scene backgrounds:** `region-map.png` (generate-maps), `conscript-title.png`, `trash-bag-tent.png` (overlay prop), `items/*.png`.

**Standard size:** 1536×1024 RGB PNG.

## Narrative excerpts (prompt fodder)

Sync with `Game.cs` if strings change.

- **Town:** Empty streets under streetlights; industrial west, commercial east; shadows.
- **Industrial:** Warehouses, fenced lots, distant rail yard, few lit windows, edge of town.
- **Commercial:** Shopfronts, late-night kiosk glow, thin foot traffic, pines south of blocks.
- **Store:** Brutal fluorescent interior, security camera, clerk on phone — **interior**, not street.
- **Forest entry:** Edge of pines, leaving town behind.
- **Forest / stream:** Deep taiga, snow, survival — cold greens and browns, not urban.
- **Tent:** Crude trash-bag shelter interior — cramped, dim.

## Style palette (outdoor districts)

| Element | Guidance |
|---------|----------|
| Time | Night (districts/town); dawn/dusk only if narrative demands |
| Weather | Damp pavement, recent rain, reflections |
| Lights | Sodium amber streetlights; occasional cool window glow |
| Architecture | Soviet-era blocks, corrugated warehouses, shop neon for commercial |
| Vehicles | Old Lada/truck silhouettes OK; not the focus |
| Mood | Lonely, oppressive, cinematic — “doomer” urban photography |
| Avoid | People, readable signs, logos, fantasy, bright daytime |

## Example prompts

**Industrial district** (`industrial.png`):

```text
Cinematic nighttime photograph of a post-Soviet industrial district on the edge of a Siberian town. Narrow wet asphalt between weathered warehouses and corrugated factories, chain-link fences, rusted pipes, shipping containers, brick chimney. Warm sodium streetlights on rain-slick road; distant rail yard lights; old Soviet truck parked; sparse bare autumn trees. Moody gritty photorealistic, 3:2 landscape, no people, no text.
```

**Commercial district** (`commercial.png`):

```text
Cinematic nighttime photograph of a post-Soviet commercial street in a Siberian town. Small shopfronts, late-night convenience kiosk with warm neon, wet pavement reflecting signs, empty sidewalk, Khrushchyovka blocks in background. Same moody amber/cool palette as town scene but clearly retail not industrial. Photorealistic 3:2, no people, no readable text.
```

## Item icons (`items/*.png`)

- Smaller assets; sizes vary — check existing icon dimensions before resizing.
- Often need **transparent** backgrounds; GenerateImage may produce opaque PNGs — user may need manual alpha cleanup or accept solid backing.
- Only generate when explicitly requested; gameplay readability matters at small size.

## Wiring (no code change for swap-in)

Replacing `Conscript/img/<name>.png` does not require C# edits if `Game.Run()` already loads that filename. New phases need `Game.cs` texture field + `EnterPhase` switch entry.

Fallback pattern: `LoadTextureOrFallback("industrial.png", _townBackground)` — missing file silently uses town art; still install the PNG.
