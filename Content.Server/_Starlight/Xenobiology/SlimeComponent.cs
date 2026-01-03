using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Server._Starlight.Xenobiology;

/// <summary>
/// This component describes the current state of the slime.
/// </summary>
[RegisterComponent]
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
    [DataField("nutritionChangePerSecond", required: true)]
    public FixedPoint2 NutritionChangePerSecond;
    
    /// <summary>
    /// The amount of damage dealt to the target entity wheb the slime eats.
    /// </summary>
    [DataField("damageOnEat", required: true)]
    public DamageSpecifier DamageOnEat;
    
    /// <summary>
    /// The amount of nutrition the slime gains on each eat.
    /// </summary>
    [DataField("nutritionOnHit", required: true)]
    public FixedPoint2 NutritionOnHit;
    
    /// <summary>
    /// What this slime splits into if not mutating.
    /// </summary>
    [DataField("splitInto", required: true)]
    public string SplitInto;
}