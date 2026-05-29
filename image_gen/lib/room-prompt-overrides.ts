import { mkdir, readFile, writeFile } from "fs/promises";
import path from "path";

const OVERRIDES_PATH = path.join(process.cwd(), "data", "room-prompt-overrides.json");

export async function getPromptOverrides(): Promise<Record<string, string>> {
  try {
    const content = await readFile(OVERRIDES_PATH, "utf-8");
    const parsed = JSON.parse(content) as unknown;

    if (typeof parsed !== "object" || parsed === null || Array.isArray(parsed)) {
      return {};
    }

    return Object.fromEntries(
      Object.entries(parsed).filter(
        (entry): entry is [string, string] => typeof entry[1] === "string",
      ),
    );
  } catch {
    return {};
  }
}

export async function getPromptOverride(phase: string): Promise<string | undefined> {
  const overrides = await getPromptOverrides();
  return overrides[phase];
}

export async function setPromptOverride(phase: string, prompt: string): Promise<void> {
  const overrides = await getPromptOverrides();
  overrides[phase] = prompt.trim();
  await mkdir(path.dirname(OVERRIDES_PATH), { recursive: true });
  await writeFile(OVERRIDES_PATH, `${JSON.stringify(overrides, null, 2)}\n`);
}

export async function clearPromptOverride(phase: string): Promise<void> {
  const overrides = await getPromptOverrides();

  if (!(phase in overrides)) {
    return;
  }

  delete overrides[phase];
  await mkdir(path.dirname(OVERRIDES_PATH), { recursive: true });
  await writeFile(OVERRIDES_PATH, `${JSON.stringify(overrides, null, 2)}\n`);
}
