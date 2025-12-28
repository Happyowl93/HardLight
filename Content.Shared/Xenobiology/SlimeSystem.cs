using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Components;

namespace Content.Shared.Xenobiology;

/// <summary>
/// Handles the general behavior of slimes.
/// </summary>
public sealed class SlimeSystem : EntitySystem
{
    [Dependency] private SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    
    /// <inheritdoc />
    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var query = EntityQueryEnumerator<SlimeComponent>();
        while (query.MoveNext(out var uid, out var slime))
        {
            slime.Nutrition = FixedPoint2.Max(slime.Nutrition + (frameTime * slime.NutritionChangePerSecond), 0);
        }
    }
    
    /// <summary>
    /// Attempts to eat a target.
    /// </summary>
    /// <param name="slime">The slime entity.</param>
    /// <param name="target">The target entity ID.</param>
    /// <returns>Returns false if the slime was unable to eat the target. Returns true otherwise.</returns>
    public bool TryEat(Entity<SlimeComponent?> slime, EntityUid target)
    {
        if (!Resolve(slime, ref slime.Comp, false)) return false;
        
        if (!_interaction.InRangeUnobstructed(slime.Owner, target, range: 0.75f)) return false;
        if (!TryComp<DamageableComponent>(target, out var mobState)) return false;

        if (!_damageableSystem.TryChangeDamage(target, slime.Comp.DamageOnEat, out var returnDamage, ignoreResistances: true)) return false;

        if (returnDamage.AnyPositive())
        {
            slime.Comp.Nutrition += slime.Comp.NutritionOnHit;
        }
        
        return true;
    }
}