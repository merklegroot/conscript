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

export const REPO_ROOT = path.join(process.cwd(), "..");

export const INSTALL_SCENE_IMAGE_SCRIPT = path.join(
  REPO_ROOT,
  "scripts",
  "install_scene_image.py",
);

export async function ensureGeneratedImagesDir(): Promise<string> {
  await mkdir(GENERATED_IMAGES_DIR, { recursive: true });
  return GENERATED_IMAGES_DIR;
}
