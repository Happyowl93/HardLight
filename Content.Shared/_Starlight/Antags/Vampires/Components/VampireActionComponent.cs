using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._Starlight.Antags.Vampires;

/// <summary>
/// Attach to a spawned action entity to define Vampire-specific gating and costs
/// - BloodToUnlock - required TotalBlood to unlock the action
/// - BloodCost - Amount to consume on use
/// - RequiredClass - optional class requirement for the action to be usable
/// </summary>
[RegisterComponent]
public sealed partial class VampireActionComponent : Component
{
    [DataField]
    public int BloodToUnlock = 0;

    [DataField]
    public float BloodCost = 0f;

    [DataField]
    public VampireClassType? RequiredClass = null;
}

