import { randomBytes } from "crypto";
import { writeFile } from "fs/promises";
import path from "path";
import { NextResponse } from "next/server";
import { installRoomImageFromPath } from "@/lib/install-room-from-file";
import { ensureGeneratedImagesDir, GENERATED_IMAGES_DIR } from "@/lib/paths";
import { getRoomByPhase } from "@/lib/game-rooms";

const ALLOWED_TYPES = new Set([
  "image/png",
  "image/jpeg",
  "image/webp",
]);

function extensionForMime(mime: string): string {
  switch (mime) {
    case "image/jpeg":
      return ".jpg";
    case "image/webp":
      return ".webp";
    default:
      return ".png";
  }
}

export async function POST(request: Request) {
  let formData: FormData;

  try {
    formData = await request.formData();
  } catch {
    return NextResponse.json(
      { error: "Expected multipart form data" },
      { status: 400 },
    );
  }

  const phase = String(formData.get("phase") ?? "").trim();
  const image = formData.get("image");

  if (!phase) {
    return NextResponse.json({ error: "phase is required" }, { status: 400 });
  }

  if (!getRoomByPhase(phase)) {
    return NextResponse.json({ error: "Unknown room" }, { status: 400 });
  }

  if (!(image instanceof File) || image.size === 0) {
    return NextResponse.json(
      { error: "image file is required" },
      { status: 400 },
    );
  }

  if (!ALLOWED_TYPES.has(image.type)) {
    return NextResponse.json(
      { error: `Unsupported image type: ${image.type || "unknown"}` },
      { status: 400 },
    );
  }

  await ensureGeneratedImagesDir();

  const id = `paste-${Date.now()}-${randomBytes(3).toString("hex")}`;
  const extension = extensionForMime(image.type);
  const archiveFile = `${id}${extension}`;
  const archivePath = path.join(GENERATED_IMAGES_DIR, archiveFile);

  const buffer = Buffer.from(await image.arrayBuffer());
  await writeFile(archivePath, buffer);

  try {
    const result = await installRoomImageFromPath(archivePath, phase);

    const metadata = {
      id,
      createdAt: new Date().toISOString(),
      imageFile: archiveFile,
      metadataFile: `${id}.json`,
      source: "clipboard-paste",
      room: {
        phase: result.room.phase,
        name: result.room.name,
        imageFile: result.room.imageFile,
      },
    };

    await writeFile(
      path.join(GENERATED_IMAGES_DIR, `${id}.json`),
      `${JSON.stringify(metadata, null, 2)}\n`,
      "utf8",
    );

    return NextResponse.json({
      ok: true,
      phase: result.room.phase,
      roomName: result.room.name,
      imageFile: result.room.imageFile,
      destPath: result.destPath,
      message: result.message,
      archivedAs: `generated_images/${archiveFile}`,
    });
  } catch (error) {
    const message =
      error instanceof Error ? error.message : "Failed to install image";

    return NextResponse.json({ error: message }, { status: 500 });
  }
}
