# Map generation reference

## Natural Earth 50m URLs (Conscript defaults)

```
Countries:  https://naciscdn.org/naturalearth/50m/cultural/ne_50m_admin_0_countries.zip
Admin-1:    https://naciscdn.org/naturalearth/50m/cultural/ne_50m_admin_1_states_provinces.zip
Lakes:      https://naciscdn.org/naturalearth/50m/physical/ne_50m_lakes.zip
```

Use **10m** only if 50m boundaries look too coarse at the chosen zoom.

## Useful column names

| Layer | Filter columns |
|-------|----------------|
| Countries | `NAME` (e.g. Russia, Mongolia, China) |
| Admin-1 | `name` (e.g. Buryat, Irkutsk, Zabay, Tuva) |
| Lakes | `name` (e.g. Baikal) |

Inspect with:

```python
print(admin1["name"].dropna().sort_values().tolist())
```

## bounds.json schema

```json
{
  "minLon": 103.0,
  "maxLon": 110.8,
  "minLat": 50.0,
  "maxLat": 54.2,
  "width": 496,
  "height": 200,
  "ulanUde": { "lon": 107.584, "lat": 51.834 },
  "forestCamp": { "lon": 107.35, "lat": 51.95 }
}
```

Add named markers for each game phase or POI. C# can hardcode the same numbers (current Conscript approach) or load JSON at startup if maps multiply.

## Minimal new-map script skeleton

```python
#!/usr/bin/env python3
from pathlib import Path
import json
import geopandas as gpd
import matplotlib.pyplot as plt
from shapely.geometry import box

BOUNDS = (min_lon, max_lon, min_lat, max_lat)  # noqa: fill in
OUT_PNG = Path("Conscript/img/my-map.png")
OUT_META = Path("Conscript/img/my-map.bounds.json")

def main():
    min_lon, max_lon, min_lat, max_lat = BOUNDS
    clip_box = box(min_lon, min_lat, max_lon, max_lat)
    # load layers, .clip(clip_box), .plot(ax=ax, ...)
    fig, ax = plt.subplots(figsize=(4.96, 2.0), dpi=100)
    # style + savefig + write json

if __name__ == "__main__":
    main()
```

## Alternatives (when not to use GeoPandas)

| Approach | Use when |
|----------|----------|
| **Static tile export** (QGIS, Mapbox screenshot) | Pixel-perfect cartography; no programmatic clip |
| **Cartopy / contextily** | Need satellite or OSM basemap (heavier deps) |
| **SVG export** | Vector UI at infinite zoom (Raylib needs raster or SVG parser) |

For Conscript's dark minimalist sidebar, Natural Earth + matplotlib stays lightweight and reproducible.

## Conscript file map

```
scripts/
  generate_region_map.py   # source generator
  requirements.txt         # geopandas, matplotlib, shapely
  data/                    # cached NE zips (gitignored)
Conscript/
  img/region-map.png
  img/region-map.bounds.json
  Game.cs                  # RegionMap* constants, DrawWorldMap
  Conscript.csproj         # EmbeddedResource img/**/*
```
