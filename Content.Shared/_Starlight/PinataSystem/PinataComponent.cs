using Content.Shared.EntityTable.EntitySelectors;
using Robust.Shared.Prototypes;

namespace Content.Server._Starlight.PinataSystem;

/// <summary>
/// Makes the entity throw items when someone hits it.
/// </summary>
[RegisterComponent]
public sealed partial class PinataComponent : Component
{
    /// <summary>
    /// The entity table to select loot from.
    /// </summary>
    [DataField(required: true)]
    public EntityTableSelector Table = default!;
}
