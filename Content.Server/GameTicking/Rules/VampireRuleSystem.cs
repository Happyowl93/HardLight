using Content.Server.Antag;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Mind;
using Content.Server.Objectives;
using Content.Server.Roles;
using Content.Server._Starlight.Antags.Vampires;
using Content.Shared.Roles;
using Content.Shared.Roles.Components;
using Content.Shared._Starlight.Antags.Vampires;
using Robust.Shared.Prototypes;
using System.Text;

namespace Content.Server.GameTicking.Rules;

public sealed partial class VampireRuleSystem : GameRuleSystem<VampireRuleComponent>
{
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly AntagSelectionSystem _antag = default!;
    [Dependency] private readonly SharedRoleSystem _role = default!;
    [Dependency] private readonly ObjectivesSystem _objective = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VampireRuleComponent, AfterAntagEntitySelectedEvent>(OnSelectAntag);
        SubscribeLocalEvent<VampireRuleComponent, ObjectivesTextPrependEvent>(OnTextPrepend);
    }

    private void OnSelectAntag(EntityUid uid, VampireRuleComponent comp, ref AfterAntagEntitySelectedEvent args)
        => MakeVampire(args.EntityUid, comp);

    public bool MakeVampire(EntityUid target, VampireRuleComponent rule)
    {
        if (!_mind.TryGetMind(target, out var mindId, out var mind))
            return false;

        if (TryComp(target, out MetaDataComponent? meta))
        {
            var name = meta?.EntityName ?? "Unknown";
            var briefing = Loc.GetString("vampire-role-greeting", ("name", name));
            _antag.SendBriefing(target, briefing, Color.Yellow, null);

            _role.MindHasRole<VampireRoleComponent>(mindId, out var vampRole);
            _role.MindHasRole<RoleBriefingComponent>(mindId, out var briefingComp);
            if (vampRole is not null && briefingComp is null)
            {
                AddComp<RoleBriefingComponent>(vampRole.Value.Owner);
                Comp<RoleBriefingComponent>(vampRole.Value.Owner).Briefing = briefing;
            }
        }

        EnsureComp<VampireComponent>(target);

        rule.VampireMinds.Add(mindId);

        foreach (var objective in rule.BaseObjectives)
            _mind.TryAddObjective(mindId, mind, objective);

        var rng = new Random();
        if (rule.EscapeObjectives.Count > 0)
        {
            var obj = rule.EscapeObjectives[rng.Next(rule.EscapeObjectives.Count)];
            _mind.TryAddObjective(mindId, mind, obj);
        }

        if (rule.StealObjectives.Count > 0)
        {
            var obj = rule.StealObjectives[rng.Next(rule.StealObjectives.Count)];
            _mind.TryAddObjective(mindId, mind, obj);
        }

        return true;
    }

    private void OnTextPrepend(EntityUid uid, VampireRuleComponent comp, ref ObjectivesTextPrependEvent args)
    {
        var mostDrainedName = string.Empty;
        var mostDrained = 0f;

        var query = EntityQueryEnumerator<VampireComponent>();
        while (query.MoveNext(out var vampUid, out var vamp))
        {
            if (!_mind.TryGetMind(vampUid, out var mindId, out var mind))
                continue;

            if (!TryComp(vampUid, out MetaDataComponent? meta))
                continue;

            if (vamp.TotalBlood > mostDrained)
            {
                mostDrained = vamp.TotalBlood;
                mostDrainedName = _objective.GetTitle((mindId, mind), meta.EntityName);
            }
        }

        var sb = new StringBuilder();
        sb.AppendLine(Loc.GetString($"roundend-prepend-vampire-drained{(!string.IsNullOrWhiteSpace(mostDrainedName) ? "-named" : "")}", ("name", mostDrainedName), ("number", mostDrained)));
        args.Text = sb.ToString();
    }
}
