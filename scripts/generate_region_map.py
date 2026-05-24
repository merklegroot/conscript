#!/usr/bin/env python3
"""
Build the sidebar region map (Lake Baikal, Buryatia, Mongolia border).
Run from repo root:
  pip install -r scripts/requirements.txt
  python scripts/generate_region_map.py
"""

from __future__ import annotations

import json
import urllib.request
import zipfile
from pathlib import Path

import geopandas as gpd
import matplotlib.pyplot as plt
from shapely.geometry import box

REPO_ROOT = Path(__file__).resolve().parents[1]
OUT_DIR = REPO_ROOT / "Conscript" / "img"
DATA_DIR = Path(__file__).resolve().parent / "data"
OUT_PNG = OUT_DIR / "region-map.png"
OUT_META = OUT_DIR / "region-map.bounds.json"

# Russia-centric view — Urals to Pacific, Arctic to Central Asia (orient the player globally)
BOUNDS = (22.0, 175.0, 35.0, 74.0)  # min_lon, max_lon, min_lat, max_lat
WIDTH_PX = 2048
GEO_ASPECT = (BOUNDS[1] - BOUNDS[0]) / (BOUNDS[3] - BOUNDS[2])
HEIGHT_PX = int(round(WIDTH_PX / GEO_ASPECT))  # equal-degree projection, no stretch
DPI = 100

NE_50M_ADMIN1 = (
    "https://naciscdn.org/naturalearth/50m/cultural/"
    "ne_50m_admin_1_states_provinces.zip"
)
NE_50M_LAKES = (
    "https://naciscdn.org/naturalearth/50m/physical/ne_50m_lakes.zip"
)
NE_50M_COUNTRIES = (
    "https://naciscdn.org/naturalearth/50m/cultural/ne_50m_admin_0_countries.zip"
)


def download_zip(url: str, name: str) -> Path:
    DATA_DIR.mkdir(parents=True, exist_ok=True)
    zip_path = DATA_DIR / f"{name}.zip"
    extract_dir = DATA_DIR / name
    if not extract_dir.exists():
        print(f"Downloading {url} ...")
        urllib.request.urlretrieve(url, zip_path)
        with zipfile.ZipFile(zip_path, "r") as zf:
            zf.extractall(extract_dir)
    shp = next(extract_dir.rglob("*.shp"))
    return shp


def load_layer(url: str, name: str) -> gpd.GeoDataFrame:
    return gpd.read_file(download_zip(url, name))


def main() -> None:
    min_lon, max_lon, min_lat, max_lat = BOUNDS
    clip_box = box(min_lon, min_lat, max_lon, max_lat)

    countries = load_layer(NE_50M_COUNTRIES, "ne_50m_admin_0_countries")
    admin1 = load_layer(NE_50M_ADMIN1, "ne_50m_admin_1_states_provinces")
    lakes = load_layer(NE_50M_LAKES, "ne_50m_lakes")

    region_countries = countries.clip(clip_box)

    buryatia = admin1[
        admin1["name"].str.contains("Buryat", case=False, na=False)
    ].clip(clip_box)

    nearby = admin1[
        admin1["name"].str.contains("Irkutsk", case=False, na=False)
        | admin1["name"].str.contains("Zabay", case=False, na=False)
        | admin1["name"].str.contains("Tuva", case=False, na=False)
    ].clip(clip_box)

    baikal = lakes[lakes["name"].str.contains("Baikal", case=False, na=False)].clip(
        clip_box
    )

    fig_w = WIDTH_PX / DPI
    fig_h = HEIGHT_PX / DPI
    fig, ax = plt.subplots(figsize=(fig_w, fig_h), dpi=DPI)
    fig.patch.set_facecolor("#0c0e12")
    ax.set_facecolor("#12161c")

    region_countries.plot(
        ax=ax, color="#1a1e26", edgecolor="#3a4048", linewidth=0.6, zorder=1
    )
    nearby.plot(
        ax=ax, color="#161a22", edgecolor="#2e333c", linewidth=0.4, zorder=2
    )
    buryatia.plot(
        ax=ax, color="#222a1e", edgecolor="#4a5240", linewidth=0.9, zorder=3
    )
    if not baikal.empty:
        baikal.plot(ax=ax, color="#2e4a58", edgecolor="#3d6270", linewidth=0.5, zorder=4)

    ax.set_xlim(min_lon, max_lon)
    ax.set_ylim(min_lat, max_lat)
    ax.set_aspect("equal", adjustable="box")
    ax.axis("off")

    # Labels (no marker baked in — the game draws the live position)
    if not baikal.empty:
        c = baikal.geometry.representative_point().iloc[0]
        ax.text(
            c.x - 0.8,
            c.y + 0.4,
            "Lake Baikal",
            fontsize=8,
            color="#6a8a9a",
            ha="right",
            va="bottom",
        )
    if not buryatia.empty:
        c = buryatia.geometry.representative_point().iloc[0]
        ax.text(
            c.x,
            c.y,
            "Buryatia",
            fontsize=8,
            color="#7a7668",
            ha="center",
            va="center",
        )

    ax.text(
        min_lon + 2.0,
        max_lat - 1.5,
        "Europe",
        fontsize=7,
        color="#5a5d64",
        ha="left",
        va="top",
    )
    ax.text(
        37.6,
        55.8,
        "Moscow",
        fontsize=7,
        color="#6a6a72",
        ha="left",
        va="bottom",
    )
    ax.text(
        100.0,
        62.0,
        "Siberia",
        fontsize=8,
        color="#5a5d64",
        ha="center",
        va="center",
    )
    ax.text(
        max_lon - 1.5,
        (min_lat + max_lat) / 2,
        "Pacific",
        fontsize=7,
        color="#5a5d64",
        ha="right",
        va="center",
    )
    ax.text(
        (min_lon + max_lon) / 2,
        min_lat + 1.0,
        "Mongolia",
        fontsize=7,
        color="#5a5d64",
        ha="center",
        va="bottom",
    )
    ax.text(
        (min_lon + max_lon) / 2,
        max_lat - 1.0,
        "Arctic Ocean",
        fontsize=7,
        color="#5a5d64",
        ha="center",
        va="top",
    )

    OUT_DIR.mkdir(parents=True, exist_ok=True)
    fig.savefig(
        OUT_PNG,
        dpi=DPI,
        facecolor=fig.get_facecolor(),
        edgecolor="none",
        bbox_inches=None,
        pad_inches=0,
    )
    plt.close(fig)

    from PIL import Image

    with Image.open(OUT_PNG) as img:
        png_w, png_h = img.size
    meta = {
        "minLon": min_lon,
        "maxLon": max_lon,
        "minLat": min_lat,
        "maxLat": max_lat,
        "width": png_w,
        "height": png_h,
        "ulanUde": {"lon": 107.584, "lat": 51.834},
        "forestCamp": {"lon": 107.35, "lat": 51.95},
    }
    OUT_META.write_text(json.dumps(meta, indent=2))
    print(f"Wrote {OUT_PNG}")
    print(f"Wrote {OUT_META}")


if __name__ == "__main__":
    main()
