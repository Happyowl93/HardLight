using System.Diagnostics;
using Content.Server.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.EntityTable;
using Content.Shared.EntityTable.Conditions;
using Content.Shared.GameTicking.Components;
using Content.Shared.GameTicking.Rules;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.GameTicking.Rules;

/// <summary>
/// A system to handle one-shot dynamic rules, with slightly different add/start semantics.
/// </summary>
public sealed class SubRuleSystem : GameRuleSystem<SubRuleComponent>
{
    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly EntityTableSystem _entityTable = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    protected override void Added(EntityUid uid, SubRuleComponent component, GameRuleComponent gameRule, GameRuleAddedEvent args)
    {
        base.Added(uid, component, gameRule, args);

        component.Budget = _random.Next(component.BudgetMin, component.BudgetMax);;

        AddChildRules((uid, component));
    }

    protected override void Started(EntityUid uid, SubRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        StartChildRules((uid, component));
    }

    protected override void Ended(EntityUid uid, SubRuleComponent component, GameRuleComponent gameRule, GameRuleEndedEvent args)
    {
        base.Ended(uid, component, gameRule, args);

        foreach (var rule in component.Rules)
        {
            GameTicker.EndGameRule(rule);
        }
    }

    /// <summary>
    /// Generates and returns a list of randomly selected,
    /// valid rules to spawn based on <see cref="SubRuleComponent.Table"/>.
    /// </summary>
    private IEnumerable<EntProtoId> GetRuleSpawns(Entity<SubRuleComponent> entity)
    {
        var ctx = new EntityTableContext(new Dictionary<string, object>
        {
            { HasBudgetCondition.BudgetContextKey, entity.Comp.Budget },
        });

        return _entityTable.GetSpawns(entity.Comp.Table, ctx: ctx);
    }

    /// <summary>
    /// Uses the definition of the component to create and add sub rules, but not yet start them.
    /// </summary>
    /// <returns>
    /// Returns a list of the rules that were added.
    /// </returns>
    private List<EntityUid> AddChildRules(Entity<SubRuleComponent> entity)
    {
        var addedRules = new List<EntityUid>();

        foreach (var rule in GetRuleSpawns(entity))
        {
            var ruleUid = GameTicker.AddGameRule(rule, entity.Comp.Rules);

            addedRules.Add(ruleUid);

            if (TryComp<DynamicRuleCostComponent>(ruleUid, out var cost))
            {
                entity.Comp.Budget -= cost.Cost;
                _adminLog.Add(LogType.EventRan, LogImpact.High, $"{ToPrettyString(entity)} ran rule {ToPrettyString(ruleUid)} with cost {cost.Cost} on budget {entity.Comp.Budget}.");
            }
            else
            {
                _adminLog.Add(LogType.EventRan, LogImpact.High, $"{ToPrettyString(entity)} ran rule {ToPrettyString(ruleUid)} which had no cost.");
            }
        }

        return addedRules;
    }

    /// <summary>
    /// Starts rules already added to the component.
    /// </summary>
    /// <returns>
    /// Returns a list of the rules that were started.
    /// </returns>
    private List<EntityUid> StartChildRules(Entity<SubRuleComponent> entity)
    {
        var startedRules = new List<EntityUid>();

        foreach (var ruleUid in entity.Comp.Rules)
        {
            var res = GameTicker.StartGameRule(ruleUid);
            Debug.Assert(res);
        }

        return startedRules;
    }
}
