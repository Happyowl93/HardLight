using Robust.Shared.GameStates;

namespace Content.Shared._Starlight.Xenobiology;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SlimeProcessorComponent : Component
{
    /// <summary>
    /// The amount of time it takes to process slime corpses.
    /// </summary>
    [DataField("processingTime", required: true), AutoNetworkedField]
    public float ProcessingTime = default;

    /// <summary>
    /// How many extracts are obtained per slime corpse.
    /// </summary>
    [DataField("yieldMultiplier", required: true), AutoNetworkedField]
    public int YieldMultiplier = default;

    /// <summary>
    /// How long between each slime acquire.
    /// </summary>
    [DataField("slimeAcquireCooldown"), AutoNetworkedField]
    public float SlimeAcquireCooldown = default;
    
    /// <summary>
    /// The current list of extracts to spit out after processing.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public List<string> Extracts = new();
    
    /// <summary>
    /// Gets set to <see cref="ProcessingTime"/> when clicked and has extracts.
    /// When it hits 0, spit out the extracts
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public float ProcessingTimer = 0;

    /// <summary>
    /// Gets set to <see cref="SlimeAcquireCooldown"/> after each slime is sucked up.
    /// When it hits 0, sucks up a new slime.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public float SlimeAcquireTimer = 0;

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