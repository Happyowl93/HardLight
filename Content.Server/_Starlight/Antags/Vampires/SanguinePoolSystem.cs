using Content.Server.Polymorph.Components;
using Content.Server.Polymorph.Systems;
using Content.Shared._Starlight.Antags.Vampires.Components;
using Content.Shared._Starlight.Antags.Vampires.Systems;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Maps;
using Content.Shared.Fluids.Components;
using Robust.Shared.Map.Components;

namespace Content.Server._Starlight.Antags.Vampires;

public sealed class SanguinePoolSystem : SharedSanguinePoolSystem
{
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly TurfSystem _turf = default!;
    [Dependency] private readonly PolymorphSystem _polymorph = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;

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

            // Spawn more frequently: once per entered tile (but don't duplicate if the tile already has a blood puddle).
            if (xform.GridUid is not { } gridUid || !TryComp(gridUid, out MapGridComponent? gridComp))
                continue;

            var tile = _map.CoordinatesToTile(gridUid, gridComp, xform.Coordinates);
            if (comp.HasLastTrailTile && comp.LastTrailGrid == gridUid && comp.LastTrailTile == tile)
                continue;

            comp.LastTrailGrid = gridUid;
            comp.LastTrailTile = tile;
            comp.HasLastTrailTile = true;

            var tileCoords = _map.GridTileToLocal(gridUid, gridComp, tile);
            if (HasBloodPuddleNearby(tileCoords))
                continue;

            Spawn(comp.TrailPrototype, tileCoords);
        }
    }

    private bool HasBloodPuddleNearby(Robust.Shared.Map.EntityCoordinates coords)
    {
        foreach (var ent in _lookup.GetEntitiesInRange(coords, 0.45f, LookupFlags.Static | LookupFlags.Dynamic | LookupFlags.Sundries))
        {
            if (IsBloodPuddle(ent))
                return true;
        }

        return false;
    }

    private bool IsBloodPuddle(EntityUid uid)
    {
        if (!TryComp<PuddleComponent>(uid, out var puddle))
            return false;

        if (!_solution.TryGetSolution(uid, puddle.SolutionName, out _, out var solution))
            return false;

        return solution.ContainsReagent("Blood", null);
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
