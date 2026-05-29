import ImageGenerator from "@/components/ImageGenerator";
import PasteRoomImage from "@/components/PasteRoomImage";
import { getRoomWithPromptResolved } from "@/lib/get-room-with-prompt";

type HomeProps = {
  searchParams: Promise<{ room?: string }>;
};

export default async function Home({ searchParams }: HomeProps) {
  const { room: roomParam } = await searchParams;
  const room = roomParam
    ? await getRoomWithPromptResolved(decodeURIComponent(roomParam))
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
            {room.promptSource === "verified"
              ? "verified"
              : room.promptSource === "custom"
                ? "custom"
                : "inferred"}{" "}
            prompt).
            After generating, use <strong>Apply to {room.name}</strong> below, or
            paste an image (Cmd+V) to install into{" "}
            <code className="rounded bg-zinc-100 px-1 py-0.5 text-xs dark:bg-zinc-900">
              Conscript/img/{room.imageFile}
            </code>
            .
          </p>
        ) : null}
      </div>

      {room ? (
        <div className="mb-8">
          <PasteRoomImage
            phase={room.phase}
            roomName={room.name}
            imageFile={room.imageFile}
          />
        </div>
      ) : null}

      <ImageGenerator
        initialPrompt={room?.prompt}
        initialAspectRatio={room ? "3:2" : undefined}
        targetRoom={
          room
            ? {
                phase: room.phase,
                name: room.name,
                imageFile: room.imageFile,
              }
            : undefined
        }
      />
    </main>
  );
}
