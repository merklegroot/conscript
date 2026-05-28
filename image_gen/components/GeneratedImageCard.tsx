import Link from "next/link";
import type { GenerationRecord } from "@/lib/generation-types";
import {
  generatedDetailHref,
  generatedImageUrl,
} from "@/lib/load-generations";

type GeneratedImageCardProps = {
  record: GenerationRecord;
  imageExists: boolean;
};

function formatDate(iso: string): string {
  return new Date(iso).toLocaleString(undefined, {
    dateStyle: "medium",
    timeStyle: "short",
  });
}

export default function GeneratedImageCard({
  record,
  imageExists,
}: GeneratedImageCardProps) {
  const prompt = record.grok.request.prompt;

  return (
    <Link
      href={generatedDetailHref(record.id)}
      className="block overflow-hidden rounded-lg border border-zinc-200 bg-white transition hover:border-zinc-400 dark:border-zinc-800 dark:bg-zinc-950 dark:hover:border-zinc-600"
    >
      <div className="aspect-[3/2] bg-zinc-100 dark:bg-zinc-900">
        {imageExists ? (
          // eslint-disable-next-line @next/next/no-img-element
          <img
            src={generatedImageUrl(record.imageFile)}
            alt={prompt.slice(0, 80)}
            className="h-full w-full object-cover"
          />
        ) : (
          <div className="flex h-full items-center justify-center px-4 text-center text-sm text-zinc-500">
            Missing: {record.imageFile}
          </div>
        )}
      </div>

      <div className="space-y-3 border-t border-zinc-200 px-4 py-3 dark:border-zinc-800">
        <div>
          <p className="text-xs text-zinc-500 dark:text-zinc-400">
            {formatDate(record.createdAt)}
            {" · "}
            <span className="font-mono">{record.id}</span>
          </p>
          <p className="mt-2 line-clamp-3 text-sm text-zinc-700 dark:text-zinc-300">
            {prompt}
          </p>
        </div>

      </div>
    </Link>
  );
}
