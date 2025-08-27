using Robust.Shared.GameStates;


[RegisterComponent, NetworkedComponent]
public sealed partial class VampireActionComponent : Component
{
    [DataField] public bool RequireBlood = true;

    [DataField] public float BloodCost = 0;

}
