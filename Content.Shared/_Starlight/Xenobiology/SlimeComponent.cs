using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared._Starlight.Xenobiology;

/// <summary>
/// This component describes the current state of the slime.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SlimeComponent : Component
{
    /// <summary>
    /// The starting nutrition amount. (Or current, if the slime is spawned)
    /// </summary>
    [DataField("nutrition", required: true), AutoNetworkedField]
    public FixedPoint2 Nutrition;
    
    /// <summary>
    /// The amount the nutrition changes every second. Positive means the nutrition will increase, negative means the nutrition will decrease. Cannot go below 0.
    /// </summary>
    [DataField("nutritionChangePerSecond", required: true), AutoNetworkedField]
    public FixedPoint2 NutritionChangePerSecond;
    
    /// <summary>
    /// The amount of damage dealt to the target entity wheb the slime eats.
    /// </summary>
    [DataField("damageOnEat", required: true), AutoNetworkedField]
    public DamageSpecifier DamageOnEat;
    
    /// <summary>
    /// The amount of nutrition the slime gains on each eat.
    /// </summary>
    [DataField("nutritionOnHit", required: true), AutoNetworkedField]
    public FixedPoint2 NutritionOnHit;
    
    /// <summary>
    /// What this slime splits into if not mutating.
    /// </summary>
    [DataField("splitInto", required: true), AutoNetworkedField]
    public string SplitInto;
    
    /// <summary>
    /// The extract this slime provides when processed in the Slime Processor.
    /// </summary>
    [DataField("extract", required: true), AutoNetworkedField]
    public string Extract;

    /// <summary>
    /// The chance of mutating upon splitting.
    /// </summary>
    [DataField("mutationChance", required: true), AutoNetworkedField]
    public FixedPoint2 MutationChance;
    
    /// <summary>
    /// If mutating, the list of possible slimes to become.
    /// If blank, will not mutate at all.
    /// </summary>
    [DataField("splitIntoMutation"), AutoNetworkedField]
    public List<String> SplitIntoMutation = new();

    /// <summary>
    /// The amount of slime steroid potions that have been applied to this slime.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public int SlimeSteroidAmount = 0;
}