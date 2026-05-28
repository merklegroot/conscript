import { access } from "fs/promises";
import Link from "next/link";
import { notFound } from "next/navigation";
import path from "path";
import CopyPromptButton from "@/components/CopyPromptButton";
import PasteRoomImage from "@/components/PasteRoomImage";
import { gameImageUrl, getGameImageVersion } from "@/lib/game-image-cache";
import { getRoomWithPrompt } from "@/lib/game-rooms";
import { CONSCRIPT_IMG_DIR } from "@/lib/paths";

type RoomPageProps = {
  params: Promise<{ phase: string }>;
};

export const dynamic = "force-dynamic";

async function imageExists(imageFile: string): Promise<boolean> {
  try {
    await access(path.join(CONSCRIPT_IMG_DIR, imageFile));
    return true;
  } catch {
    return false;
  }
}

export default async function RoomDetailPage({ params }: RoomPageProps) {
  const { phase: phaseParam } = await params;
  const phase = decodeURIComponent(phaseParam);
  const room = getRoomWithPrompt(phase);

  if (!room) {
    notFound();
  }

  const hasImage = await imageExists(room.imageFile);
  const imageVersion = await getGameImageVersion(room.imageFile);
  const regenerateHref = `/?room=${encodeURIComponent(room.phase)}`;

  return (
    <main className="mx-auto max-w-4xl flex-1 px-6 py-12">
      <Link
        href="/rooms"
        className="text-sm text-zinc-600 hover:text-zinc-900 dark:text-zinc-400 dark:hover:text-zinc-100"
      >
        ← Rooms
      </Link>

      <h1 className="mt-6 text-2xl font-semibold">{room.name}</h1>

      <div className="mt-8 overflow-hidden rounded-lg border border-zinc-200 bg-zinc-100 dark:border-zinc-800 dark:bg-zinc-900">
        {hasImage ? (
          // eslint-disable-next-line @next/next/no-img-element
          <img
            src={gameImageUrl(room.imageFile, imageVersion)}
            alt={room.name}
            className="w-full"
          />
        ) : (
          <p className="px-6 py-24 text-center text-sm text-zinc-500">
            Missing: {room.imageFile}
          </p>
        )}
      </div>

      <div className="mt-8">
        <PasteRoomImage
          phase={room.phase}
          roomName={room.name}
          imageFile={room.imageFile}
        />
      </div>

      <section className="mt-10 space-y-4">
        <div className="flex flex-wrap items-center justify-between gap-3">
          <h2 className="text-lg font-semibold">Regeneration prompt</h2>
          <div className="flex flex-wrap gap-2">
            <CopyPromptButton text={room.prompt} />
            <Link
              href={regenerateHref}
              className="rounded-lg bg-zinc-900 px-3 py-1.5 text-sm font-medium text-white transition hover:bg-zinc-700 dark:bg-zinc-100 dark:text-zinc-900 dark:hover:bg-zinc-300"
            >
              Generate with Grok
            </Link>
          </div>
        </div>

        <p className="text-xs text-zinc-500 dark:text-zinc-400">
          {room.promptSource === "verified"
            ? "Verified — matches a logged generation in image-prompts.md"
            : "Inferred from Game.cs narrative and project style guide — tweak before regenerating"}
        </p>

        <pre className="overflow-x-auto rounded-lg border border-zinc-200 bg-zinc-50 p-4 text-sm leading-relaxed whitespace-pre-wrap text-zinc-800 dark:border-zinc-800 dark:bg-zinc-950 dark:text-zinc-200">
          {room.prompt}
        </pre>

        <p className="text-xs text-zinc-500 dark:text-zinc-400">
          Target asset:{" "}
          <code className="font-mono">Conscript/img/{room.imageFile}</code>
          {" · "}
          <code className="font-mono">
            python3 scripts/install_scene_image.py generated_images/&lt;file&gt; Conscript/img/
            {room.imageFile}
          </code>
        </p>
      </section>
    </main>
  );
}
