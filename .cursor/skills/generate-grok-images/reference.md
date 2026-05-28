# Grok image generation — reference

## Script

`scripts/generate_grok_image.sh` — calls `POST https://api.x.ai/v1/images/generations`.

| Flag | Purpose |
|------|---------|
| `-p`, `--prompt TEXT` | Prompt string |
| `-f`, `--prompt-file FILE` | Read prompt from file |
| `--aspect-ratio RATIO` | `16:9`, `3:2`, `4:3`, `1:1`, `9:16`, `auto`, … |
| `--resolution` | `1k` or `2k` |
| `--model` | Default `grok-imagine-image-quality` |
| `-o`, `--output-dir` | Default `generated_images/` at repo root |
| `--json` | Print `{ id, imagePath, metadataPath, record }` on stdout |
| `--print image\|metadata\|both\|json` | Control stdout (default: image path) |

Progress messages go to **stderr**; stdout is the path (or JSON) for piping.

## Metadata JSON

Each run writes `generated_images/<id>.json`:

```json
{
  "id": "20260528-161231-d34e8d",
  "imageFile": "20260528-161231-d34e8d.jpg",
  "metadataFile": "20260528-161231-d34e8d.json",
  "grok": {
    "endpoint": "https://api.x.ai/v1/images/generations",
    "request": { "model": "...", "prompt": "...", "response_format": "b64_json", "n": 1 },
    "response": { "data": [...], "usage": { "cost_in_usd_ticks": ... } }
  }
}
```

Use this file to recover the exact prompt and API parameters after generation.

## image_gen web app

| Path | Role |
|------|------|
| `image_gen/` | Next.js UI |
| `image_gen/.env.local` | `XAI_API_KEY` |
| `image_gen/app/api/generate` | Same Grok call + save logic as shell |
| `generated_images/` | Shared output directory (repo root) |

## Example: warehouse ambush draft

```bash
./scripts/generate_grok_image.sh \
  -p "Cinematic photorealistic nighttime scene at a Soviet-era warehouse loading bay in Ulan-Ude, Republic of Buryatia, early autumn, heavy rain. Ground-level view on wet concrete near an old delivery truck, half-open roll-up door with warm industrial light, floodlights through rain, ominous silhouettes in deep shadow (no clear faces). Moody post-Soviet atmosphere, wide landscape 3:2. No readable text, no logos." \
  --aspect-ratio 3:2

python3 scripts/install_scene_image.py \
  generated_images/20260528-161231-d34e8d.jpg \
  Conscript/img/warehouse-14-ambush.png
```

## Example: batch / CI

```bash
RESULT=$(./scripts/generate_grok_image.sh -f prompts/warehouse.txt --json)
IMAGE=$(echo "$RESULT" | jq -r .imagePath)
```
