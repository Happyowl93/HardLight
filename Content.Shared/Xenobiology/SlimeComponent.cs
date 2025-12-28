using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Xenobiology;

/// <summary>
/// This component describes the current state of the slime.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SlimeComponent : Component
{
    /// <summary>
    /// The starting nutrition amount. (Or current, if the slime is spawned)
    /// </summary>
    [DataField("nutrition", required: true)]
    public FixedPoint2 Nutrition;
    
    /// <summary>
    /// The amount the nutrition changes every second. Positive means the nutrition will increase, negative means the nutrition will decrease. Cannot go below 0.
    /// </summary>
    [DataField("nutrition_change_per_second", required: true)]
    public FixedPoint2 NutritionChangePerSecond;
    
    /// <summary>
    /// The amount of damage dealt to the target entity wheb the slime eats.
    /// </summary>
    [DataField("damage_on_eat", required: true)]
    public DamageSpecifier DamageOnEat;
    
    /// <summary>
    /// The amount of nutrition the slime gains on each eat.
    /// </summary>
    [DataField("nutrition_on_hit", required: true)]
    public FixedPoint2 NutritionOnHit;
}

[Serializable, NetSerializable]
public sealed class SlimeComponentState : ComponentState
{
    public FixedPoint2 Nutrition;
    public FixedPoint2 NutritionChangePerSecond;
    public DamageSpecifier DamageOnEat;
    public FixedPoint2 NutritionOnHit;

    public SlimeComponentState(FixedPoint2 nutrition,
        FixedPoint2 nutritionChangePerSecond,
        DamageSpecifier damageOnEat,
        FixedPoint2 nutritionOnHit)
    {
        Nutrition = nutrition;
        NutritionChangePerSecond = nutritionChangePerSecond;
        DamageOnEat = damageOnEat;
        NutritionOnHit = nutritionOnHit;
    }
}