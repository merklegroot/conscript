---
name: generate-maps
description: >-
  Generate real-world sidebar or UI maps from Natural Earth shapefiles using
  GeoPandas and matplotlib, export PNG plus bounds JSON, and wire lat/lon markers
  into the Raylib game. Use when the user asks to create, regenerate, or extend
  geographic maps, region maps, GeoPandas maps, map markers, or map bounds for
  Conscript or similar embedded-texture games.
---

# Generate Maps (GeoPandas → game asset)

## When to use this skill

- Regenerating **Conscript**'s sidebar map (`region-map.png`)
- Adding a **new region** or zoom level for the game
- Changing map **style**, **labels**, or **marker coordinates**
- Syncing **C# constants** with exported bounds metadata

**Default stack:** GeoPandas + matplotlib + Natural Earth 50m shapefiles (cached under `scripts/data/`). The game loads a **static PNG** at runtime; Python runs at **build/dev time only**.

## Conscript quick path (existing map)

From repo root:

```bash
python3 -m pip install -r scripts/requirements.txt
python3 scripts/generate_region_map.py
dotnet build conscript.slnx
```

**Outputs**

| File | Purpose |
|------|---------|
| `Conscript/img/region-map.png` | Embedded map texture (`img/**/*` in `.csproj`) |
| `Conscript/img/region-map.bounds.json` | Lon/lat bounds + marker coordinates |

**After changing bounds or markers**, update matching constants in `Conscript/Game.cs`:

- `RegionMapMinLon` / `RegionMapMaxLon` / `RegionMapMinLat` / `RegionMapMaxLat`
- `UlanUdeLon` / `UlanUdeLat`, `ForestCampLon` / `ForestCampLat`

Marker projection (screen Y flipped vs latitude):

```csharp
double nx = (lon - RegionMapMinLon) / (RegionMapMaxLon - RegionMapMinLon);
double ny = (RegionMapMaxLat - lat) / (RegionMapMaxLat - RegionMapMinLat);
```

Drawn in `DrawWorldMap` via `GeoToMapPixel`. **Do not bake the player marker into the PNG** — the game overlays it from live coordinates.

## Workflow for a new or changed map

Copy this checklist and complete each step:

```
- [ ] 1. Define geographic need (bounds, regions, labels, resolution)
- [ ] 2. Edit or fork scripts/generate_region_map.py
- [ ] 3. Run script; verify PNG + bounds JSON
- [ ] 4. Embed PNG (Conscript/img/*.png auto-embedded)
- [ ] 5. Sync Game.cs constants with bounds JSON
- [ ] 6. Load texture in Run(); unload on exit
- [ ] 7. dotnet build && visual check in sidebar
```

### Step 1 — Define bounds

Pick `(min_lon, max_lon, min_lat, max_lat)` with enough context (neighbors, water, borders) but keep the sidebar readable. Conscript default:

```python
BOUNDS = (103.0, 110.8, 50.0, 54.2)  # eastern Buryatia / Baikal
```

Use [Natural Earth](https://www.naturalearthdata.com/) admin layers; prefer **50m** for sidebar scale (smaller download than 10m).

### Step 2 — Script pattern

Follow `scripts/generate_region_map.py`:

1. **Download once** — `urllib` + zip extract to `scripts/data/{layer_name}/`
2. **Load** — `gpd.read_file(shp_path)`
3. **Clip** — `gdf.clip(shapely.geometry.box(min_lon, min_lat, max_lon, max_lat))`
4. **Filter** — country names, `admin1` name contains (e.g. `"Buryat"`), lake names (e.g. `"Baikal"`)
5. **Plot** — dark UI palette (`#0c0e12` figure, muted fills, thin borders)
6. **`ax.set_aspect("equal")`**, `ax.axis("off")`, fixed `xlim`/`ylim` to `BOUNDS`
7. **Export** — PNG at target pixel size (`WIDTH_PX` / `HEIGHT_PX` / `DPI`)
8. **Write bounds JSON** — min/max lon/lat, image size, named marker `{lon, lat}` entries

**Older GeoPandas:** use `shapely.geometry.box(...)` for clip, not `GeoSeries.from_bbox`.

### Step 3 — Visual style (match Conscript UI)

| Element | Typical color |
|---------|----------------|
| Figure background | `#0c0e12` |
| Axes background | `#12161c` |
| Countries | `#1a1e26` fill, `#3a4048` edge |
| Highlight region (e.g. Buryatia) | `#222a1e` fill, `#4a5240` edge |
| Lakes | `#2e4a58` fill |
| Labels | `#5a5d64` – `#7a7668`, fontsize 5–6 |

Keep labels minimal; orient the player, don't clutter.

### Step 4 — Game integration (Raylib-cs)

1. Add `private Texture2D _…MapTexture;`
2. `LoadEmbeddedTexture("your-map.png")` in `Run()`
3. `UnloadTexture` on exit
4. `DrawTexturePro` in sidebar draw method; `DrawRectangleLines` border
5. Overlay marker with `GeoToMapPixel` — never draw marker in matplotlib unless it's a fixed POI

### Step 5 — Verify

- Script exits 0; PNG exists and looks correct at 100% zoom
- `bounds.json` lon/lat matches `Game.cs` constants
- Marker sits on expected geography (Ulan-Ude east of Baikal)
- `dotnet build` succeeds; embedded resource name is `region-map.png` under `Conscript/img/`

## Extending Conscript's map

| Change | Edit |
|--------|------|
| Zoom / pan | `BOUNDS` in `generate_region_map.py` + all `RegionMap*` constants |
| New location marker | Add entry to bounds JSON + `GetMapPlayerGeoPosition()` switch |
| More regions highlighted | Filter `admin1` / `countries` in script |
| Larger sidebar map | `WIDTH_PX`/`HEIGHT_PX` and `DrawWorldMap` `mapH` |

For a **second map** (e.g. all-Russia overview), add a new script + PNG name; don't overload one bounds file.

## Troubleshooting

| Issue | Fix |
|-------|-----|
| `GeoSeries.from_bbox` missing | Use `box(min_lon, min_lat, max_lon, max_lat)` from shapely |
| Empty Buryatia layer | Check `admin1["name"]` values; Natural Earth spelling varies |
| Marker misplaced | Bounds in C# must match JSON; remember `ny` uses `maxLat - lat` |
| Stale map in game | Rebuild after PNG change (embedded resource) |
| Huge git diff | Commit PNG; keep `scripts/data/` gitignored |

## Additional resources

- Conscript script: [scripts/generate_region_map.py](../../../scripts/generate_region_map.py)
- Python deps: [scripts/requirements.txt](../../../scripts/requirements.txt)
- Game wiring: `DrawWorldMap`, `GeoToMapPixel` in [Conscript/Game.cs](../../../Conscript/Game.cs)
- Layer URLs and field names: [reference.md](reference.md)
