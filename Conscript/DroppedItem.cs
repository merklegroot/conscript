namespace Conscript;

internal sealed class DroppedItem
{
    public required string Name { get; init; }
    public int? Charges { get; init; }
    public Game.Phase Room { get; init; }
    public int TurnsRemaining { get; set; }
    public int AnchorIndex { get; init; }
}
