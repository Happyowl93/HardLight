namespace Content.Shared._Starlight.Antags.Vampires.Components;

using Content.Shared._Starlight.Antags.Vampires.Prototypes;
using Robust.Shared.Prototypes;

/// <summary>
/// Attach to a spawned action entity to define Vampire-specific drink related effects
/// </summary>
[RegisterComponent]
public sealed partial class VampireDrinkComponent : Component
{
    [DataField]
    public float HumanoidEfficiency = 0.5f;

    [DataField]
    public float NonHumanoidEfficiency = 0f; //Default to zero incase the line was removed from the yml

    [DataField]
    public DamageSpecifier DrinkDamage = new();

    [DataField]
    public float HumanoidSipAmount = 10f;
    
    [DataField]
    public float NonHumanoidSipAmount = 5f;

    [DataField]
    public float DamageSpecifier MaxDrinkDamage = new();
    
}
