import Link from "next/link";
import { notFound } from "next/navigation";
import AssignRoomImage from "@/components/AssignRoomImage";
import CopyPromptButton from "@/components/CopyPromptButton";
import { GAME_ROOMS } from "@/lib/game-rooms";
import { generatedImageUrl, getGenerationById } from "@/lib/load-generations";

type GeneratedDetailPageProps = {
  params: Promise<{ id: string }>;
};

export const dynamic = "force-dynamic";

function formatDate(iso: string): string {
  return new Date(iso).toLocaleString(undefined, {
    dateStyle: "medium",
    timeStyle: "short",
  });
}

export default async function GeneratedDetailPage({
  params,
}: GeneratedDetailPageProps) {
  const { id: idParam } = await params;
  const id = decodeURIComponent(idParam);
  const generation = await getGenerationById(id);

  if (!generation) {
    notFound();
  }

  const { record, imageExists } = generation;
  const prompt = record.grok.request.prompt;

  return (
    <main className="mx-auto max-w-4xl flex-1 px-6 py-12">
      <Link
        href="/generated"
        className="text-sm text-zinc-600 hover:text-zinc-900 dark:text-zinc-400 dark:hover:text-zinc-100"
      >
        ← Generated
      </Link>

      <h1 className="mt-6 text-2xl font-semibold">Generation</h1>
      <p className="mt-2 text-sm text-zinc-600 dark:text-zinc-400">
        {formatDate(record.createdAt)}
        {" · "}
        <span className="font-mono">{record.id}</span>
      </p>

      <div className="mt-8 overflow-hidden rounded-lg border border-zinc-200 bg-zinc-100 dark:border-zinc-800 dark:bg-zinc-900">
        {imageExists ? (
          // eslint-disable-next-line @next/next/no-img-element
          <img
            src={generatedImageUrl(record.imageFile)}
            alt={prompt.slice(0, 80)}
            className="w-full"
          />
        ) : (
          <p className="px-6 py-24 text-center text-sm text-zinc-500">
            Missing: {record.imageFile}
          </p>
        )}
      </div>

      <section className="mt-8 space-y-3">
        <div className="flex flex-wrap items-center justify-between gap-2">
          <h2 className="text-lg font-semibold">Prompt</h2>
          <CopyPromptButton text={prompt} />
        </div>
        <pre className="overflow-x-auto rounded-lg border border-zinc-200 bg-zinc-50 p-4 text-sm leading-relaxed whitespace-pre-wrap text-zinc-800 dark:border-zinc-800 dark:bg-zinc-950 dark:text-zinc-200">
          {prompt}
        </pre>
      </section>

      {imageExists ? (
        <AssignRoomImage generationId={record.id} rooms={GAME_ROOMS} />
      ) : null}

      <details className="mt-8 text-sm text-zinc-600 dark:text-zinc-400">
        <summary className="cursor-pointer font-medium text-zinc-900 dark:text-zinc-100">
          Full metadata
        </summary>
        <pre className="mt-3 overflow-x-auto rounded-lg bg-zinc-50 p-4 font-mono text-xs dark:bg-zinc-950">
          {JSON.stringify(record, null, 2)}
        </pre>
      </details>
    </main>
  );
}
