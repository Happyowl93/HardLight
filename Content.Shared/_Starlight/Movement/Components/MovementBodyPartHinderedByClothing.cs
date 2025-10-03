using Robust.Shared.GameStates;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;

namespace Content.Shared.Movement.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class MovementBodyPartHinderedByClothingComponent : Component
{
    // value from 0 ~ 1 where 0 is no reduction in movespeed
    [DataField("hinderAmount")]
    public float HinderAmount = 0.0f;
}