import { getRoomPrompt, type PromptSource } from "./room-prompts";

export type GameRoom = {
  phase: string;
  name: string;
  imageFile: string;
};

/** Playable location phases and their scene backgrounds (mirrors Game.cs). */
export const GAME_ROOMS: GameRoom[] = [
  { phase: "Opening", name: "Family Apartment", imageFile: "apartment-inside.png" },
  { phase: "Outside", name: "Apartment Courtyard", imageFile: "apartment-outside.png" },
  { phase: "Town", name: "Town", imageFile: "town.png" },
  {
    phase: "IndustrialDistrict",
    name: "Industrial District",
    imageFile: "industrial.png",
  },
  {
    phase: "CommercialDistrict",
    name: "Commercial District",
    imageFile: "commercial.png",
  },
  { phase: "Store", name: "Convenience Store", imageFile: "store.png" },
  { phase: "Cafe", name: "Кафе", imageFile: "cafe.png" },
  {
    phase: "DeliveryTruck",
    name: "Delivery Truck",
    imageFile: "delivery-truck-cab.png",
  },
  {
    phase: "WarehouseTruck",
    name: "Warehouse 14 — Bay 3",
    imageFile: "warehouse-14.png",
  },
  {
    phase: "WarehouseAmbush",
    name: "Warehouse 14 — Bay 3 (ambush)",
    imageFile: "warehouse-14-ambush.png",
  },
  {
    phase: "WarehouseAftermath",
    name: "Warehouse 14 — Bay 3 (aftermath)",
    imageFile: "warehouse-14-aftermath.png",
  },
  {
    phase: "GasStation",
    name: "Gas Station",
    imageFile: "gas-station.png",
  },
  { phase: "ForestEntry", name: "Forest Entry", imageFile: "forest-entry.png" },
  { phase: "ForestStream", name: "Forest Stream", imageFile: "forest-stream.png" },
  { phase: "Forest", name: "Deep Forest", imageFile: "trees.png" },
  { phase: "Tent", name: "Trash Bag Tent", imageFile: "tent-interior.png" },
];

export function roomHref(phase: string): string {
  return `/rooms/${encodeURIComponent(phase)}`;
}

export function getRoomByPhase(phase: string): GameRoom | undefined {
  return GAME_ROOMS.find((room) => room.phase === phase);
}

export type GameRoomWithPrompt = GameRoom & {
  prompt: string;
  defaultPrompt: string;
  defaultPromptSource: PromptSource;
  promptSource: PromptSource;
};

export function getRoomWithPrompt(phase: string): GameRoomWithPrompt | undefined {
  const room = getRoomByPhase(phase);
  const promptInfo = getRoomPrompt(phase);

  if (!room || !promptInfo) {
    return undefined;
  }

  return {
    ...room,
    defaultPrompt: promptInfo.prompt,
    defaultPromptSource: promptInfo.source,
    prompt: promptInfo.prompt,
    promptSource: promptInfo.source,
  };
}
