using Content.Server._Starlight.GameTicking.Rules.Components;
using Content.Server.GameTicking;
using Content.Server.Ghost.Roles.Events;
using Content.Server.Mind;
using Content.Shared._Starlight.Terminator;
using Robust.Shared.Prototypes;

namespace Content.Server._Starlight.Terminator;

public sealed partial class TerminatorSystem : EntitySystem
{
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly GameTicker _game = default!;

    private EntProtoId SpawnRulePrototype = "TerminatorSpawn";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TerminatorComponent, GhostRoleSpawnerUsedEvent>(OnSpawned);
    }

    private void OnSpawned(EntityUid uid, TerminatorComponent terminator, GhostRoleSpawnerUsedEvent args)
    {
        return;
    }

    public bool CreateTerminator(EntityUid target)
    {
        var uid = _game.AddGameRule(SpawnRulePrototype);
        var comp = EnsureComp<TerminatorRuleComponent>(uid);

        if (!_mind.TryGetMind(target, out var mindId, out var mind)) return false;
        comp.Target = mindId;
        _game.StartGameRule(uid);
        return true;
    }
}