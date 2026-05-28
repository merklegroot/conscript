"use client";

import { useState } from "react";
import type { AspectRatio, GenerationRecord, ImageResolution } from "@/lib/generation-types";

type GenerateSuccess = {
  id: string;
  imageFile: string;
  metadataFile: string;
  imageUrl: string;
  record: GenerationRecord;
};

const ASPECT_RATIOS: { value: AspectRatio | ""; label: string }[] = [
  { value: "", label: "Default" },
  { value: "16:9", label: "16:9" },
  { value: "3:2", label: "3:2" },
  { value: "4:3", label: "4:3" },
  { value: "1:1", label: "1:1" },
  { value: "9:16", label: "9:16" },
  { value: "auto", label: "Auto" },
];

type ImageGeneratorProps = {
  initialPrompt?: string;
  initialAspectRatio?: AspectRatio;
};

export default function ImageGenerator({
  initialPrompt = "",
  initialAspectRatio,
}: ImageGeneratorProps) {
  const [prompt, setPrompt] = useState(initialPrompt);
  const [aspectRatio, setAspectRatio] = useState<AspectRatio | "">(
    initialAspectRatio ?? "",
  );
  const [resolution, setResolution] = useState<ImageResolution | "">("");
  const [isGenerating, setIsGenerating] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [result, setResult] = useState<GenerateSuccess | null>(null);

  async function handleSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError(null);
    setIsGenerating(true);

    try {
      const response = await fetch("/api/generate", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          prompt,
          ...(aspectRatio ? { aspect_ratio: aspectRatio } : {}),
          ...(resolution ? { resolution } : {}),
        }),
      });

      const data = (await response.json()) as GenerateSuccess & {
        error?: string;
      };

      if (!response.ok) {
        throw new Error(data.error ?? "Image generation failed");
      }

      setResult(data);
    } catch (submitError) {
      setResult(null);
      setError(
        submitError instanceof Error
          ? submitError.message
          : "Image generation failed",
      );
    } finally {
      setIsGenerating(false);
    }
  }

  return (
    <div className="space-y-8">
      <form onSubmit={handleSubmit} className="space-y-4">
        <div>
          <label
            htmlFor="prompt"
            className="mb-2 block text-sm font-medium text-zinc-900 dark:text-zinc-100"
          >
            Prompt
          </label>
          <textarea
            id="prompt"
            name="prompt"
            rows={6}
            required
            value={prompt}
            onChange={(event) => setPrompt(event.target.value)}
            placeholder="Describe the image you want Grok to generate..."
            className="w-full rounded-lg border border-zinc-300 bg-white px-3 py-2 text-sm text-zinc-900 shadow-sm outline-none focus:border-zinc-500 focus:ring-2 focus:ring-zinc-200 dark:border-zinc-700 dark:bg-zinc-950 dark:text-zinc-100 dark:focus:border-zinc-500 dark:focus:ring-zinc-800"
          />
        </div>

        <div className="grid gap-4 sm:grid-cols-2">
          <div>
            <label
              htmlFor="aspect_ratio"
              className="mb-2 block text-sm font-medium text-zinc-900 dark:text-zinc-100"
            >
              Aspect ratio
            </label>
            <select
              id="aspect_ratio"
              value={aspectRatio}
              onChange={(event) =>
                setAspectRatio(event.target.value as AspectRatio | "")
              }
              className="w-full rounded-lg border border-zinc-300 bg-white px-3 py-2 text-sm text-zinc-900 dark:border-zinc-700 dark:bg-zinc-950 dark:text-zinc-100"
            >
              {ASPECT_RATIOS.map((option) => (
                <option key={option.label} value={option.value}>
                  {option.label}
                </option>
              ))}
            </select>
          </div>

          <div>
            <label
              htmlFor="resolution"
              className="mb-2 block text-sm font-medium text-zinc-900 dark:text-zinc-100"
            >
              Resolution
            </label>
            <select
              id="resolution"
              value={resolution}
              onChange={(event) =>
                setResolution(event.target.value as ImageResolution | "")
              }
              className="w-full rounded-lg border border-zinc-300 bg-white px-3 py-2 text-sm text-zinc-900 dark:border-zinc-700 dark:bg-zinc-950 dark:text-zinc-100"
            >
              <option value="">Default</option>
              <option value="1k">1k</option>
              <option value="2k">2k</option>
            </select>
          </div>
        </div>

        <button
          type="submit"
          disabled={isGenerating || !prompt.trim()}
          className="rounded-lg bg-zinc-900 px-4 py-2 text-sm font-medium text-white transition hover:bg-zinc-700 disabled:cursor-not-allowed disabled:opacity-50 dark:bg-zinc-100 dark:text-zinc-900 dark:hover:bg-zinc-300"
        >
          {isGenerating ? "Generating…" : "Generate image"}
        </button>
      </form>

      {error ? (
        <p className="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-800 dark:border-red-900 dark:bg-red-950 dark:text-red-200">
          {error}
        </p>
      ) : null}

      {result ? (
        <section className="space-y-4">
          <div>
            <h2 className="text-lg font-semibold">Generated image</h2>
            <p className="mt-1 text-sm text-zinc-600 dark:text-zinc-400">
              Saved to{" "}
              <code className="rounded bg-zinc-100 px-1 py-0.5 text-xs dark:bg-zinc-900">
                generated_images/{result.imageFile}
              </code>{" "}
              with metadata in{" "}
              <code className="rounded bg-zinc-100 px-1 py-0.5 text-xs dark:bg-zinc-900">
                generated_images/{result.metadataFile}
              </code>
            </p>
          </div>

          {/* eslint-disable-next-line @next/next/no-img-element */}
          <img
            src={result.imageUrl}
            alt={result.record.grok.request.prompt}
            className="max-w-full rounded-lg border border-zinc-200 shadow-sm dark:border-zinc-800"
          />

          <details className="rounded-lg border border-zinc-200 bg-zinc-50 p-4 dark:border-zinc-800 dark:bg-zinc-950">
            <summary className="cursor-pointer text-sm font-medium">
              Generation metadata
            </summary>
            <pre className="mt-3 overflow-x-auto text-xs text-zinc-700 dark:text-zinc-300">
              {JSON.stringify(result.record, null, 2)}
            </pre>
          </details>
        </section>
      ) : null}
    </div>
  );
}
