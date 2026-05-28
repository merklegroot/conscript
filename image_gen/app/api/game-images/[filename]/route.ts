import { readFile, stat } from "fs/promises";
import path from "path";
import { NextResponse } from "next/server";
import { GAME_ROOMS } from "@/lib/game-rooms";
import { CONSCRIPT_IMG_DIR } from "@/lib/paths";

export const dynamic = "force-dynamic";

const ALLOWED_FILES = new Set(GAME_ROOMS.map((room) => room.imageFile));

type RouteContext = {
  params: Promise<{ filename: string }>;
};

export async function GET(_request: Request, context: RouteContext) {
  const { filename } = await context.params;
  const decodedFilename = decodeURIComponent(filename);

  if (!ALLOWED_FILES.has(decodedFilename)) {
    return NextResponse.json({ error: "Invalid filename" }, { status: 400 });
  }

  const filePath = path.join(CONSCRIPT_IMG_DIR, decodedFilename);

  try {
    const [file, fileStat] = await Promise.all([
      readFile(filePath),
      stat(filePath),
    ]);

    return new NextResponse(file, {
      headers: {
        "Content-Type": "image/png",
        "Cache-Control": "no-store, must-revalidate",
        ETag: `"${fileStat.mtimeMs}"`,
      },
    });
  } catch {
    return NextResponse.json({ error: "File not found" }, { status: 404 });
  }
}
