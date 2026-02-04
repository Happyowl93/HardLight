using Content.Server.Ghost.Roles.Events;
using Content.Shared._Starlight.Terminator;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Server._Starlight.Terminator;

public sealed partial class TerminatorSystem : EntitySystem
{
    private EntProtoId SpawnPointPrototype = "SpawnPointGhostTerminator";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TerminatorComponent, GhostRoleSpawnerUsedEvent>(OnSpawned);
    }

    private void OnSpawned(EntityUid uid, TerminatorComponent terminator, GhostRoleSpawnerUsedEvent args)
    {
        if (!TryComp<TerminatorSpawnTargetComponent>(args.Spawner, out var spawnTarget)) return;

        terminator.Target = spawnTarget.Target;
    }

    public EntityUid CreateSpawner(EntityCoordinates coordinates, EntityUid target)
    {
        var uid = Spawn(SpawnPointPrototype, coordinates);
        var comp = EnsureComp<TerminatorSpawnTargetComponent>(uid);
        comp.Target = target;

        return uid;
    }
}