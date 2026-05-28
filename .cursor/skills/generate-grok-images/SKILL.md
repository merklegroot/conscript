---
name: generate-grok-images
description: >-
  Generate images with xAI Grok Imagine via scripts/generate_grok_image.sh or
  the image_gen Next.js app. Saves PNG/JPG + JSON metadata under
  generated_images/. Use when the user mentions Grok, xAI, grok imagine, grok
  image, generate_grok_image, image_gen, or wants automated/scripted image
  generation instead of the Cursor GenerateImage tool.
---

# Generate Images with Grok (xAI)

## Critical rule

**Use Grok — do not use the Cursor `GenerateImage` tool** for requests covered by this skill.

Run **`scripts/generate_grok_image.sh`** (preferred for agents and automation) or guide the user to **`image_gen/`** (`npm run dev`) for a browser UI.

API key: `image_gen/.env.local` → `XAI_API_KEY` (script loads it automatically).

## When to use this skill

| User intent | Skill |
|-------------|--------|
| Grok / xAI / `generate_grok_image.sh` / `image_gen` / scripted batch | **This skill** |
| Cursor `GenerateImage` only, no Grok | [generate-images](../generate-images/SKILL.md) |
| GeoPandas map | [generate-maps](../generate-maps/SKILL.md) |

**Trigger phrases:** “use grok”, “grok image”, “xAI image”, “generate with grok”, “automate image gen”, `@generate-grok-images`.

## Workflow checklist

```
- [ ] 1. Identify target (game asset path, draft, or user-only preview)
- [ ] 2. Read scene narrative in Game.cs if Conscript background
- [ ] 3. Read 1–2 reference PNGs in Conscript/img/ for style (if game art)
- [ ] 4. Compose prompt (reuse style from generate-images/reference.md)
- [ ] 5. Run scripts/generate_grok_image.sh (see below)
- [ ] 6. Read output image + generated_images/*.json metadata
- [ ] 7. If game asset: install_scene_image.py → Conscript/img/<name>.png
- [ ] 8. dotnet build conscript.slnx if embedded texture changed
- [ ] 9. Log in Conscript/img/image-prompts.md (tool: Grok)
```

## Run the generator (default)

From repo root:

```bash
./scripts/generate_grok_image.sh \
  -p "YOUR FULL PROMPT HERE" \
  --aspect-ratio 3:2
```

**Prompt file** (long prompts):

```bash
./scripts/generate_grok_image.sh -f /path/to/prompt.txt --aspect-ratio 3:2
```

**Machine-readable output** (paths + full record):

```bash
./scripts/generate_grok_image.sh -p "..." --aspect-ratio 3:2 --json
```

**Capture image path in a script:**

```bash
IMAGE=$(./scripts/generate_grok_image.sh -p "...")
```

Outputs (always):

- `generated_images/<id>.{png|jpg|webp}` — image
- `generated_images/<id>.json` — prompt, request payload, usage (same schema as `image_gen` web app)

Default model: `grok-imagine-image-quality`. Optional: `--resolution 2k`, `--model <name>`.

## Conscript scene backgrounds

Same photorealistic post-Soviet Ulan-Ude style as [generate-images](../generate-images/SKILL.md). Prompt skeleton:

```text
Cinematic photorealistic [scene], post-Soviet Siberian town (Ulan-Ude), early autumn.
[Objects and mood from Game.cs narrative.]
Wide landscape 3:2, no people, no readable text or logos.
```

Use **`--aspect-ratio 3:2`** for scene art. After generation:

```bash
python3 scripts/install_scene_image.py \
  generated_images/<id>.jpg \
  Conscript/img/<target>.png
```

(`install_scene_image.py` accepts jpg/png; outputs 1536×1024 PNG.)

## Log prompts

Append to `Conscript/img/image-prompts.md`:

- **tool**: `Grok` (`scripts/generate_grok_image.sh` or `image_gen`)
- **model**: `grok-imagine-image-quality` (or value from JSON metadata)
- **prompt**: exact string passed to `-p` / `-f`
- **notes**: `generated_images/<id>.*`, install path, `Game.cs` phase if relevant

## Web UI (optional)

If the user prefers clicking over CLI:

```bash
cd image_gen && npm run dev
```

Open http://localhost:3000 — same API, same `generated_images/` output.

## Troubleshooting

| Issue | Fix |
|-------|-----|
| `XAI_API_KEY is not set` | Add key to `image_gen/.env.local` |
| User asked for Cursor GenerateImage | Use [generate-images](../generate-images/SKILL.md) instead |
| Wrong district / time of day | Regenerate with tighter prompt; read narrative in `Game.cs` |
| Stale texture in game | `dotnet build` after installing to `Conscript/img/` |
| `jq` / `curl` missing | Install via Homebrew; script requires curl, jq, base64, openssl, python3 |

## Additional resources

- Script flags and examples: [reference.md](reference.md)
- Scene inventory, style palette, example prompts: [generate-images/reference.md](../generate-images/reference.md)
