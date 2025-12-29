using System.ComponentModel.DataAnnotations;
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

    /// <summary>
    /// The amount of nutrition beyond which the slime will split.
    /// </summary>
    [DataField("split_threshold", required: true)]
    public FixedPoint2 SplitThreshold;
    
    /// <summary>
    /// What this slime splits into if not mutating
    /// </summary>
    [DataField("split_into", required: true)]
    public string SplitInto;
    
    public bool Splitting = false;
    public FixedPoint2 CurrentSplitTime = FixedPoint2.Zero;
}

[Serializable, NetSerializable]
public sealed class SlimeComponentState : ComponentState
{
    public FixedPoint2 Nutrition;
    public FixedPoint2 NutritionChangePerSecond;
    public DamageSpecifier DamageOnEat;
    public FixedPoint2 NutritionOnHit;
    public FixedPoint2 SplitThreshold;
    public string SplitInto;

    public bool Splitting;
    public FixedPoint2 CurrentSplitTime;

    public SlimeComponentState(FixedPoint2 nutrition,
        FixedPoint2 nutritionChangePerSecond,
        DamageSpecifier damageOnEat,
        FixedPoint2 nutritionOnHit,
        FixedPoint2 splitThreshold,
        string splitInto,
        bool splitting,
        FixedPoint2 currentSplitTime)
    {
        Nutrition = nutrition;
        NutritionChangePerSecond = nutritionChangePerSecond;
        DamageOnEat = damageOnEat;
        NutritionOnHit = nutritionOnHit;
        SplitThreshold = splitThreshold;
        SplitInto = splitInto;

        Splitting = splitting;
        CurrentSplitTime = currentSplitTime;
    }
}