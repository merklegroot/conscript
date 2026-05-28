#!/usr/bin/env bash
# Generate an image with xAI Grok Imagine and save to generated_images/.
#
# Requires: curl, jq, base64
# API key: set XAI_API_KEY or add it to image_gen/.env.local
#
# Examples:
#   ./scripts/generate_grok_image.sh -p "A rainy warehouse at night"
#   ./scripts/generate_grok_image.sh -f prompts/warehouse.txt --aspect-ratio 3:2
#   ./scripts/generate_grok_image.sh -p "..." --resolution 2k --json
#   IMAGE=$(./scripts/generate_grok_image.sh -p "...")

set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
ENV_FILE="${ROOT}/image_gen/.env.local"
OUTPUT_DIR="${ROOT}/generated_images"
GROK_ENDPOINT="https://api.x.ai/v1/images/generations"
DEFAULT_MODEL="grok-imagine-image-quality"

PROMPT=""
PROMPT_FILE=""
ASPECT_RATIO=""
RESOLUTION=""
MODEL="${DEFAULT_MODEL}"
PRINT_MODE="image"

usage() {
  cat <<'EOF'
Usage: generate_grok_image.sh [options]

Generate an image with Grok Imagine (xAI) and write it under generated_images/
alongside a JSON metadata file (same format as the image_gen web app).

Options:
  -p, --prompt TEXT         Prompt text (required unless -f or stdin is used)
  -f, --prompt-file FILE    Read prompt from a file
      --aspect-ratio RATIO  e.g. 16:9, 3:2, 1:1, auto
      --resolution RES      1k or 2k
      --model MODEL         Default: grok-imagine-image-quality
  -o, --output-dir DIR      Output directory (default: generated_images/)
      --json                Print one JSON object to stdout (id, paths, record)
      --print MODE          stdout: image | metadata | both | json (default: image)
  -h, --help                Show this help

Environment:
  XAI_API_KEY               xAI API key (or set in image_gen/.env.local)

Dependencies: curl, jq, base64
EOF
}

die() {
  echo "generate_grok_image.sh: $*" >&2
  exit 1
}

require_cmd() {
  command -v "$1" >/dev/null 2>&1 || die "missing required command: $1"
}

load_api_key() {
  if [[ -n "${XAI_API_KEY:-}" ]]; then
    return
  fi

  if [[ -f "${ENV_FILE}" ]]; then
  set -a
  # shellcheck source=/dev/null
  source "${ENV_FILE}"
  set +a
  fi

  if [[ -z "${XAI_API_KEY:-}" ]]; then
    die "XAI_API_KEY is not set. Add your key to ${ENV_FILE}"
  fi
}

parse_args() {
  while [[ $# -gt 0 ]]; do
    case "$1" in
      -p|--prompt)
        PROMPT="${2:-}"
        shift 2
        ;;
      -f|--prompt-file)
        PROMPT_FILE="${2:-}"
        shift 2
        ;;
      --aspect-ratio)
        ASPECT_RATIO="${2:-}"
        shift 2
        ;;
      --resolution)
        RESOLUTION="${2:-}"
        shift 2
        ;;
      --model)
        MODEL="${2:-}"
        shift 2
        ;;
      -o|--output-dir)
        OUTPUT_DIR="${2:-}"
        shift 2
        ;;
      --json)
        PRINT_MODE="json"
        shift
        ;;
      --print)
        PRINT_MODE="${2:-}"
        shift 2
        ;;
      -h|--help)
        usage
        exit 0
        ;;
      *)
        die "unknown argument: $1 (try --help)"
        ;;
    esac
  done
}

read_prompt_from_stdin() {
  python3 - <<'PY'
import select
import sys

if sys.stdin.isatty():
    sys.exit(1)

ready, _, _ = select.select([sys.stdin], [], [], 0)
if not ready:
    sys.exit(1)

data = sys.stdin.read()
if not data.strip():
    sys.exit(1)

print(data, end="")
PY
}

read_prompt() {
  if [[ -n "${PROMPT}" ]]; then
    return
  fi

  if [[ -n "${PROMPT_FILE}" ]]; then
    [[ -f "${PROMPT_FILE}" ]] || die "prompt file not found: ${PROMPT_FILE}"
    PROMPT="$(<"${PROMPT_FILE}")"
    return
  fi

  if PROMPT="$(read_prompt_from_stdin)"; then
    return
  fi

  die "prompt is required (-p, -f, or stdin)"
}

create_generation_id() {
  local timestamp suffix
  timestamp="$(date -u +%Y%m%d-%H%M%S)"
  suffix="$(openssl rand -hex 3)"
  printf '%s-%s' "${timestamp}" "${suffix}"
}

iso_timestamp() {
  python3 -c 'import datetime; print(datetime.datetime.now(datetime.UTC).isoformat(timespec="milliseconds").replace("+00:00", "Z"))'
}

extension_for_mime() {
  case "$1" in
    image/jpeg) printf '.jpg' ;;
    image/webp) printf '.webp' ;;
    *) printf '.png' ;;
  esac
}

