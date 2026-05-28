"use client";

import { useRouter } from "next/navigation";
import { useCallback, useEffect, useRef, useState } from "react";
import { roomHref } from "@/lib/game-rooms";

type PasteRoomImageProps = {
  phase: string;
  roomName: string;
  imageFile: string;
};

type PasteSuccess = {
  ok: true;
  roomName: string;
  destPath: string;
  phase: string;
  message?: string;
  archivedAs?: string;
};

function isEditableTarget(target: EventTarget | null): boolean {
  if (!(target instanceof HTMLElement)) {
    return false;
  }

  const tag = target.tagName;

  return (
    tag === "INPUT" ||
    tag === "TEXTAREA" ||
    target.isContentEditable
  );
}

export default function PasteRoomImage({
  phase,
  roomName,
  imageFile,
}: PasteRoomImageProps) {
  const router = useRouter();
  const zoneRef = useRef<HTMLDivElement>(null);
  const [isFocused, setIsFocused] = useState(false);
  const [isInstalling, setIsInstalling] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<PasteSuccess | null>(null);

  const uploadImage = useCallback(
    async (file: File) => {
      setError(null);
      setSuccess(null);
      setIsInstalling(true);

      try {
        const formData = new FormData();
        formData.set("phase", phase);
        formData.set("image", file);

        const response = await fetch("/api/install-room-image-paste", {
          method: "POST",
          body: formData,
        });

        const data = (await response.json()) as PasteSuccess & {
          error?: string;
        };

        if (!response.ok) {
          throw new Error(data.error ?? "Failed to install pasted image");
        }

        setSuccess(data);
        router.refresh();
      } catch (uploadError) {
        setError(
          uploadError instanceof Error
            ? uploadError.message
            : "Failed to install pasted image",
        );
      } finally {
        setIsInstalling(false);
      }
    },
    [phase, router],
  );

  const handlePaste = useCallback(
    (event: ClipboardEvent) => {
      if (isEditableTarget(event.target)) {
        return;
      }

      const items = event.clipboardData?.items;
      if (!items) {
        return;
      }

      for (const item of items) {
        if (!item.type.startsWith("image/")) {
          continue;
        }

        const file = item.getAsFile();
        if (!file) {
          continue;
        }

        event.preventDefault();
        void uploadImage(file);
        return;
      }
    },
    [uploadImage],
  );

  useEffect(() => {
    window.addEventListener("paste", handlePaste);
    return () => window.removeEventListener("paste", handlePaste);
  }, [handlePaste]);

  function handleFileInput(event: React.ChangeEvent<HTMLInputElement>) {
    const file = event.target.files?.[0];
    if (file) {
      void uploadImage(file);
    }
    event.target.value = "";
  }

  return (
    <section className="space-y-3 rounded-lg border border-dashed border-zinc-300 p-4 dark:border-zinc-700">
      <h2 className="text-lg font-semibold">Paste image</h2>
      <p className="text-sm text-zinc-600 dark:text-zinc-400">
        Paste a screenshot or image from your clipboard to install it as{" "}
        <strong>{roomName}</strong> (
        <code className="font-mono text-xs">{imageFile}</code>
        ). Works anywhere on this page unless you are typing in a text field.
      </p>

      <div
        ref={zoneRef}
        tabIndex={0}
        onFocus={() => setIsFocused(true)}
        onBlur={() => setIsFocused(false)}
        className={`rounded-lg border px-4 py-8 text-center text-sm transition ${
          isFocused
            ? "border-zinc-500 bg-zinc-50 dark:border-zinc-400 dark:bg-zinc-900"
            : "border-zinc-200 bg-zinc-50/50 dark:border-zinc-800 dark:bg-zinc-950/50"
        }`}
      >
        {isInstalling ? (
          <p className="text-zinc-600 dark:text-zinc-300">Installing pasted image…</p>
        ) : (
          <>
            <p className="font-medium text-zinc-800 dark:text-zinc-200">
              Press Cmd+V (Ctrl+V) to paste
            </p>
            <p className="mt-2 text-zinc-500">or</p>
            <label className="mt-2 inline-block cursor-pointer font-medium text-zinc-900 underline dark:text-zinc-100">
              choose a file
              <input
                type="file"
                accept="image/png,image/jpeg,image/webp"
                className="sr-only"
                onChange={handleFileInput}
              />
            </label>
          </>
        )}
      </div>

      {error ? (
        <p className="rounded-lg border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-800 dark:border-red-900 dark:bg-red-950 dark:text-red-200">
          {error}
        </p>
      ) : null}

      {success ? (
        <div className="rounded-lg border border-green-200 bg-green-50 px-3 py-2 text-sm text-green-900 dark:border-green-900 dark:bg-green-950 dark:text-green-100">
          <p>
            Installed to <code className="font-mono">{success.destPath}</code> for{" "}
            <strong>{success.roomName}</strong>.
          </p>
          {success.archivedAs ? (
            <p className="mt-1 text-xs">Archived as {success.archivedAs}</p>
          ) : null}
          {success.message ? (
            <p className="mt-1 font-mono text-xs">{success.message}</p>
          ) : null}
          <p className="mt-2">
            <a href={roomHref(success.phase)} className="font-medium underline">
              View room
            </a>
            {" · "}
            Run <code className="font-mono">dotnet build</code> for the game.
          </p>
        </div>
      ) : null}
    </section>
  );
}
