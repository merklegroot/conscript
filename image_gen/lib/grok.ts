import type {
  AspectRatio,
  GrokImageGenerationRequest,
  GrokImageGenerationResponse,
  ImageResolution,
} from "./generation-types";

export const GROK_IMAGE_ENDPOINT =
  "https://api.x.ai/v1/images/generations";

export const DEFAULT_GROK_IMAGE_MODEL = "grok-imagine-image-quality";

export type GenerateWithGrokInput = {
  prompt: string;
  model?: string;
  aspect_ratio?: AspectRatio;
  resolution?: ImageResolution;
};

export type GenerateWithGrokResult = {
  request: GrokImageGenerationRequest;
  response: GrokImageGenerationResponse;
  imageBuffer: Buffer;
  mimeType: string;
};

function extensionForMimeType(mimeType: string): string {
  switch (mimeType) {
    case "image/jpeg":
      return ".jpg";
    case "image/webp":
      return ".webp";
    case "image/png":
    default:
      return ".png";
  }
}

export function fileExtensionForMimeType(mimeType: string): string {
  return extensionForMimeType(mimeType);
}

function buildRequestBody(
  input: GenerateWithGrokInput,
): GrokImageGenerationRequest {
  const request: GrokImageGenerationRequest = {
    model: input.model ?? DEFAULT_GROK_IMAGE_MODEL,
    prompt: input.prompt.trim(),
    response_format: "b64_json",
    n: 1,
  };

  if (input.aspect_ratio) {
    request.aspect_ratio = input.aspect_ratio;
  }

  if (input.resolution) {
    request.resolution = input.resolution;
  }

  return request;
}

async function downloadImageFromUrl(url: string): Promise<{
  buffer: Buffer;
  mimeType: string;
}> {
  const response = await fetch(url);

  if (!response.ok) {
    throw new Error(`Failed to download image from xAI URL (${response.status})`);
  }

  const mimeType = response.headers.get("content-type") ?? "image/png";
  const buffer = Buffer.from(await response.arrayBuffer());

  return { buffer, mimeType };
}

export async function generateWithGrok(
  input: GenerateWithGrokInput,
): Promise<GenerateWithGrokResult> {
  const apiKey = process.env.XAI_API_KEY?.trim();

  if (!apiKey) {
    throw new Error(
      "XAI_API_KEY is not set. Add your xAI API key to image_gen/.env.local",
    );
  }

  const request = buildRequestBody(input);

  const apiResponse = await fetch(GROK_IMAGE_ENDPOINT, {
    method: "POST",
    headers: {
      Authorization: `Bearer ${apiKey}`,
      "Content-Type": "application/json",
    },
    body: JSON.stringify(request),
  });

  const responseText = await apiResponse.text();
  let responseJson: GrokImageGenerationResponse;

  try {
    responseJson = JSON.parse(responseText) as GrokImageGenerationResponse;
  } catch {
    throw new Error(
      `xAI API returned invalid JSON (${apiResponse.status}): ${responseText}`,
    );
  }

  if (!apiResponse.ok) {
    throw new Error(
      `xAI API error (${apiResponse.status}): ${responseText}`,
    );
  }

  const firstImage = responseJson.data?.[0];

  if (!firstImage) {
    throw new Error("xAI API returned no image data");
  }

  let imageBuffer: Buffer;
  let mimeType = firstImage.mime_type ?? "image/png";

  if (firstImage.b64_json) {
    imageBuffer = Buffer.from(firstImage.b64_json, "base64");
  } else if (firstImage.url) {
    const downloaded = await downloadImageFromUrl(firstImage.url);
    imageBuffer = downloaded.buffer;
    mimeType = downloaded.mimeType;
  } else {
    throw new Error("xAI API returned neither b64_json nor url for the image");
  }

  return {
    request,
    response: responseJson,
    imageBuffer,
    mimeType,
  };
}
