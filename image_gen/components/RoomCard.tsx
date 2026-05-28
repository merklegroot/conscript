import type { GameRoom } from "@/lib/game-rooms";
import { gameImageUrl } from "@/lib/game-rooms";

type RoomCardProps = {
  room: GameRoom;
  imageExists: boolean;
};

export default function RoomCard({ room, imageExists }: RoomCardProps) {
  return (
    <article className="overflow-hidden rounded-lg border border-zinc-200 bg-white dark:border-zinc-800 dark:bg-zinc-950">
      <div className="aspect-[3/2] bg-zinc-100 dark:bg-zinc-900">
        {imageExists ? (
          // eslint-disable-next-line @next/next/no-img-element
          <img
            src={gameImageUrl(room.imageFile)}
            alt={room.name}
            className="h-full w-full object-cover"
          />
        ) : (
          <div className="flex h-full items-center justify-center px-4 text-center text-sm text-zinc-500">
            Missing: {room.imageFile}
          </div>
        )}
      </div>

      <div className="border-t border-zinc-200 px-4 py-3 dark:border-zinc-800">
        <h2 className="font-medium text-zinc-900 dark:text-zinc-100">{room.name}</h2>
        <p className="mt-1 text-xs text-zinc-500 dark:text-zinc-400">
          <span className="font-mono">{room.phase}</span>
          {" · "}
          <span className="font-mono">{room.imageFile}</span>
        </p>
      </div>
    </article>
  );
}
