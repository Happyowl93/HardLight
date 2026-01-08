using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Robust.Shared.Timing;

namespace Content.Shared._Starlight.Xenobiology;

/// <summary>
/// Handles the general behavior of slime extracts
/// </summary>
public sealed class SlimeExtractSystem : EntitySystem
{
    [Dependency] private readonly EntityManager _entityManager = default!;
    [Dependency] private readonly SharedEntityEffectsSystem _entityEffectsSystem = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainerSystem = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SlimeExtractComponent, SolutionContainerChangedEvent>(OnSolutionChanged);
    }
    
    /// <inheritdoc />
    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var query = EntityQueryEnumerator<SlimeExtractComponent>();

        List<ApplyEffectRecord> applyEffectRecords = new();
        List<DeleteRecord> deleteRecords = new();

        while (query.MoveNext(out var uid, out var slimeExtractComponent))
        {
            if (slimeExtractComponent.RemainingUses <= 0) continue;
            
            if (!_solutionContainerSystem.TryGetSolution(uid, slimeExtractComponent.ContainerName, out var solcom, out var currentSolution)) continue;
            var shouldDelete = false;
            var shouldExhaust = false;
            foreach (var reaction in slimeExtractComponent.ExtractReactions)
            {
                if ((reaction.ActivationMoment.HasValue && reaction.ActivationMoment.Value <= _gameTiming.CurTime) && IsSolutionRequirementFulfilled(reaction.Requirements, currentSolution))
                {
                    var minimumScalingFactor = FindMinimumScalingFactor(reaction.Requirements, currentSolution);
                    foreach (var requirement in reaction.Requirements)
                    {
                        _solutionContainerSystem.RemoveReagent(solcom.Value, new ReagentId(requirement.Key, null), minimumScalingFactor * requirement.Value);
                    }
                    foreach (var effect in reaction.Effects)
                    {
                        // Need to defer the application of effects in order to avoid modifying the query's collection
                        // Because the rainbow extract can summon other extracts
                        var factor = (minimumScalingFactor * effect.ScalingFactor) + effect.ScalingOffset;
                        applyEffectRecords.Add(new(uid, effect.Effect, factor.Float()));
                    }
                    if (reaction.ShouldDelete)
                    {
                        shouldDelete = true;
                    }
                    shouldExhaust = true;
                    reaction.ActivationMoment = _gameTiming.CurTime + reaction.Delay;
                }
            }

            if (shouldExhaust)
            {
                slimeExtractComponent.RemainingUses -= 1;
            }
            if (shouldDelete && slimeExtractComponent.RemainingUses <= 0)
                deleteRecords.Add(new(uid));
        }
        
        foreach (var applyEffectRecord in applyEffectRecords)
            _entityEffectsSystem.TryApplyEffect(applyEffectRecord._entityUid, applyEffectRecord._entityEffect, applyEffectRecord._scale);
        foreach (var deleteRecord in deleteRecords)
            _entityManager.PredictedQueueDeleteEntity(deleteRecord._entityUid);
    }
    
    public bool IsSolutionRequirementFulfilled(Dictionary<string, FixedPoint2> requiredSolution, Solution currentSolution)
    {
        foreach (var req in requiredSolution)
        {
            if (!currentSolution.TryGetReagentQuantity(new ReagentId(req.Key, null), out var amount)) return false;
            if (amount < req.Value) return false;
        }
        
        return true;
    }

    public FixedPoint2 FindMinimumScalingFactor(Dictionary<string, FixedPoint2> requiredSolution, Solution currentSolution)
    {
        var minimumScalingFactor = FixedPoint2.MaxValue;
        foreach (var req in requiredSolution)
        {
            if (!currentSolution.TryGetReagentQuantity(new ReagentId(req.Key, null), out var amount)) return 0.0;
            minimumScalingFactor = FixedPoint2.Min(minimumScalingFactor, amount/req.Value);
        }
        return minimumScalingFactor;
    }

    private void OnSolutionChanged(Entity<SlimeExtractComponent> entity, ref SolutionContainerChangedEvent args)
    {
        foreach (var reaction in entity.Comp.ExtractReactions)
        {
            if (IsSolutionRequirementFulfilled(reaction.Requirements, args.Solution))
            {
                reaction.ActivationMoment = _gameTiming.CurTime + reaction.Delay;
            }
            else
            {
                reaction.ActivationMoment = null;
            }
        }
    }
}

internal record DeleteRecord
{
    public EntityUid _entityUid;
    
    public DeleteRecord(EntityUid entityUid)
    {
        _entityUid = entityUid;
    }
}

internal record ApplyEffectRecord
{
    public EntityUid _entityUid;
    public EntityEffect _entityEffect;
    public float _scale;

    public ApplyEffectRecord(EntityUid entityUid, EntityEffect entityEffect, float scale)
    {
        _entityUid = entityUid;
        _entityEffect = entityEffect;
        _scale = scale;
    }
}