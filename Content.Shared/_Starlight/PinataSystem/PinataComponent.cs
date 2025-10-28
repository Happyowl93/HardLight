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
    /// The entity table to select loot from when entity hitten by someone.
    /// </summary>
    [DataField]
    public EntityTableSelector? HitTable;
    
    /// <summary>
    /// The entity table to select loot from when entity gibbed.
    /// </summary>
    [DataField]
    public EntityTableSelector? GibTable;
}
