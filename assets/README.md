# Conscript — icons & Steam assets

Generated from masters in `source/` via:

```bash
python3 -m pip install Pillow
python3 scripts/generate_steam_assets.py
```

Edit the PNG masters in `source/`, re-run the script, then upload the outputs below in Steamworks.

## Windows / macOS executable

| File | Use |
|------|-----|
| `icons/conscript.ico` | Embedded in Windows `.exe` (via `ApplicationIcon` in `Conscript.csproj`) |
| `icons/conscript.icns` | macOS app bundle / Steam “Mac Icon” field (regenerate with script on macOS) |

## Steam client icons (Community & Client)

| Steamworks field | Upload |
|------------------|--------|
| Shortcut Icon | `icons/shortcut-icon-256.png` or `icons/conscript.ico` |
| App Icon | `icons/app-icon-184.jpg` |
| Mac Icon | `icons/conscript.icns` |

## Store capsules (required)

| Steamworks field | Upload |
|------------------|--------|
| Header Capsule | `steam/header-capsule.png` (920×430) |
| Small Capsule | `steam/small-capsule.png` (462×174) |
| Main Capsule | `steam/main-capsule.png` (1232×706) |
| Vertical Capsule | `steam/vertical-capsule.png` (748×896) |

Check readability at the auto-generated tiny size: `steam/_preview-small-120x45.png`.

## Library assets (required)

| Steamworks field | Upload |
|------------------|--------|
| Library Capsule | `steam/library-capsule.png` (600×900) |
| Library Header Capsule | `steam/library-header-capsule.png` (920×430) |
| Library Hero | `steam/library-hero.png` (3840×1240, artwork only) |
| Library Logo | `steam/library-logo.png` (1280×720, transparent PNG) |

## Optional

| Steamworks field | Upload |
|------------------|--------|
| Page Background | `steam/page-background.png` (1438×810) |

## Source art

| File | Role |
|------|------|
| `source/conscript-icon-master.png` | App / shortcut icon emblem |
| `source/conscript-capsule-master.png` | Landscape store & library hero |
| `source/conscript-vertical-master.png` | Portrait / library grid capsule |

Official specs: [Steamworks graphical assets](https://partner.steamgames.com/doc/store/assets).
