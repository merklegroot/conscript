namespace Conscript;

internal readonly struct DebugRoomEntry
{
    public Game.Phase Phase { get; init; }
    public string DisplayName { get; init; }
}

/// <summary>All playable phases for the debug room picker (mirrors image_gen GAME_ROOMS + extras).</summary>
internal static class DebugRoomCatalog
{
    public static readonly DebugRoomEntry[] Rooms =
    [
        new() { Phase = Game.Phase.Opening, DisplayName = "Family Apartment" },
        new() { Phase = Game.Phase.Outside, DisplayName = "Apartment Courtyard" },
        new() { Phase = Game.Phase.Town, DisplayName = "Town" },
        new() { Phase = Game.Phase.IndustrialDistrict, DisplayName = "Industrial District" },
        new() { Phase = Game.Phase.CommercialDistrict, DisplayName = "Commercial District" },
        new() { Phase = Game.Phase.Store, DisplayName = "Convenience Store" },
        new() { Phase = Game.Phase.Cafe, DisplayName = "Кафе" },
        new() { Phase = Game.Phase.CafeBasement, DisplayName = "Кафе — Basement" },
        new() { Phase = Game.Phase.DeliveryTruck, DisplayName = "Delivery Truck" },
        new() { Phase = Game.Phase.WarehouseTruck, DisplayName = "Warehouse 14 — Bay 3" },
        new() { Phase = Game.Phase.WarehouseAmbush, DisplayName = "Warehouse 14 — Ambush" },
        new() { Phase = Game.Phase.WarehouseAftermath, DisplayName = "Warehouse 14 — Aftermath" },
        new() { Phase = Game.Phase.WarehouseInterior, DisplayName = "Warehouse 14 — Interior" },
        new() { Phase = Game.Phase.GasStation, DisplayName = "Gas Station" },
        new() { Phase = Game.Phase.ForestEntry, DisplayName = "Forest Entry" },
        new() { Phase = Game.Phase.ForestStream, DisplayName = "Forest Stream" },
        new() { Phase = Game.Phase.Forest, DisplayName = "Deep Forest" },
        new() { Phase = Game.Phase.Tent, DisplayName = "Trash Bag Tent" },
        new() { Phase = Game.Phase.Death, DisplayName = "Death" },
    ];
}
