import { readFile } from "fs/promises";
import path from "path";
import { NextResponse } from "next/server";
import { GENERATED_IMAGES_DIR } from "@/lib/paths";

const SAFE_FILENAME = /^[a-zA-Z0-9._-]+\.(png|jpe?g|webp|json)$/;

type RouteContext = {
  params: Promise<{ filename: string }>;
};

function contentTypeForFilename(filename: string): string {
  if (filename.endsWith(".json")) {
    return "application/json";
  }

  if (filename.endsWith(".webp")) {
    return "image/webp";
  }

  if (filename.endsWith(".jpg") || filename.endsWith(".jpeg")) {
    return "image/jpeg";
  }

  return "image/png";
}

export async function GET(_request: Request, context: RouteContext) {
  const { filename } = await context.params;
  const decodedFilename = decodeURIComponent(filename);

  if (!SAFE_FILENAME.test(decodedFilename)) {
    return NextResponse.json({ error: "Invalid filename" }, { status: 400 });
  }

  const filePath = path.join(GENERATED_IMAGES_DIR, decodedFilename);

  try {
    const file = await readFile(filePath);

    return new NextResponse(file, {
      headers: {
        "Content-Type": contentTypeForFilename(decodedFilename),
        "Cache-Control": "public, max-age=31536000, immutable",
      },
    });
  } catch {
    return NextResponse.json({ error: "File not found" }, { status: 404 });
  }
}
