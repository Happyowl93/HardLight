using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Starlight.Xenobiology;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class SlimeProcessorComponent : Component
{
    /// <summary>
    /// The amount of time it takes to process slime corpses.
    /// </summary>
    [DataField("processingTime", required: true), AutoNetworkedField]
    public TimeSpan ProcessingTime = default!;

    /// <summary>
    /// How many extracts are obtained per slime corpse.
    /// </summary>
    [DataField("yieldMultiplier", required: true), AutoNetworkedField]
    public int YieldMultiplier = default;

    /// <summary>
    /// How long between each slime acquire.
    /// </summary>
    [DataField("slimeAcquireCooldown", required: true), AutoNetworkedField]
    public TimeSpan SlimeAcquireCooldown = default!;
    
    /// <summary>
    /// The current list of extracts to spit out after processing.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public List<EntProtoId> Extracts = new();
    
    /// <summary>
    /// The moment in time when processing will be done.
    /// </summary>
    [ViewVariables, AutoNetworkedField, AutoPausedField]
    public TimeSpan? ProcessingFinishedMoment = default!;

    /// <summary>
    /// The moment in time when another slime will be sucked up.
    /// </summary>
    [ViewVariables, AutoNetworkedField, AutoPausedField]
    public TimeSpan? SlimeAcquireMoment = default!;

    /// <summary>
    /// Whether this processor is processing.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public bool IsProcessing = false;

    /// <summary>
    /// Whether this processor is powered.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public bool IsPowered = false;
}