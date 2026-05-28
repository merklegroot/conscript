import { execFile as execFileCallback } from "node:child_process";
import path from "path";
import { promisify } from "node:util";
import { getRoomByPhase, type GameRoom } from "@/lib/game-rooms";
import { CONSCRIPT_IMG_DIR, INSTALL_SCENE_IMAGE_SCRIPT } from "@/lib/paths";

const execFile = promisify(execFileCallback);

export type InstallRoomImageResult = {
  room: GameRoom;
  destPath: string;
  message: string;
};

export async function installRoomImageFromPath(
  sourcePath: string,
  phase: string,
): Promise<InstallRoomImageResult> {
  const room = getRoomByPhase(phase);

  if (!room) {
    throw new Error("Unknown room");
  }

  const destPath = path.join(CONSCRIPT_IMG_DIR, room.imageFile);

  const { stdout, stderr } = await execFile(
    "python3",
    [INSTALL_SCENE_IMAGE_SCRIPT, sourcePath, destPath],
    { encoding: "utf8" },
  );

  return {
    room,
    destPath: `Conscript/img/${room.imageFile}`,
    message: (stdout || stderr).trim(),
  };
}
