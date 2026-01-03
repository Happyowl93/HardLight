using Content.Shared._Starlight.Xenobiology;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;

namespace Content.Server._Starlight.Xenobiology;

/// <summary>
/// Handles the general behavior of slime extracts
/// </summary>
public sealed class SlimeExtractSystem : EntitySystem
{
    [Dependency] private readonly EntityManager _entityManager = default!;
    [Dependency] private readonly SharedEntityEffectsSystem _entityEffectsSystem = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainerSystem = default!;

    /// <inheritdoc />
    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var query = EntityQueryEnumerator<SlimeExtractComponent>();

        while (query.MoveNext(out var uid, out var slimeExtractComponent))
        {
            if (!_solutionContainerSystem.TryGetSolution(uid, slimeExtractComponent.ContainerName, out var solcom, out var currentSolution)) continue;
            foreach (var reaction in slimeExtractComponent.ExtractReactions)
            {
                if (IsSolutionRequirementFulfilled(reaction.Requirements, currentSolution))
                {
                    var minimumScalingFactor = FindMinimumScalingFactor(reaction.Requirements, currentSolution);
                    foreach (var effect in reaction.Effects)
                    {
                        var factor = (minimumScalingFactor * effect.ScalingFactor) + effect.ScalingOffset;
                        _entityEffectsSystem.TryApplyEffect(uid, effect.Effect, factor.Float());
                    }
                    foreach (var requirement in reaction.Requirements)
                    {
                        _solutionContainerSystem.RemoveReagent(solcom.Value, requirement.Reagent, minimumScalingFactor * requirement.Quantity);
                    }
                    _entityManager.QueueDeleteEntity(uid);
                    break;
                }
            }
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
}