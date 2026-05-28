import ImageGenerator from "@/components/ImageGenerator";

export default function Home() {
  return (
    <main className="mx-auto max-w-5xl flex-1 px-6 py-12">
      <div className="mb-8">
        <h1 className="text-2xl font-semibold">Home</h1>
        <p className="mt-2 text-sm text-zinc-600 dark:text-zinc-400">
          Generate images with Grok Imagine. Outputs are saved under{" "}
          <code className="rounded bg-zinc-100 px-1 py-0.5 text-xs dark:bg-zinc-900">
            generated_images/
          </code>{" "}
          at the repo root.
        </p>
      </div>

      <ImageGenerator />
    </main>
  );
}
