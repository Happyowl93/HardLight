using Content.Server._Starlight.Xenobiology;
using Content.Server.NPC;
using Content.Server.NPC.HTN.Preconditions;
using Content.Shared._Starlight.Xenobiology;
using Content.Shared.FixedPoint;

namespace Content.Server._Starlight.NPC.HTN.Preconditions;

public sealed partial class SlimeEnoughNutritionToSplitPrecondition : HTNPrecondition
{
    [Dependency] private readonly IEntityManager _entMan = default!;
    private SlimeSystem _slime = default!;

    /// <summary>
    /// The amount of nutrition required for the slime to split.
    /// </summary>
    [DataField("splitThreshold", required: true)]
    public FixedPoint2 SplitThreshold = 0;
    
    public override bool IsMet(NPCBlackboard blackboard)
    {
        if (!blackboard.TryGetValue<EntityUid>(NPCBlackboard.Owner, out var owner, _entMan))
        {
            return false;
        }

        return _entMan.TryGetComponent<SlimeComponent>(owner, out var slime) && slime.Nutrition >= SplitThreshold;
    }
}