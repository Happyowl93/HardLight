namespace Content.Server._Starlight.Xenobiology;

[RegisterComponent]
public sealed partial class SlimeProcessorComponent : Component
{
    /// <summary>
    /// The amount of time it takes to process slime corpses.
    /// </summary>
    [DataField("processingTime", required: true)]
    public float ProcessingTime = default;

    /// <summary>
    /// How many extracts are obtained per slime corpse.
    /// </summary>
    [DataField("yieldMultiplier", required: true)]
    public int YieldMultiplier = default;

    /// <summary>
    /// How long between each slime acquire.
    /// </summary>
    [DataField("slimeAcquireCooldown")]
    public float SlimeAcquireCooldown = default;
    
    /// <summary>
    /// The current list of extracts to spit out after processing.
    /// </summary>
    [ViewVariables]
    public List<string> Extracts = new();
    
    /// <summary>
    /// Gets set to <see cref="ProcessingTime"/> when clicked and has extracts.
    /// When it hits 0, spit out the extracts
    /// </summary>
    [ViewVariables]
    public float ProcessingTimer = 0;

    /// <summary>
    /// Gets set to <see cref="SlimeAcquireCooldown"/> after each slime is sucked up.
    /// When it hits 0, sucks up a new slime.
    /// </summary>
    public float SlimeAcquireTimer = 0;

    /// <summary>
    /// Whether this processor is processing.
    /// </summary>
    [ViewVariables]
    public bool IsProcessing = false;

    /// <summary>
    /// Whether this processor is powered.
    /// </summary>
    [ViewVariables]
    public bool IsPowered = false;
}