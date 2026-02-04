using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client._Starlight.CombatMode;

[Prototype("Sight")]
public sealed partial class SightPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = string.Empty;

    [DataField(required: true)]
    public string Name { get; private set; } = string.Empty;

    [DataField]
    public SightType Type = SightType.Melee;

    [DataField]
    public string? BoltVariant = null;

    [DataField]
    public bool Bolt = false;

    [DataField(required: true)]
    public SpriteSpecifier Sprite = SpriteSpecifier.Invalid;
}

public enum SightType : int
{
    Ranged,
    Melee,
}