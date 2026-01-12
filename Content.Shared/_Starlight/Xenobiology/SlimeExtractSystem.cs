using Content.Shared._Starlight.Utility;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
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
    [Dependency] private readonly TimedEventSystem _timedEventSystem = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SlimeExtractComponent, SolutionContainerChangedEvent>(OnSolutionChanged);
        SubscribeLocalEvent<SlimeExtractComponent, EntityPausedEvent>(OnPaused);
        SubscribeLocalEvent<SlimeExtractComponent, EntityUnpausedEvent>(OnUnpaused);
        SubscribeLocalEvent<SlimeExtractComponent, TriggerReactionEvent>(OnTriggerReaction);
    }
    
    public bool IsSolutionRequirementFulfilled(Dictionary<ProtoId<ReagentPrototype>, FixedPoint2> requiredSolution, Solution currentSolution)
    {
        foreach (var req in requiredSolution)
        {
            var amount = currentSolution.GetTotalPrototypeQuantity(req.Key);
            if (amount < req.Value) return false;
        }
        
        return true;
    }

    public FixedPoint2 FindMinimumScalingFactor(Dictionary<ProtoId<ReagentPrototype>, FixedPoint2>requiredSolution, Solution currentSolution)
    {
        var minimumScalingFactor = FixedPoint2.MaxValue;
        foreach (var req in requiredSolution)
        {
            var amount = currentSolution.GetTotalPrototypeQuantity(req.Key);
            minimumScalingFactor = FixedPoint2.Min(minimumScalingFactor, amount/req.Value);
        }
        return minimumScalingFactor;
    }

    private void OnSolutionChanged(Entity<SlimeExtractComponent> entity, ref SolutionContainerChangedEvent args)
    {
        for (var i = 0; i <  entity.Comp.ExtractReactions.Count; i++)
        {
            var reaction = entity.Comp.ExtractReactions[i];
            if (IsSolutionRequirementFulfilled(reaction.Requirements, args.Solution))
            {
                if (!entity.Comp.CurrentlyPaused)
                {
                    if (reaction.ActivationGuid.HasValue) // If we already have an event scheduled, restart it
                        _timedEventSystem.TryDeleteEvent(reaction.ActivationGuid.Value, out _);
                    var guid = _timedEventSystem.ScheduleEvent(entity.Owner, new TriggerReactionEvent(i), _gameTiming.CurTime + reaction.Delay);
                    reaction.ActivationGuid = guid;
                }
                else
                {
                    reaction.PausedEvent = new PausedEvent(new TriggerReactionEvent(i), _gameTiming.CurTime + reaction.Delay);
                }
            }
            else
            {
                if (reaction.ActivationGuid.HasValue)
                {
                    _timedEventSystem.TryDeleteEvent(reaction.ActivationGuid.Value, out _);
                    reaction.ActivationGuid = null;
                }
            }
        }
    }

    private void OnPaused(Entity<SlimeExtractComponent> entity, ref EntityPausedEvent args)
    {
        entity.Comp.CurrentlyPaused = true;
        foreach (var reaction in entity.Comp.ExtractReactions)
        {
            if (reaction.ActivationGuid.HasValue)
            {
                if (_timedEventSystem.TryDeleteEvent(reaction.ActivationGuid.Value, out var record))
                {
                    reaction.PausedEvent = new PausedEvent(record.Args, record.TimeStamp);
                    reaction.ActivationGuid = null;
                }
            }
        }
    }
    
    private void OnUnpaused(Entity<SlimeExtractComponent> entity, ref EntityUnpausedEvent args)
    {
        entity.Comp.CurrentlyPaused = false;
        for (var i = 0; i <  entity.Comp.ExtractReactions.Count; i++)
        {
            var reaction = entity.Comp.ExtractReactions[i];
            if (reaction.PausedEvent is not null)
            {
                var guid = _timedEventSystem.ScheduleEvent(entity.Owner, reaction.PausedEvent.ExtractReactionIndex, reaction.PausedEvent.TimeStamp + args.PausedTime);
                reaction.ActivationGuid = guid;
            }
        }
    }

    private void OnTriggerReaction(Entity<SlimeExtractComponent> entity, ref TriggerReactionEvent args)
    {
        if (!_solutionContainerSystem.TryGetSolution(entity.Owner, entity.Comp.ContainerName, out var solutionComponent, out var currentSolution)) return;
        var reaction = entity.Comp.ExtractReactions[args.ExtractReactionIndex];
        if (IsSolutionRequirementFulfilled(reaction.Requirements, currentSolution))
        {
            var minimumScalingFactor = FindMinimumScalingFactor(reaction.Requirements, currentSolution);
            foreach (var requirement in reaction.Requirements)
            {
                _solutionContainerSystem.RemoveReagent(solutionComponent.Value, new ReagentId(requirement.Key, null), minimumScalingFactor * requirement.Value);
            }
            foreach (var effect in reaction.Effects)
            {
                var factor = (minimumScalingFactor * effect.ScalingFactor) + effect.ScalingOffset;
                _entityEffectsSystem.TryApplyEffect(entity.Owner, effect.Effect, factor.Float());
            }
            entity.Comp.RemainingUses -= 1;
            if (reaction.ShouldDelete && entity.Comp.RemainingUses <= 0)
            {
                PredictedQueueDel(entity.Owner);
            }
        }
    }
}

[Serializable, NetSerializable]
public record TriggerReactionEvent(int ExtractReactionIndex);

[Serializable, NetSerializable]
public record PausedEvent(object ExtractReactionIndex, TimeSpan TimeStamp);