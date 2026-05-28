import { NextResponse } from "next/server";
import type { AspectRatio, ImageResolution } from "@/lib/generation-types";
import { generateWithGrok } from "@/lib/grok";
import { saveGeneration } from "@/lib/save-generation";

type GenerateRequestBody = {
  prompt?: string;
  model?: string;
  aspect_ratio?: AspectRatio;
  resolution?: ImageResolution;
};

export async function POST(request: Request) {
  let body: GenerateRequestBody;

  try {
    body = (await request.json()) as GenerateRequestBody;
  } catch {
    return NextResponse.json(
      { error: "Request body must be valid JSON" },
      { status: 400 },
    );
  }

  const prompt = body.prompt?.trim();

  if (!prompt) {
    return NextResponse.json(
      { error: "prompt is required" },
      { status: 400 },
    );
  }

  try {
    const grokResult = await generateWithGrok({
      prompt,
      model: body.model,
      aspect_ratio: body.aspect_ratio,
      resolution: body.resolution,
    });

    const record = await saveGeneration(grokResult);

    return NextResponse.json({
      id: record.id,
      imageFile: record.imageFile,
      metadataFile: record.metadataFile,
      imageUrl: `/api/generated/${encodeURIComponent(record.imageFile)}`,
      record,
    });
  } catch (error) {
    const message =
      error instanceof Error ? error.message : "Image generation failed";

    return NextResponse.json({ error: message }, { status: 500 });
  }
}
