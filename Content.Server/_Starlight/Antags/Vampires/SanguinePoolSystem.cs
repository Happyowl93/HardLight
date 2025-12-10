using Content.Server.Polymorph.Components;
using Content.Server.Polymorph.Systems;
using Content.Shared._Starlight.Antags.Vampires.Components;
using Content.Shared._Starlight.Antags.Vampires.Systems;
using Content.Shared.Maps;
using Robust.Shared.Map.Components;

namespace Content.Server._Starlight.Antags.Vampires;

public sealed class SanguinePoolSystem : SharedSanguinePoolSystem
{
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly TurfSystem _turf = default!;
    [Dependency] private readonly PolymorphSystem _polymorph = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<SanguinePoolComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var comp, out var xform))
        {
            if (ShouldForceRevert(uid, xform))
                continue;

            if (comp.TrailPrototype == null)
                continue;

            comp.Accumulator += frameTime;
            if (comp.Accumulator < comp.TrailInterval)
                continue;

            comp.Accumulator -= comp.TrailInterval;

            Spawn(comp.TrailPrototype, xform.Coordinates);
        }
    }

    private bool ShouldForceRevert(EntityUid uid, TransformComponent xform)
    {
        var gridUid = xform.GridUid;
        var inSpace = gridUid == null;

        if (!inSpace && gridUid != null)
        {
            if (!TryComp(gridUid.Value, out MapGridComponent? grid) ||
                !_map.TryGetTileRef(gridUid.Value, grid, xform.Coordinates, out var tileRef) ||
                _turf.IsSpace(tileRef))
            {
                inSpace = true;
            }
        }

        if (!inSpace)
            return false;

        if (TryComp<PolymorphedEntityComponent>(uid, out var polymorph))
            _polymorph.Revert((uid, polymorph));

        return true;
    }
}
