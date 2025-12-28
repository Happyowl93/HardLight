using Content.Server.Chat.Systems;
using Content.Shared.Chat;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Damage.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Silicons.Bots;
using Content.Shared.Xenobiology;
using Robust.Shared.Audio.Systems;

namespace Content.Server.NPC.HTN.PrimitiveTasks.Operators.Specific;

public sealed partial class SlimeEatOperator : HTNOperator
{
    [Dependency] private readonly IEntityManager _entMan = default!;
    private ChatSystem _chat = default!;
    private SlimeSystem _slime = default!;
    private SharedAudioSystem _audio = default!;
    private SharedInteractionSystem _interaction = default!;
    private SharedPopupSystem _popup = default!;
    private SharedSolutionContainerSystem _solutionContainer = default!;

    /// <summary>
    /// Target entity to eat.
    /// </summary>
    [DataField("targetKey", required: true)]
    public string TargetKey = string.Empty;

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _chat = sysManager.GetEntitySystem<ChatSystem>();
        _slime = sysManager.GetEntitySystem<SlimeSystem>();
        _audio = sysManager.GetEntitySystem<SharedAudioSystem>();
        _interaction = sysManager.GetEntitySystem<SharedInteractionSystem>();
        _popup = sysManager.GetEntitySystem<SharedPopupSystem>();
        _solutionContainer = sysManager.GetEntitySystem<SharedSolutionContainerSystem>();
    }

    public override void TaskShutdown(NPCBlackboard blackboard, HTNOperatorStatus status)
    {
        base.TaskShutdown(blackboard, status);
        blackboard.Remove<EntityUid>(TargetKey);
        blackboard.Remove<FixedPoint2>(SlimePickNearbyEdibleOperator.HungerThresholdKey);
        blackboard.Remove<string>(SlimePickNearbyEdibleOperator.TargetDamageTypeKey);
        blackboard.Remove<FixedPoint2>(SlimePickNearbyEdibleOperator.TargetDamageThresholdKey);
    }

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        if (!blackboard.TryGetValue<EntityUid>(TargetKey, out var target, _entMan) || _entMan.Deleted(target))
            return HTNOperatorStatus.Failed;
        
        if (!blackboard.TryGetValue<FixedPoint2>(SlimePickNearbyEdibleOperator.HungerThresholdKey, out var hungerThreshold, _entMan) ||
            !blackboard.TryGetValue<string>(SlimePickNearbyEdibleOperator.TargetDamageTypeKey, out var targetDamageType, _entMan) ||
            !blackboard.TryGetValue<FixedPoint2>(SlimePickNearbyEdibleOperator.TargetDamageThresholdKey, out var targetDamageThreshold, _entMan))
            return HTNOperatorStatus.Failed;

        if (!_entMan.TryGetComponent<SlimeComponent>(owner, out var slime))
            return HTNOperatorStatus.Failed;
        
        if (hungerThreshold <= slime.Nutrition)
            return HTNOperatorStatus.Failed;

        if (!_entMan.TryGetComponent<DamageableComponent>(target, out var damage))
            return HTNOperatorStatus.Failed;
        
        if (!damage.DamagePerGroup.TryGetValue(targetDamageType, out var targetDamage) || !(targetDamage <
                targetDamageThreshold))
            return HTNOperatorStatus.Failed;

        if (!_slime.TryEat((owner, slime), target))
            return HTNOperatorStatus.Failed;

        return HTNOperatorStatus.Finished;
    }
}