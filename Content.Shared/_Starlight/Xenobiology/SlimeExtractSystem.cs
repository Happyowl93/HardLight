using Content.Shared._Starlight.Xenobiology.Potions;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;

namespace Content.Shared._Starlight.Xenobiology;

/// <summary>
/// Handles the general behavior of slime extracts
/// </summary>
public sealed class SlimeExtractSystem : EntitySystem
{
    [Dependency] private readonly EntityManager _entityManager = default!;
    [Dependency] private readonly SharedEntityEffectsSystem _entityEffectsSystem = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainerSystem = default!;

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

        while (query.MoveNext(out var uid, out var slimeExtractComponent))
        {
            if (slimeExtractComponent.RemainingUses <= 0) continue;
            
            slimeExtractComponent.TimeSinceLastInject += frameTime;
            if (!_solutionContainerSystem.TryGetSolution(uid, slimeExtractComponent.ContainerName, out var solcom, out var currentSolution)) continue;
            var shouldDelete = false;
            var shouldExhaust = false;
            foreach (var reaction in slimeExtractComponent.ExtractReactions)
            {
                if (slimeExtractComponent.TimeSinceLastInject >= reaction.Delay && IsSolutionRequirementFulfilled(reaction.Requirements, currentSolution))
                {
                    var minimumScalingFactor = FindMinimumScalingFactor(reaction.Requirements, currentSolution);
                    foreach (var requirement in reaction.Requirements)
                    {
                        _solutionContainerSystem.RemoveReagent(solcom.Value, requirement.Reagent, minimumScalingFactor * requirement.Quantity);
                    }
                    foreach (var effect in reaction.Effects)
                    {
                        var factor = (minimumScalingFactor * effect.ScalingFactor) + effect.ScalingOffset;
                        _entityEffectsSystem.TryApplyEffect(uid, effect.Effect, factor.Float());
                    }
                    if (reaction.ShouldDelete)
                    {
                        shouldDelete = true;
                    }
                    shouldExhaust = true;
                }
            }

            if (shouldExhaust)
            {
                slimeExtractComponent.RemainingUses -= 1;
                slimeExtractComponent.TimeSinceLastInject = 0F;
            }
            if (shouldDelete && slimeExtractComponent.RemainingUses <= 0)
                _entityManager.PredictedQueueDeleteEntity(uid);
        }
    }
    
    public bool IsSolutionRequirementFulfilled(Solution requiredSolution, Solution currentSolution)
    {
        foreach (var req in requiredSolution.Contents)
        {
            if (!currentSolution.TryGetReagentQuantity(req.Reagent, out var amount)) return false;
            if (amount < req.Quantity) return false;
        }
        
        return true;
    }

    public FixedPoint2 FindMinimumScalingFactor(Solution requiredSolution, Solution currentSolution)
    {
        var minimumScalingFactor = FixedPoint2.MaxValue;
        foreach (var req in requiredSolution.Contents)
        {
            if (!currentSolution.TryGetReagentQuantity(req.Reagent, out var amount)) return 0.0;
            minimumScalingFactor = FixedPoint2.Min(minimumScalingFactor, amount/req.Quantity);
        }
        return minimumScalingFactor;
    }

    private void OnSolutionChanged(Entity<SlimeExtractComponent> entity, ref SolutionContainerChangedEvent args)
    {
        entity.Comp.TimeSinceLastInject = 0F;
    }
}