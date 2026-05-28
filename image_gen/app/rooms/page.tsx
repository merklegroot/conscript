import { access } from "fs/promises";
import path from "path";
import RoomCard from "@/components/RoomCard";
import { GAME_ROOMS } from "@/lib/game-rooms";
import { CONSCRIPT_IMG_DIR } from "@/lib/paths";

async function imageExists(imageFile: string): Promise<boolean> {
  try {
    await access(path.join(CONSCRIPT_IMG_DIR, imageFile));
    return true;
  } catch {
    return false;
  }
}

export default async function RoomsPage() {
  const roomsWithFiles = await Promise.all(
    GAME_ROOMS.map(async (room) => ({
      room,
      imageExists: await imageExists(room.imageFile),
    })),
  );

  return (
    <main className="mx-auto max-w-6xl flex-1 px-6 py-12">
      <div className="mb-8">
        <h1 className="text-2xl font-semibold">Rooms</h1>
        <p className="mt-2 text-sm text-zinc-600 dark:text-zinc-400">
          Scene backgrounds for each playable location in Conscript (
          {GAME_ROOMS.length} rooms).
        </p>
      </div>

      <ul className="grid list-none gap-6 sm:grid-cols-2 lg:grid-cols-3">
        {roomsWithFiles.map(({ room, imageExists }) => (
          <li key={room.phase}>
            <RoomCard room={room} imageExists={imageExists} />
          </li>
        ))}
      </ul>
    </main>
  );
}