build_request_json() {
  local trimmed_prompt
  trimmed_prompt="$(printf '%s' "${PROMPT}" | sed -e 's/^[[:space:]]*//' -e 's/[[:space:]]*$//')"
  [[ -n "${trimmed_prompt}" ]] || die "prompt is empty"

  jq -n \
    --arg model "${MODEL}" \
    --arg prompt "${trimmed_prompt}" \
    --arg aspect_ratio "${ASPECT_RATIO}" \
    --arg resolution "${RESOLUTION}" \
    '{
      model: $model,
      prompt: $prompt,
      response_format: "b64_json",
      n: 1
    }
    + (if $aspect_ratio != "" then {aspect_ratio: $aspect_ratio} else {} end)
    + (if $resolution != "" then {resolution: $resolution} else {} end)'
}

call_grok_api() {
  local request_json="$1"
  local response_file http_code response_body

  response_file="$(mktemp)"

  http_code="$(
    curl -sS -o "${response_file}" -w '%{http_code}' \
      -X POST "${GROK_ENDPOINT}" \
      -H "Authorization: Bearer ${XAI_API_KEY}" \
      -H "Content-Type: application/json" \
      -d "${request_json}"
  )"

  response_body="$(cat "${response_file}")"
  rm -f "${response_file}"

  if [[ "${http_code}" -lt 200 || "${http_code}" -ge 300 ]]; then
    die "xAI API error (${http_code}): ${response_body}"
  fi

  printf '%s' "${response_body}"
}

save_image_from_response() {
  local response_json="$1"
  local image_path mime b64 url

  b64="$(jq -r '.data[0].b64_json // empty' <<<"${response_json}")"
  url="$(jq -r '.data[0].url // empty' <<<"${response_json}")"
  mime="$(jq -r '.data[0].mime_type // "image/png"' <<<"${response_json}")"

  if [[ -z "${b64}" && -z "${url}" ]]; then
    die "xAI API returned no image data"
  fi

  if [[ -n "${b64}" ]]; then
    printf '%s' "${b64}" | base64 -d >"${image_path}"
  else
    curl -sS -o "${image_path}" "${url}"
    if [[ "${mime}" == "image/png" ]]; then
      detected="$(file -b --mime-type "${image_path}" 2>/dev/null || true)"
      if [[ -n "${detected}" ]]; then
        mime="${detected}"
      fi
    fi
  fi
}

write_metadata() {
  local id="$1"
  local created_at="$2"
  local image_file="$3"
  local metadata_file="$4"
  local request_json="$5"
  local response_json="$6"
  local metadata_path sanitized_response

  metadata_path="${OUTPUT_DIR}/${metadata_file}"
  sanitized_response="$(
    jq '.data |= map(
      if .b64_json then .b64_json = "[omitted — saved to image file]"
      else . end
    )' <<<"${response_json}"
  )"

  jq -n \
    --arg id "${id}" \
    --arg createdAt "${created_at}" \
    --arg imageFile "${image_file}" \
    --arg metadataFile "${metadata_file}" \
    --argjson request "${request_json}" \
    --argjson response "${sanitized_response}" \
    '{
      id: $id,
      createdAt: $createdAt,
      imageFile: $imageFile,
      metadataFile: $metadataFile,
      grok: {
        endpoint: "https://api.x.ai/v1/images/generations",
        request: $request,
        response: $response
      }
    }' >"${metadata_path}"
}

print_result() {
  local image_path="$1"
  local metadata_path="$2"
  local record_json="$3"

  case "${PRINT_MODE}" in
    image)
      printf '%s\n' "${image_path}"
      ;;
    metadata)
      printf '%s\n' "${metadata_path}"
      ;;
    both)
      printf '%s\n%s\n' "${image_path}" "${metadata_path}"
      ;;
    json)
      jq -n \
        --arg id "$(jq -r '.id' <<<"${record_json}")" \
        --arg imageFile "$(jq -r '.imageFile' <<<"${record_json}")" \
        --arg metadataFile "$(jq -r '.metadataFile' <<<"${record_json}")" \
        --arg imagePath "${image_path}" \
        --arg metadataPath "${metadata_path}" \
        --argjson record "${record_json}" \
        '{
          id: $id,
          imageFile: $imageFile,
          metadataFile: $metadataFile,
          imagePath: $imagePath,
          metadataPath: $metadataPath,
          record: $record
        }'
      ;;
    *)
      die "invalid --print mode: ${PRINT_MODE} (use image, metadata, both, or json)"
      ;;
  esac
}

main() {
  require_cmd curl
  require_cmd jq
  require_cmd base64
  require_cmd openssl
  require_cmd python3

  parse_args "$@"
  load_api_key
  read_prompt

  local request_json response_json id created_at extension image_file metadata_file
  local image_path metadata_path record_json

  mkdir -p "${OUTPUT_DIR}"

  request_json="$(build_request_json)"
  response_json="$(call_grok_api "${request_json}")"

  id="$(create_generation_id)"
  created_at="$(iso_timestamp)"
  extension="$(extension_for_mime "$(jq -r '.data[0].mime_type // "image/png"' <<<"${response_json}")")"
  image_file="${id}${extension}"
  metadata_file="${id}.json"
  image_path="${OUTPUT_DIR}/${image_file}"

  save_image_from_response "${response_json}"
  write_metadata "${id}" "${created_at}" "${image_file}" "${metadata_file}" \
    "${request_json}" "${response_json}"

  metadata_path="${OUTPUT_DIR}/${metadata_file}"
  record_json="$(<"${metadata_path}")"

  echo "Saved image: ${image_path}" >&2
  echo "Saved metadata: ${metadata_path}" >&2

  print_result "${image_path}" "${metadata_path}" "${record_json}"
}

main "$@"
