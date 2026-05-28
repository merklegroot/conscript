import GeneratedImageCard from "@/components/GeneratedImageCard";
import { loadGenerations } from "@/lib/load-generations";

export const dynamic = "force-dynamic";

export default async function GeneratedPage() {
  const generations = await loadGenerations();

  return (
    <main className="mx-auto max-w-6xl flex-1 px-6 py-12">
      <div className="mb-8">
        <h1 className="text-2xl font-semibold">Generated</h1>
        <p className="mt-2 text-sm text-zinc-600 dark:text-zinc-400">
          Images created via Grok (web app or{" "}
          <code className="rounded bg-zinc-100 px-1 py-0.5 text-xs dark:bg-zinc-900">
            generate_grok_image.sh
          </code>
          ) in{" "}
          <code className="rounded bg-zinc-100 px-1 py-0.5 text-xs dark:bg-zinc-900">
            generated_images/
          </code>
          .
        </p>
      </div>

      {generations.length === 0 ? (
        <p className="text-sm text-zinc-600 dark:text-zinc-400">
          No generations yet. Create one on{" "}
          <a href="/" className="font-medium underline">
            Home
          </a>
          .
        </p>
      ) : (
        <ul className="grid list-none gap-6 sm:grid-cols-2 lg:grid-cols-3">
          {generations.map(({ record, imageExists }) => (
            <li key={record.id}>
              <GeneratedImageCard record={record} imageExists={imageExists} />
            </li>
          ))}
        </ul>
      )}
    </main>
  );
}
