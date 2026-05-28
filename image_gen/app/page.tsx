import ImageGenerator from "@/components/ImageGenerator";
import { getRoomWithPrompt } from "@/lib/game-rooms";

type HomeProps = {
  searchParams: Promise<{ room?: string }>;
};

export default async function Home({ searchParams }: HomeProps) {
  const { room: roomParam } = await searchParams;
  const room = roomParam
    ? getRoomWithPrompt(decodeURIComponent(roomParam))
    : undefined;

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
        {room ? (
          <p className="mt-2 text-sm text-zinc-600 dark:text-zinc-400">
            Prefilled for <strong>{room.name}</strong> (
            {room.promptSource === "verified" ? "verified" : "inferred"} prompt).
            Install to{" "}
            <code className="rounded bg-zinc-100 px-1 py-0.5 text-xs dark:bg-zinc-900">
              Conscript/img/{room.imageFile}
            </code>{" "}
            after generating.
          </p>
        ) : null}
      </div>

      <ImageGenerator
        initialPrompt={room?.prompt}
        initialAspectRatio={room ? "3:2" : undefined}
      />
    </main>
  );
}
