using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Coordinates;
using Content.Shared.EntityEffects;

namespace Content.Shared.Xenobiology;

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
            if (!_solutionContainerSystem.TryGetSolution(uid, slimeExtractComponent.ContainerName, out _, out var currentSolution)) continue;
            foreach (var reaction in slimeExtractComponent.ExtractReactions)
            {
                if (IsSolutionRequirementFulfilled(reaction.Requirements, currentSolution))
                {
                    foreach (var effect in reaction.Effects)
                    {
                        _entityEffectsSystem.TryApplyEffect(uid, effect);
                    }
                    currentSolution.RemoveAllSolution();
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
}