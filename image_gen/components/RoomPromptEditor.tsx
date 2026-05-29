"use client";

import { useRouter } from "next/navigation";
import { useState } from "react";
import CopyPromptButton from "@/components/CopyPromptButton";
import type { PromptSource } from "@/lib/room-prompts";

type RoomPromptEditorProps = {
  phase: string;
  defaultPrompt: string;
  defaultPromptSource: PromptSource;
  initialPrompt: string;
  promptSource: PromptSource;
  imageFile: string;
};

function promptSourceLabel(source: PromptSource): string {
  switch (source) {
    case "verified":
      return "Verified — matches a logged generation in image-prompts.md";
    case "custom":
      return "Custom — saved override for this room";
    case "inferred":
      return "Inferred from Game.cs narrative and project style guide — tweak before regenerating";
  }
}

export function roomPromptSessionKey(phase: string): string {
  return `room-prompt-${phase}`;
}

export default function RoomPromptEditor({
  phase,
  defaultPrompt,
  defaultPromptSource,
  initialPrompt,
  promptSource,
  imageFile,
}: RoomPromptEditorProps) {
  const router = useRouter();
  const [prompt, setPrompt] = useState(initialPrompt);
  const [savedPrompt, setSavedPrompt] = useState(initialPrompt);
  const [source, setSource] = useState(promptSource);
  const [isSaving, setIsSaving] = useState(false);
  const [isResetting, setIsResetting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);

  const isDirty = prompt.trim() !== savedPrompt.trim();
  const canReset = source === "custom" || prompt.trim() !== defaultPrompt.trim();

  async function handleSave() {
    const trimmed = prompt.trim();

    if (!trimmed) {
      setError("Prompt cannot be empty");
      return;
    }

    setError(null);
    setSuccess(null);
    setIsSaving(true);

    try {
      const response = await fetch("/api/room-prompt", {
        method: "PUT",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ phase, prompt: trimmed }),
      });

      const data = (await response.json()) as { error?: string };

      if (!response.ok) {
        throw new Error(data.error ?? "Failed to save prompt");
      }

      setPrompt(trimmed);
      setSavedPrompt(trimmed);
      setSource("custom");
      setSuccess("Prompt saved.");
      router.refresh();
    } catch (saveError) {
      setError(
        saveError instanceof Error ? saveError.message : "Failed to save prompt",
      );
    } finally {
      setIsSaving(false);
    }
  }

  async function handleReset() {
    setError(null);
    setSuccess(null);
    setIsResetting(true);

    try {
      const response = await fetch("/api/room-prompt", {
        method: "DELETE",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ phase }),
      });

      const data = (await response.json()) as { error?: string };

      if (!response.ok) {
        throw new Error(data.error ?? "Failed to reset prompt");
      }

      setPrompt(defaultPrompt);
      setSavedPrompt(defaultPrompt);
      setSource(defaultPromptSource);
      setSuccess("Reset to default prompt.");
      router.refresh();
    } catch (resetError) {
      setError(
        resetError instanceof Error ? resetError.message : "Failed to reset prompt",
      );
    } finally {
      setIsResetting(false);
    }
  }

  function handleGenerate() {
    sessionStorage.setItem(roomPromptSessionKey(phase), prompt.trim());
    router.push(`/?room=${encodeURIComponent(phase)}`);
  }

  return (
    <section className="space-y-4">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <h2 className="text-lg font-semibold">Regeneration prompt</h2>
        <div className="flex flex-wrap gap-2">
          <CopyPromptButton text={prompt} />
          <button
            type="button"
            onClick={handleSave}
            disabled={isSaving || !isDirty || !prompt.trim()}
            className="rounded-lg border border-zinc-300 px-3 py-1.5 text-sm font-medium text-zinc-800 transition hover:bg-zinc-50 disabled:cursor-not-allowed disabled:opacity-50 dark:border-zinc-700 dark:text-zinc-200 dark:hover:bg-zinc-900"
          >
            {isSaving ? "Saving…" : "Save"}
          </button>
          {canReset ? (
            <button
              type="button"
              onClick={handleReset}
              disabled={isResetting}
              className="rounded-lg border border-zinc-300 px-3 py-1.5 text-sm font-medium text-zinc-800 transition hover:bg-zinc-50 disabled:cursor-not-allowed disabled:opacity-50 dark:border-zinc-700 dark:text-zinc-200 dark:hover:bg-zinc-900"
            >
              {isResetting ? "Resetting…" : "Reset to default"}
            </button>
          ) : null}
          <button
            type="button"
            onClick={handleGenerate}
            disabled={!prompt.trim()}
            className="rounded-lg bg-zinc-900 px-3 py-1.5 text-sm font-medium text-white transition hover:bg-zinc-700 disabled:cursor-not-allowed disabled:opacity-50 dark:bg-zinc-100 dark:text-zinc-900 dark:hover:bg-zinc-300"
          >
            Generate with Grok
          </button>
        </div>
      </div>

      <p className="text-xs text-zinc-500 dark:text-zinc-400">
        {promptSourceLabel(source)}
        {isDirty ? " · Unsaved changes" : null}
      </p>

      <textarea
        value={prompt}
        onChange={(event) => {
          setPrompt(event.target.value);
          setSuccess(null);
        }}
        rows={10}
        spellCheck={false}
        className="w-full overflow-x-auto rounded-lg border border-zinc-200 bg-zinc-50 px-4 py-3 text-sm leading-relaxed text-zinc-800 outline-none focus:border-zinc-400 focus:ring-2 focus:ring-zinc-200 dark:border-zinc-800 dark:bg-zinc-950 dark:text-zinc-200 dark:focus:border-zinc-600 dark:focus:ring-zinc-800"
      />

      {error ? (
        <p className="rounded-lg border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-800 dark:border-red-900 dark:bg-red-950 dark:text-red-200">
          {error}
        </p>
      ) : null}

      {success ? (
        <p className="rounded-lg border border-green-200 bg-green-50 px-3 py-2 text-sm text-green-900 dark:border-green-900 dark:bg-green-950 dark:text-green-100">
          {success}
        </p>
      ) : null}

      <p className="text-xs text-zinc-500 dark:text-zinc-400">
        Target asset:{" "}
        <code className="font-mono">Conscript/img/{imageFile}</code>
        {" · "}
        <code className="font-mono">
          python3 scripts/install_scene_image.py generated_images/&lt;file&gt; Conscript/img/
          {imageFile}
        </code>
      </p>
    </section>
  );
}
