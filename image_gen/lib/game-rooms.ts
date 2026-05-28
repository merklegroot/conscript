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
  { phase: "ForestEntry", name: "Forest Entry", imageFile: "forest-entry.png" },
  { phase: "ForestStream", name: "Forest Stream", imageFile: "forest-stream.png" },
  { phase: "Forest", name: "Deep Forest", imageFile: "trees.png" },
  { phase: "Tent", name: "Trash Bag Tent", imageFile: "tent-interior.png" },
];

export function gameImageUrl(imageFile: string): string {
  return `/api/game-images/${encodeURIComponent(imageFile)}`;
}
