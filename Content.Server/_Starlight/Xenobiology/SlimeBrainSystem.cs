using Content.Server.NPC.Pathfinding;
using Content.Shared._Starlight.Xenobiology;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Components;
using Robust.Shared.Prototypes;

namespace Content.Server._Starlight.Xenobiology;

/*
 * Slimes are a hive mind! Why? Because it's easier to program, there will only be one so we don't have to worry about a big update loop, and its hilarious.
 * This is going to be primarily event based and work with HTN, with the following design philosophy:
 * Slimes will each have Moods that determine their overall behavior. Moods inform the HTN as to which branch to take when deciding slime behavior.
 * For instance, calm slimes will slowly wander around and eat anything nearby tagged with Monkey, while hungry slimes will eat anything biological.
 * The slime hivemind holds memories. Memories are globally known dictionaries holding information relevant for slimes. Memories are shared by all slimes.
 * The most basic memory is opinion. Each entry is an EntityUid paired with a fixedpoint ranging from -100 to 100, with -100 being hatred and 100 being love.
 * Slimes, all slimes, will take orders from loved individuals and will KOS any hated individuals.
 * Opinion doesn't decay to any resting state. You make friends with slimes by feeding them and make enemies with slimes by attacking them or anyone they love.
 * Don't be surprised if shooting the Xenobiologist causes The Slime Swarm to brutalize you.
 * Memories dictate moods and their transitions. For instance, a slime will go from Calm to Hostile if there is an entity nearby with -50 opinion.
 * And more interestingly, a slime will enter go from Hungry to Search if commanded by someone with 100 opinion.
 * Slimes have poor situational awareness and will only switch their moods on an UpdateMood event, usually raised by HTN.
 * Because updating mood every tick sounds like a performance nightmare.
 */

public sealed class SlimeBrainSystem : EntitySystem
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly SlimeBrainSystem _slimeBrainSystem = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly PathfindingSystem _pathfinding = default!;
    
    public HashSet<EntityUid> TargetFood = new();

    /// <summary>
    /// How far to look for food at each slime.
    /// </summary>
    public readonly float FoodSearchRange = 5F;
    
    /// <summary>
    /// The amount of damage below which the slime brain will consider the target to be edible.
    /// </summary>
    public readonly FixedPoint2 TargetDamageThreshold = 100;
    
    /// <summary>
    /// If not null, will only allow slimes to eat entities with the specified damage container.
    /// If null, will make slimes try to eat everything.
    /// </summary>
    public readonly ProtoId<DamageContainerPrototype>? OnlyTarget = "Biological";

    public bool IsEdibleBySlimeTest(EntityUid entity)
    {
        var damageQuery = _entManager.GetEntityQuery<DamageableComponent>();
        var slimeQuery = _entManager.GetEntityQuery<SlimeComponent>();
        var mobStateQuery = _entManager.GetEntityQuery<MobStateComponent>();
        
        // Don't cannibalize other slimes
        if (slimeQuery.HasComponent(entity)) return false;

        if (!damageQuery.TryGetComponent(entity, out var damage)) return false;
        
        // Don't target entities that aren't mobs
        if (!mobStateQuery.HasComponent(entity)) return false;

        // Don't target entities in the wrong damage group
        if (OnlyTarget.HasValue)
            if (!(damage.DamageContainerID.HasValue && damage.DamageContainerID.Value == OnlyTarget.Value))
                return false;

        // Only target entities that are not damaged enough
        if (!(damage.TotalDamage < TargetDamageThreshold)) return false;

        return true;
    }
}