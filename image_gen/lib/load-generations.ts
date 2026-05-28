import { access, readdir, readFile } from "fs/promises";
import path from "path";
import type { GenerationRecord } from "./generation-types";
import { GENERATED_IMAGES_DIR } from "./paths";

export function generatedImageUrl(imageFile: string): string {
  return `/api/generated/${encodeURIComponent(imageFile)}`;
}

export function generatedDetailHref(id: string): string {
  return `/generated/${encodeURIComponent(id)}`;
}

async function fileExists(filePath: string): Promise<boolean> {
  try {
    await access(filePath);
    return true;
  } catch {
    return false;
  }
}

export type LoadedGeneration = {
  record: GenerationRecord;
  imageExists: boolean;
};

export async function loadGenerations(): Promise<LoadedGeneration[]> {
  let entries: string[];

  try {
    entries = await readdir(GENERATED_IMAGES_DIR);
  } catch {
    return [];
  }

  const jsonFiles = entries.filter((name) => name.endsWith(".json"));
  const loaded: LoadedGeneration[] = [];

  for (const metadataFile of jsonFiles) {
    const filePath = path.join(GENERATED_IMAGES_DIR, metadataFile);

    let raw: string;

    try {
      raw = await readFile(filePath, "utf8");
    } catch {
      continue;
    }

    let record: GenerationRecord;

    try {
      record = JSON.parse(raw) as GenerationRecord;
    } catch {
      continue;
    }

    if (!record.id || !record.imageFile) {
      continue;
    }

    const imageExists = await fileExists(
      path.join(GENERATED_IMAGES_DIR, record.imageFile),
    );

    loaded.push({ record, imageExists });
  }

  loaded.sort(
    (a, b) =>
      new Date(b.record.createdAt).getTime() -
      new Date(a.record.createdAt).getTime(),
  );

  return loaded;
}

export async function getGenerationById(
  id: string,
): Promise<LoadedGeneration | undefined> {
  const metadataPath = path.join(GENERATED_IMAGES_DIR, `${id}.json`);

  let raw: string;

  try {
    raw = await readFile(metadataPath, "utf8");
  } catch {
    const all = await loadGenerations();
    return all.find((item) => item.record.id === id);
  }

  let record: GenerationRecord;

  try {
    record = JSON.parse(raw) as GenerationRecord;
  } catch {
    return undefined;
  }

  if (!record.id || !record.imageFile) {
    return undefined;
  }

  const imageExists = await fileExists(
    path.join(GENERATED_IMAGES_DIR, record.imageFile),
  );

  return { record, imageExists };
}
