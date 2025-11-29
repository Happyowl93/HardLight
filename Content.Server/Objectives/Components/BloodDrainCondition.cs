using Content.Server._Starlight.Antags.Vampires;
using Content.Server.Objectives.Systems;

namespace Content.Server.Objectives.Components;

[RegisterComponent, Access(typeof(VampireSystem))]
public sealed partial class BloodDrainConditionComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float BloodDranked = 0f;
}