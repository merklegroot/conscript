import { NextResponse } from "next/server";
import { getRoomByPhase } from "@/lib/game-rooms";
import {
  clearPromptOverride,
  getPromptOverride,
  setPromptOverride,
} from "@/lib/room-prompt-overrides";

type RoomPromptRequestBody = {
  phase?: string;
  prompt?: string;
};

export async function GET(request: Request) {
  const phase = new URL(request.url).searchParams.get("phase")?.trim();

  if (!phase) {
    return NextResponse.json({ error: "phase is required" }, { status: 400 });
  }

  if (!getRoomByPhase(phase)) {
    return NextResponse.json({ error: "Unknown room phase" }, { status: 404 });
  }

  const prompt = await getPromptOverride(phase);

  return NextResponse.json({ phase, prompt: prompt ?? null });
}

export async function PUT(request: Request) {
  let body: RoomPromptRequestBody;

  try {
    body = (await request.json()) as RoomPromptRequestBody;
  } catch {
    return NextResponse.json(
      { error: "Request body must be valid JSON" },
      { status: 400 },
    );
  }

  const phase = body.phase?.trim();
  const prompt = body.prompt?.trim();

  if (!phase || !prompt) {
    return NextResponse.json(
      { error: "phase and prompt are required" },
      { status: 400 },
    );
  }

  if (!getRoomByPhase(phase)) {
    return NextResponse.json({ error: "Unknown room phase" }, { status: 404 });
  }

  await setPromptOverride(phase, prompt);

  return NextResponse.json({ ok: true, phase, prompt });
}

export async function DELETE(request: Request) {
  let body: RoomPromptRequestBody;

  try {
    body = (await request.json()) as RoomPromptRequestBody;
  } catch {
    return NextResponse.json(
      { error: "Request body must be valid JSON" },
      { status: 400 },
    );
  }

  const phase = body.phase?.trim();

  if (!phase) {
    return NextResponse.json({ error: "phase is required" }, { status: 400 });
  }

  if (!getRoomByPhase(phase)) {
    return NextResponse.json({ error: "Unknown room phase" }, { status: 404 });
  }

  await clearPromptOverride(phase);

  return NextResponse.json({ ok: true, phase });
}
