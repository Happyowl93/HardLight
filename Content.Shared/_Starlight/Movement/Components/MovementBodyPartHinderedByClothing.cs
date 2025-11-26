using Robust.Shared.GameStates;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;

namespace Content.Shared.Movement.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class MovementBodyPartHinderedByClothingComponent : Component
{
    [DataField]
    public float HinderModifier = 0.0f;
}