using System.Threading;
using System.Threading.Tasks;
using Content.Server._Starlight.Xenobiology;
using Content.Server.NPC;
using Content.Server.NPC.HTN.PrimitiveTasks;
using Content.Server.NPC.Pathfinding;
using Content.Shared._Starlight.Xenobiology;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Components;
using Robust.Shared.Prototypes;

namespace Content.Server._Starlight.NPC.HTN.PrimitiveTasks.Operators.Xenobiology;

public sealed partial class SlimeFindEdibleTargetOperator : HTNOperator
{
    /*
     * This class collects known edible targets for the hive mind.
     * With this, slimes should bunch up around nearby edible targets, and not aimlessly and separately search for targets.
     */
    
    [Dependency] private readonly IEntityManager _entManager = default!;

    private SlimeBrainSystem _slimeBrainSystem = default!;
    private EntityLookupSystem _lookup = default!;
    private PathfindingSystem _pathfinding = default!;
    
    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _slimeBrainSystem = sysManager.GetEntitySystem<SlimeBrainSystem>();
        _lookup = sysManager.GetEntitySystem<EntityLookupSystem>();
        _pathfinding = sysManager.GetEntitySystem<PathfindingSystem>();
    }
    
    public override async Task<(bool Valid, Dictionary<string, object>? Effects)> Plan(NPCBlackboard blackboard,
        CancellationToken cancelToken)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        
        if (!_entManager.TryGetComponent<SlimeComponent>(owner, out var slime))
            return (false, null);
        
        foreach (var entity in _lookup.GetEntitiesInRange(owner, _slimeBrainSystem.FoodSearchRange))
        {
            if (!_slimeBrainSystem.IsEdibleBySlimeTest(entity)) continue;
                
            var pathRange = SharedInteractionSystem.InteractionRange - 1f;
            var path = await _pathfinding.GetPath(owner, entity, pathRange, cancelToken);

            if (path.Result == PathResult.NoPath)
                continue;
                
            _slimeBrainSystem.TargetFood.Add(entity);

            return (true, null);
        }
        
        return (false, null);
    }
}