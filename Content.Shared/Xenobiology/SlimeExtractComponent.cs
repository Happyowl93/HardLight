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
    
    [DataField("containerName", required: true)]
    public string ContainerName = string.Empty;
}

[Serializable, NetSerializable, DataDefinition]
public sealed partial class ExtractReaction
{
    [DataField("requirements", required: true)]
    public Solution Requirements = default!;

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