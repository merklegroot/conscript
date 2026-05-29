import { access } from "fs/promises";
import Link from "next/link";
import { notFound } from "next/navigation";
import path from "path";
import PasteRoomImage from "@/components/PasteRoomImage";
import RoomPromptEditor from "@/components/RoomPromptEditor";
import { gameImageUrl, getGameImageVersion } from "@/lib/game-image-cache";
import { getRoomWithPromptResolved } from "@/lib/get-room-with-prompt";
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
  const room = await getRoomWithPromptResolved(phase);

  if (!room) {
    notFound();
  }

  const hasImage = await imageExists(room.imageFile);
  const imageVersion = await getGameImageVersion(room.imageFile);
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

      <div className="mt-10">
        <RoomPromptEditor
          phase={room.phase}
          defaultPrompt={room.defaultPrompt}
          defaultPromptSource={room.defaultPromptSource}
          initialPrompt={room.prompt}
          promptSource={room.promptSource}
          imageFile={room.imageFile}
        />
      </div>
    </main>
  );
}
