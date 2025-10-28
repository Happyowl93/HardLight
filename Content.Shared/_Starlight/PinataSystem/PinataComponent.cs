using Robust.Shared.Prototypes;

namespace Content.Server.BPL.Pinata;

[RegisterComponent]
public sealed partial class PinataComponent : Component
{
    /// <summary>
    /// The entity table to select loot from.
    /// </summary>
    [DataField(required: true)]
    public EntityTableSelector Table = default!;

    [DataField]
    public int MaxSpawn = 4;
}
