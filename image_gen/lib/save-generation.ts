import { mkdir, writeFile } from "fs/promises";
import path from "path";
import { randomBytes } from "crypto";
import type { GenerateWithGrokResult } from "./grok";
import { fileExtensionForMimeType } from "./grok";
import type { GenerationRecord } from "./generation-types";
import { GENERATED_IMAGES_DIR } from "./paths";

function createGenerationId(): string {
  const timestamp = new Date()
    .toISOString()
    .replace(/[-:]/g, "")
    .replace(/\..+/, "")
    .replace("T", "-");

  const suffix = randomBytes(3).toString("hex");

  return `${timestamp}-${suffix}`;
}

function sanitizeResponseForMetadata(
  result: GenerateWithGrokResult,
): GenerationRecord["grok"]["response"] {
  return {
    data: result.response.data.map((item) => ({
      mime_type: item.mime_type,
      url: item.url,
      b64_json: item.b64_json ? "[omitted — saved to image file]" : null,
    })),
    usage: result.response.usage,
  };
}

export async function saveGeneration(
  result: GenerateWithGrokResult,
): Promise<GenerationRecord> {
  await mkdir(GENERATED_IMAGES_DIR, { recursive: true });

  const id = createGenerationId();
  const extension = fileExtensionForMimeType(result.mimeType);
  const imageFile = `${id}${extension}`;
  const metadataFile = `${id}.json`;
  const createdAt = new Date().toISOString();

  const record: GenerationRecord = {
    id,
    createdAt,
    imageFile,
    metadataFile,
    grok: {
      endpoint: "https://api.x.ai/v1/images/generations",
      request: result.request,
      response: sanitizeResponseForMetadata(result),
    },
  };

  await writeFile(
    path.join(GENERATED_IMAGES_DIR, imageFile),
    result.imageBuffer,
  );

  await writeFile(
    path.join(GENERATED_IMAGES_DIR, metadataFile),
    `${JSON.stringify(record, null, 2)}\n`,
    "utf8",
  );

  return record;
}
