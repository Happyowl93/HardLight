using Content.Shared.Coordinates;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Robust.Shared.Random;

namespace Content.Server._Starlight.Xenobiology;

/// <summary>
/// Handles the general behavior of slimes.
/// </summary>
public sealed class SlimeSystem : EntitySystem
{
    [Dependency] private readonly EntityManager _entityManager = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;

    public List<SlimeSplitRecord> SlimeSplitRecords = new();
    public List<EntityUid> SlimesToDelete = new();

    public sealed class SlimeSplitRecord(Entity<Shared._Starlight.Xenobiology.SlimeComponent?> slime, int splitAmount)
    {
        public Entity<Shared._Starlight.Xenobiology.SlimeComponent?> Slime = slime;
        public int SplitAmount = splitAmount;
    }
    
    /// <inheritdoc />
    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var query = EntityQueryEnumerator<Shared._Starlight.Xenobiology.SlimeComponent>();
        
        while (query.MoveNext(out var uid, out var slime))
        {
            slime.Nutrition = FixedPoint2.Max(slime.Nutrition + (frameTime * slime.NutritionChangePerSecond), 0);
        }

        foreach (var record in SlimeSplitRecords)
        {
            TrySplitSlime(record.Slime, record.SplitAmount);
        }
        
        foreach (var slime in SlimesToDelete)
        {
            _entityManager.QueueDeleteEntity(slime);
        }
    }
    
    /// <summary>
    /// Attempts to eat a target.
    /// </summary>
    /// <param name="slime">The slime entity.</param>
    /// <param name="target">The target entity ID.</param>
    /// <returns>Returns false if the slime was unable to eat the target. Returns true otherwise.</returns>
    public bool TryEat(Entity<Shared._Starlight.Xenobiology.SlimeComponent?> slime, EntityUid target)
    {
        if (!Resolve(slime, ref slime.Comp, false)) return false;
        
        if (!_interaction.InRangeUnobstructed(slime.Owner, target, range: 0.75f)) return false;
        if (!TryComp<DamageableComponent>(target, out var damage)) return false;
        
        if (!_damageableSystem.TryChangeDamage(target, slime.Comp.DamageOnEat, out var returnDamage, ignoreResistances: true)) return false;

        if (returnDamage.AnyPositive())
        {
            slime.Comp.Nutrition += slime.Comp.NutritionOnHit;
        }
        
        return true;
    }

    public bool TrySplitSlime(Entity<Shared._Starlight.Xenobiology.SlimeComponent?> slime, int split_amount)
    {
        if (!Resolve(slime, ref slime.Comp, false)) return false;
        var newNutrition = slime.Comp.Nutrition / split_amount;
        Random random = new Random();
        for (int i = 0; i < split_amount; i++)
        {
            string protoName;
            if (random.NextFloat() < slime.Comp.MutationChance && slime.Comp.SplitIntoMutation.Count > 0)
            {
                var randomIndex = random.Next(slime.Comp.SplitIntoMutation.Count);
                protoName = slime.Comp.SplitIntoMutation[randomIndex];
            }
            else
            {
                protoName = slime.Comp.SplitInto;
            }
            var split = _entityManager.SpawnAtPosition(protoName, slime.Owner.ToCoordinates());
            Shared._Starlight.Xenobiology.SlimeComponent? comp = null;
            if (Resolve(split, ref comp))
                comp.Nutrition = newNutrition;
            else return false;
        }
        SlimesToDelete.Add(slime.Owner);
        return true;
    }

    public bool QueueSlimeSplit(Entity<Shared._Starlight.Xenobiology.SlimeComponent?> slime, int splitAmount)
    {
        SlimeSplitRecords.Add(new(slime, splitAmount));
        return true;
    }
}