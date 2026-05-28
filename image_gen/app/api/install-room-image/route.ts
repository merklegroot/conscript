import { execFile as execFileCallback } from "node:child_process";
import path from "path";
import { promisify } from "node:util";
import { NextResponse } from "next/server";
import { getRoomByPhase } from "@/lib/game-rooms";
import { getGenerationById } from "@/lib/load-generations";
import {
  CONSCRIPT_IMG_DIR,
  GENERATED_IMAGES_DIR,
  INSTALL_SCENE_IMAGE_SCRIPT,
} from "@/lib/paths";

const execFile = promisify(execFileCallback);

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

  const room = getRoomByPhase(phase);
  const generation = await getGenerationById(generationId);

  if (!room) {
    return NextResponse.json({ error: "Unknown room" }, { status: 400 });
  }

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
  const destPath = path.join(CONSCRIPT_IMG_DIR, room.imageFile);

  try {
    const { stdout, stderr } = await execFile(
      "python3",
      [INSTALL_SCENE_IMAGE_SCRIPT, sourcePath, destPath],
      { encoding: "utf8" },
    );

    return NextResponse.json({
      ok: true,
      phase: room.phase,
      roomName: room.name,
      imageFile: room.imageFile,
      destPath: `Conscript/img/${room.imageFile}`,
      message: (stdout || stderr).trim(),
    });
  } catch (error) {
    const message =
      error instanceof Error ? error.message : "Failed to install image";

    return NextResponse.json({ error: message }, { status: 500 });
  }
}
