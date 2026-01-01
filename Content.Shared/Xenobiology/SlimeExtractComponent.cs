using Content.Shared.Chemistry.Components;
using Content.Shared.EntityEffects;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Xenobiology;

[RegisterComponent, NetworkedComponent]
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
[Serializable, NetSerializable, DataDefinition]
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
    public List<EntityEffect> Effects = default!;
}

[Serializable, NetSerializable]
public sealed class SlimeExtractComponentState : ComponentState
{
    public List<ExtractReaction> ExtractReactions;

    public SlimeExtractComponentState(List<ExtractReaction> extractReactions)
    {
        ExtractReactions = extractReactions;
    }
}