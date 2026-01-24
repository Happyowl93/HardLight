using Content.Shared.Tag;
using Robust.Shared.Prototypes;

namespace Content.Shared._Starlight.Traits.Effects;

/// <summary>
/// Effect that add/replace a background to the player entity.
/// </summary>
public sealed partial class BackgroundEffect : BaseTraitEffect
{
    [Dependency] private readonly TagSystem _tag = default!;

    /// <summary>
    /// The background of the entity.
    /// </summary>
    [DataField(required: true)]
    public string Background;

    public override void Apply(TraitEffectContext ctx)
    {
        var tag = new ProtoId<TagPrototype>(Background + "TraitBackground");
        _tag.TryAddTag(ctx.Player, tag);
    }
}