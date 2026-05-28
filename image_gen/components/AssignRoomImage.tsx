"use client";

import { useState } from "react";
import type { GameRoom } from "@/lib/game-rooms";
import { roomHref } from "@/lib/game-rooms";

type AssignRoomImageProps = {
  generationId: string;
  rooms: GameRoom[];
};

type InstallSuccess = {
  ok: true;
  roomName: string;
  destPath: string;
  phase: string;
  message?: string;
};

export default function AssignRoomImage({
  generationId,
  rooms,
}: AssignRoomImageProps) {
  const [phase, setPhase] = useState(rooms[0]?.phase ?? "");
  const [isInstalling, setIsInstalling] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<InstallSuccess | null>(null);

  async function handleSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError(null);
    setSuccess(null);
    setIsInstalling(true);

    try {
      const response = await fetch("/api/install-room-image", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ generationId, phase }),
      });

      const data = (await response.json()) as InstallSuccess & {
        error?: string;
      };

      if (!response.ok) {
        throw new Error(data.error ?? "Install failed");
      }

      setSuccess(data);
    } catch (submitError) {
      setError(
        submitError instanceof Error ? submitError.message : "Install failed",
      );
    } finally {
      setIsInstalling(false);
    }
  }

  const selectedRoom = rooms.find((room) => room.phase === phase);

  return (
    <section className="mt-10 space-y-4 rounded-lg border border-zinc-200 p-4 dark:border-zinc-800">
      <h2 className="text-lg font-semibold">Use as room background</h2>
      <p className="text-sm text-zinc-600 dark:text-zinc-400">
        Installs this image into{" "}
        <code className="rounded bg-zinc-100 px-1 py-0.5 text-xs dark:bg-zinc-900">
          Conscript/img/
        </code>{" "}
        at 1536×1024 (center crop 3:2). Rebuild the game to see it in Raylib.
      </p>

      <form onSubmit={handleSubmit} className="flex flex-wrap items-end gap-3">
        <div className="min-w-[12rem] flex-1">
          <label
            htmlFor="room-phase"
            className="mb-2 block text-sm font-medium"
          >
            Room
          </label>
          <select
            id="room-phase"
            value={phase}
            onChange={(event) => setPhase(event.target.value)}
            className="w-full rounded-lg border border-zinc-300 bg-white px-3 py-2 text-sm dark:border-zinc-700 dark:bg-zinc-950"
          >
            {rooms.map((room) => (
              <option key={room.phase} value={room.phase}>
                {room.name} → {room.imageFile}
              </option>
            ))}
          </select>
        </div>

        <button
          type="submit"
          disabled={isInstalling || !phase}
          className="rounded-lg bg-zinc-900 px-4 py-2 text-sm font-medium text-white disabled:opacity-50 dark:bg-zinc-100 dark:text-zinc-900"
        >
          {isInstalling ? "Installing…" : "Install as room image"}
        </button>
      </form>

      {selectedRoom ? (
        <p className="text-xs text-zinc-500">
          Overwrites{" "}
          <code className="font-mono">Conscript/img/{selectedRoom.imageFile}</code>
        </p>
      ) : null}

      {error ? (
        <p className="rounded-lg border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-800 dark:border-red-900 dark:bg-red-950 dark:text-red-200">
          {error}
        </p>
      ) : null}

      {success ? (
        <div className="rounded-lg border border-green-200 bg-green-50 px-3 py-2 text-sm text-green-900 dark:border-green-900 dark:bg-green-950 dark:text-green-100">
          <p>
            Installed to{" "}
            <code className="font-mono">{success.destPath}</code> for{" "}
            <strong>{success.roomName}</strong>.
          </p>
          {success.message ? (
            <p className="mt-1 font-mono text-xs">{success.message}</p>
          ) : null}
          <p className="mt-2">
            <a href={roomHref(success.phase)} className="font-medium underline">
              View room
            </a>
            {" · "}
            Run <code className="font-mono">dotnet build</code> to refresh embedded
            textures.
          </p>
        </div>
      ) : null}
    </section>
  );
}
