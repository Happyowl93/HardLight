using Robust.Shared.GameStates;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;

namespace Content.Shared._Starlight.Movement.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class MovementBodyPartHinderedByShoesComponent : Component
{
    [DataField]
    public float HinderModifier = 0.0f;
}