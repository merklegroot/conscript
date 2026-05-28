import { mkdir } from "fs/promises";
import path from "path";

export const GENERATED_IMAGES_DIR = path.join(
  process.cwd(),
  "..",
  "generated_images",
);

export const CONSCRIPT_IMG_DIR = path.join(
  process.cwd(),
  "..",
  "Conscript",
  "img",
);

export async function ensureGeneratedImagesDir(): Promise<string> {
  await mkdir(GENERATED_IMAGES_DIR, { recursive: true });
  return GENERATED_IMAGES_DIR;
}
