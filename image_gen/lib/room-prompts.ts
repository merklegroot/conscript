/** Regeneration prompts for scene backgrounds. Verified = used for a shipped asset; inferred = drafted from Game.cs. */

export type PromptSource = "verified" | "inferred" | "custom";

export type RoomPrompt = {
  prompt: string;
  source: PromptSource;
};

const PROMPT_SUFFIX =
  " Ulan-Ude, Republic of Buryatia, early autumn. Cinematic photorealistic, subtle film grain, wide landscape 3:2. No people, no readable text, no logos, no signage.";

export const ROOM_PROMPTS: Record<string, RoomPrompt> = {
  Opening: {
    source: "inferred",
    prompt:
      "Dim Soviet-era family apartment interior at night, Khrushchyovka block. Small kitchen-living room, worn wallpaper, cheap curtains, single harsh ceiling light and cold blue light from a window. Empty chairs, coat hooks, sense of dread and silence after a knock at the door. Cramped, claustrophobic, on-the-run mood." +
      PROMPT_SUFFIX,
  },
  Outside: {
    source: "inferred",
    prompt:
      "Night photograph behind a Soviet apartment block courtyard, Ulan-Ude. Cracked concrete yard, metal drainpipes, laundry lines, one lit window high on the brick wall where someone just escaped. Deep shadows, sodium spill from a distant street, frost on the ground, oppressive quiet. Ground-level view looking across the yard toward the building." +
      PROMPT_SUFFIX,
  },
  Town: {
    source: "inferred",
    prompt:
      "Cinematic nighttime photograph of empty central streets in a post-Soviet Siberian town. Wet asphalt reflecting sodium streetlights, Khrushchyovka blocks on both sides, distant industrial silhouettes to the west and faint shop neon to the east, corner kiosk glow, long shadows, lonely on-the-run atmosphere." +
      PROMPT_SUFFIX,
  },
  IndustrialDistrict: {
    source: "inferred",
    prompt:
      "Cinematic nighttime photograph of a post-Soviet industrial district on the edge of a Siberian town. Narrow wet asphalt between weathered warehouses and corrugated factories, chain-link fences, rusted pipes, shipping containers, brick chimney. Warm sodium streetlights on rain-slick road; distant rail yard lights; old Soviet truck parked; dingy café corner with warm window glow; sparse bare autumn trees. Moody gritty photorealistic." +
      PROMPT_SUFFIX,
  },
  CommercialDistrict: {
    source: "inferred",
    prompt:
      "Cinematic nighttime photograph of a post-Soviet commercial street in a Siberian town. Small shopfronts, late-night convenience kiosk with warm neon, wet pavement reflecting signs, empty sidewalk, Khrushchyovka blocks in background, dark pine treeline suggested far south. Moody amber and cool neon palette, clearly retail not industrial." +
      PROMPT_SUFFIX,
  },
  Store: {
    source: "inferred",
    prompt:
      "Photorealistic interior of a bleak post-Soviet convenience store at night. Harsh flat fluorescent ceiling panels, narrow aisles, glass cooler glow, security camera on the ceiling, scratched linoleum, cheap snacks on shelves without readable labels. Empty counter area, brutal contrast after dark outdoors, paranoid exposed mood." +
      PROMPT_SUFFIX,
  },
  Cafe: {
    source: "inferred",
    prompt:
      "Dim interior of a dingy workers' café (кафе) on an industrial side street, post-Soviet Siberia. Steam over cheap tea glasses on Formica tables, yellowed walls, nicotine haze, single warm pendant light, empty stools, diesel and cigarette atmosphere, tense underworld mood. No faces, no readable signage." +
      PROMPT_SUFFIX,
  },
  DeliveryTruck: {
    source: "inferred",
    prompt:
      "Photorealistic view from inside the cab of an old Soviet ZIL delivery truck at night, driver's POV. Worn steering wheel, dim instrument cluster, rain on the windshield, wipers, warm key in ignition implied, industrial yards and sodium lights ahead through the glass. Ticking engine tension, warehouse run mood." +
      PROMPT_SUFFIX,
  },
  WarehouseTruck: {
    source: "inferred",
    prompt:
      "Night scene from inside a parked delivery truck cab at Warehouse 14 loading bay three. Through the rain-streaked windshield: corrugated hangar, half-open roll-up door, harsh floodlights cutting through heavy rain, wet concrete yard, pallets and barrels, chain-link fence. Truck hood visible at bottom of frame, idle engine mood, cargo deal about to go wrong." +
      PROMPT_SUFFIX,
  },
  WarehouseAmbush: {
    source: "verified",
    prompt:
      "Cinematic photorealistic nighttime scene at a Soviet-era warehouse loading bay in Ulan-Ude, Republic of Buryatia, early autumn, heavy rain. Ground-level view on wet concrete near an old delivery truck (edge of open truck door and a sliver of cab interior framing the right side), looking toward a half-open corrugated roll-up door spilling warm industrial light. Floodlights cut through rain, long reflections in puddles, scattered pallets and metal barrels, chain-link fence and shadowy yard beyond. Ominous tension: two indistinct threatening silhouettes standing near the doorway in deep shadow (no clear faces, no readable patches, no logos). Moody gritty post-Soviet atmosphere, subtle film grain, warm/cool mixed lighting, deep shadows. Wide landscape 3:2. No readable text, no signage, no gore, no prominent weapons.",
  },
  WarehouseAftermath: {
    source: "verified",
    prompt:
      "Cinematic photorealistic nighttime scene at a Soviet-era warehouse loading bay in Ulan-Ude, Republic of Buryatia, early autumn, heavy rain. Ground-level view on wet concrete near an old green delivery truck with open cab door on the right, corrugated roll-up door ahead — showing violent explosion aftermath. Roiling orange fire and thick black smoke billowing from the doorway, scorched blackened metal door, shattered windows, burning pallets and debris scattered across the ground, rain hissing on hot embers. Floodlights cutting through rain and smoke, long reflections in puddles, chain-link fence beyond. Moody gritty post-Soviet atmosphere, dramatic firelight vs cool rain, subtle film grain. Wide landscape 3:2. No readable text, no signage, no logos. the bodies of two bratdva mobsters are lying on the ground.",
  },
  ForestEntry: {
    source: "inferred",
    prompt:
      "Night photograph at the edge of taiga forest just past the last Soviet apartment blocks, Siberia. Pine trunks close in the foreground, muddy path, streetlight glow still bleeding through branches from behind, city noise fading, not safe yet only hidden. Cold early autumn air, oppressive blue-green shadows." +
      PROMPT_SUFFIX,
  },
  ForestStream: {
    source: "inferred",
    prompt:
      "Cinematic photograph of a narrow forest stream cutting through dark pines, Siberian taiga, early winter approaching. Painfully clear cold water over stones, animal tracks along the muddy bank, first thin snow on roots, muted greens and browns, survival mood, no civilization visible." +
      PROMPT_SUFFIX,
  },
  Forest: {
    source: "inferred",
    prompt:
      "Deep taiga forest in Siberia, early winter. Dense pine and birch trunks, first light snow on the ground and branches, overcast twilight, city far behind, harsh survival atmosphere, cold desaturated greens and grays, lonely and unforgiving." +
      PROMPT_SUFFIX,
  },
  Tent: {
    source: "inferred",
    prompt:
      "Cramped interior of a crude survival shelter made from black trash bags and duct tape, forest camp at night. Dim flashlight or candle glow on plastic folds, condensation, piled leaves and a thin blanket, claustrophobic protection from elements, desperate improvised mood." +
      PROMPT_SUFFIX,
  },
};

export function getRoomPrompt(phase: string): RoomPrompt | undefined {
  return ROOM_PROMPTS[phase];
}
