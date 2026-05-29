import "server-only";

import { getRoomWithPrompt, type GameRoomWithPrompt } from "./game-rooms";
import { getPromptOverride } from "./room-prompt-overrides";

export async function getRoomWithPromptResolved(
  phase: string,
): Promise<GameRoomWithPrompt | undefined> {
  const room = getRoomWithPrompt(phase);

  if (!room) {
    return undefined;
  }

  const override = await getPromptOverride(phase);

  if (!override) {
    return room;
  }

  return {
    ...room,
    prompt: override,
    promptSource: "custom",
  };
}
