namespace Content.Shared._Starlight.Antags.Vampires.Components;

using Content.Shared._Starlight.Antags.Vampires.Prototypes;
using Content.Shared.Damage;
using Robust.Shared.Prototypes;

/// <summary>
/// </summary>
[RegisterComponent]
public sealed partial class VampireDrinkComponent : Component
{
    [DataField]
    public float humanoidEfficiency = 0.5f;

    [DataField]
    public float nonHumanoidEfficiency = 0f; //Default to zero incase the line was removed from the yml

    [DataField]
    public DamageSpecifier drinkDamage = new();

    [DataField]
    public DamageSpecifier maxDrinkDamage = new();

    [DataField]
    public float sipAmount = 10f;
}
