using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Xenobiology;

[RegisterComponent, NetworkedComponent]
public sealed partial class SlimeComponent : Component
{
    [DataField("hunger_threshold")]
    public FixedPoint2 HungerThreshold;

    [DataField("nutrition")]
    public FixedPoint2 Nutrition;

    [DataField("nutrition_change_per_second")]
    public FixedPoint2 NutritionChangePerSecond;
    
    [DataField("damage_on_eat")]
    public DamageSpecifier DamageOnEat;
    
    [DataField("nutrition_on_hit")]
    public FixedPoint2 NutritionOnHit;
}

[Serializable, NetSerializable]
public sealed class SlimeComponentState : ComponentState
{
    public FixedPoint2 HungerThreshold;
    public FixedPoint2 Nutrition;
    public FixedPoint2 NutritionChangePerSecond;
    public DamageSpecifier DamageOnEat;
    public FixedPoint2 NutritionOnHit;

    public SlimeComponentState(FixedPoint2 hungerThreshold,
        FixedPoint2 nutrition,
        FixedPoint2 nutritionChangePerSecond,
        DamageSpecifier damageOnEat,
        FixedPoint2 nutritionOnHit)
    {
        HungerThreshold = hungerThreshold;
        Nutrition = nutrition;
        NutritionChangePerSecond = nutritionChangePerSecond;
        DamageOnEat = damageOnEat;
        NutritionOnHit = nutritionOnHit;
    }
}