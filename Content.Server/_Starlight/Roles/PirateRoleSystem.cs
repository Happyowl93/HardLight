using Content.Server.Mind;
using Content.Server.Roles;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;

namespace Content.Server._Starlight.Roles;

public sealed class PirateRoleSystem : EntitySystem
{
    [Dependency] private readonly MindSystem _mind = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PirateRoleComponent, ComponentStartup>(OnStartup);
    }

    private void OnStartup(EntityUid uid, PirateRoleComponent comp, ComponentStartup args)
    {
        // Mind role entities are children of the mind entity.
        var mindId = Transform(uid).ParentUid;
        if (!TryComp<MindComponent>(mindId, out var mind))
            return;

        _mind.TryAddObjective(mindId, mind, "PirateSurviveObjective");
        _mind.TryAddObjective(mindId, mind, "PirateFollowCaptainObjective");
    }
}
