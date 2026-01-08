using Robust.Shared.Containers;
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
    /// The name of the container the slime corpses are stored in.
    /// </summary>
    public const string SlimeContainerName = "slimes";
    
    /// <summary>
    /// Container for dead slimes inserted in the processor.
    /// </summary>
    [ViewVariables]
    public ContainerSlot SlimeContainer = default!;
}