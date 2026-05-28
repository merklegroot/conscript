import { access } from "fs/promises";
import Link from "next/link";
import { notFound } from "next/navigation";
import path from "path";
import {
  GAME_ROOMS,
  gameImageUrl,
  getRoomByPhase,
} from "@/lib/game-rooms";
import { CONSCRIPT_IMG_DIR } from "@/lib/paths";

type RoomPageProps = {
  params: Promise<{ phase: string }>;
};

export function generateStaticParams() {
  return GAME_ROOMS.map((room) => ({ phase: room.phase }));
}

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
  const room = getRoomByPhase(phase);

  if (!room) {
    notFound();
  }

  const hasImage = await imageExists(room.imageFile);

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
            src={gameImageUrl(room.imageFile)}
            alt={room.name}
            className="w-full"
          />
        ) : (
          <p className="px-6 py-24 text-center text-sm text-zinc-500">
            Missing: {room.imageFile}
          </p>
        )}
      </div>
    </main>
  );
}
