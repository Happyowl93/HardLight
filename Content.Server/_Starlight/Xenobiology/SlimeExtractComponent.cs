using Content.Shared.Chemistry.Components;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Server._Starlight.Xenobiology;

[RegisterComponent]
public sealed partial class SlimeExtractComponent : Component
{
    /// <summary>
    /// What occurs when this extract receives some specific reagent.
    /// Each entry is a reagent reaction, consisting of the requirements and then the response
    /// </summary>
    [DataField("extractReactions", required: true)]
    public List<ExtractReaction> ExtractReactions = new();
    
    /// <summary>
    /// The name of the container that holds the solution.
    /// Needed so that the slime extract can communicate with the container itself.
    /// </summary>
    [DataField("containerName", required: true)]
    public string ContainerName = string.Empty;
}

/// <summary>
/// A set of requirements and the associated effects.
/// </summary>
[Serializable, DataDefinition]
public sealed partial class ExtractReaction
{
    /// <summary>
    /// The minimum reagent requirements.
    /// </summary>
    [DataField("requirements", required: true)]
    public Solution Requirements = default!;

    /// <summary>
    /// The effects caused when there is enough of the required reagents.
    /// </summary>
    [DataField("effects", required: true)]
    public List<ScaledEntityEffect> Effects = default!;
}

/// <summary>
/// An entity effect combined with a scaling factor.
/// </summary>
/// <remarks>
/// While this could be used anywhere, this is meant for xenobiology.
/// As such, I'll document the formula here for the sake of people writing slime extracts.
/// First, a minimized scaling factor is found among the reagents.
/// Specifically, for each reagent, amountInContainer/amountRequired is calculated.
/// The minimum found across all the reagents is taken and used as the minimizedScalingFactor.
/// The final factor is then minimizedScalingFactor * scalingFactor + scalingOffset.
/// Great fans of y = mx + b should find themselves right at home here.
/// </remarks>
[Serializable, DataDefinition]
public sealed partial class ScaledEntityEffect
{
    /// <summary>
    /// The effect.
    /// </summary>
    [DataField("effect", required: true)]
    public EntityEffect Effect = default!;
    
    /// <summary>
    /// Increases the scale in proportion to how much reagent was provided.
    /// </summary>
    [DataField("scalingFactor")]
    public FixedPoint2 ScalingFactor = 0;

    /// <summary>
    /// A flat modifier added at the end of calculating the scale.
    /// </summary>
    [DataField("scalingOffset")]
    public FixedPoint2 ScalingOffset = 1;
}