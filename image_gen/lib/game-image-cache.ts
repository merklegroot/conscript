import { stat } from "fs/promises";
import path from "path";
import { CONSCRIPT_IMG_DIR } from "./paths";

export function gameImageUrl(imageFile: string, version?: string): string {
  const base = `/api/game-images/${encodeURIComponent(imageFile)}`;
  return version ? `${base}?v=${encodeURIComponent(version)}` : base;
}

export async function getGameImageVersion(
  imageFile: string,
): Promise<string | undefined> {
  try {
    const fileStat = await stat(path.join(CONSCRIPT_IMG_DIR, imageFile));
    return String(Math.floor(fileStat.mtimeMs));
  } catch {
    return undefined;
  }
}
