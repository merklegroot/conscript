import path from "path";
import { NextResponse } from "next/server";
import { getGenerationById } from "@/lib/load-generations";
import { installRoomImageFromPath } from "@/lib/install-room-from-file";
import { GENERATED_IMAGES_DIR } from "@/lib/paths";

type InstallRequestBody = {
  generationId?: string;
  phase?: string;
};

export async function POST(request: Request) {
  let body: InstallRequestBody;

  try {
    body = (await request.json()) as InstallRequestBody;
  } catch {
    return NextResponse.json(
      { error: "Request body must be valid JSON" },
      { status: 400 },
    );
  }

  const generationId = body.generationId?.trim();
  const phase = body.phase?.trim();

  if (!generationId || !phase) {
    return NextResponse.json(
      { error: "generationId and phase are required" },
      { status: 400 },
    );
  }

  const generation = await getGenerationById(generationId);

  if (!generation) {
    return NextResponse.json(
      { error: "Generation not found" },
      { status: 404 },
    );
  }

  if (!generation.imageExists) {
    return NextResponse.json(
      { error: "Generation image file is missing" },
      { status: 400 },
    );
  }

  const sourcePath = path.join(GENERATED_IMAGES_DIR, generation.record.imageFile);

  try {
    const result = await installRoomImageFromPath(sourcePath, phase);

    return NextResponse.json({
      ok: true,
      phase: result.room.phase,
      roomName: result.room.name,
      imageFile: result.room.imageFile,
      destPath: result.destPath,
      message: result.message,
    });
  } catch (error) {
    const message =
      error instanceof Error ? error.message : "Failed to install image";

    return NextResponse.json({ error: message }, { status: 500 });
  }
}
