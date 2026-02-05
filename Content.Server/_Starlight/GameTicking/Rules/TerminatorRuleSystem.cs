using Content.Server._Starlight.GameTicking.Rules.Components;
using Content.Server.Antag;
using Content.Server.GameTicking.Rules;
using Content.Server.Objectives.Components;
using Content.Shared._Starlight.Terminator;
using Robust.Shared.Prototypes;

namespace Content.Server._Starlight.GameTicking.Rules;

public sealed partial class TerminatorRuleSystem : GameRuleSystem<TerminatorRuleComponent>
{

    EntProtoId TerminatorEntityPrototype = "MobHumanTerminator";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TerminatorRuleComponent, AntagSelectEntityEvent>(OnAntagSelectEntity);
    }

    private void OnAntagSelectEntity(Entity<TerminatorRuleComponent> ent, ref AntagSelectEntityEvent args)
    {
        if (args.Session?.AttachedEntity is not { } spawner) return;

        if (ent.Comp.Target == null)
        {
            return; // todo random player for generic gamemode
        }

        var terminator = Spawn(TerminatorEntityPrototype);
        var targetOverride = EnsureComp<TargetOverrideComponent>(terminator);
        targetOverride.Target = ent.Comp.Target;
        var termComp = EnsureComp<TerminatorComponent>(terminator);
        termComp.TargetBody = ent.Comp.TargetBody;

        args.Entity = terminator;
    }
}