using System.Linq;
using Content.Server._Starlight.GameTicking.Rules.Components;
using Content.Server.GameTicking.Rules;
using Content.Server.StationEvents.Events;
using Content.Shared.GameTicking.Components;
using Content.Shared.Tag;
using Robust.Shared.EntitySerialization;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;

namespace Content.Server._Starlight.GameTicking.Rules;

public sealed class LoadTheDarkRuleSystem : StationEventSystem<LoadTheDarkRuleComponent>
{
    [Dependency] private readonly MapLoaderSystem _mapLoader = default!;
    [Dependency] private readonly IMapManager _maps = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    private static readonly ProtoId<TagPrototype> _theDarkMapTag = "TheDarkMap";

    protected override void Added(EntityUid uid, LoadTheDarkRuleComponent comp, GameRuleComponent rule, GameRuleAddedEvent args)
    {
        MapId mapId;
        IReadOnlyList<EntityUid> grids;

        var query = EntityQueryEnumerator<MapComponent>();
        while (query.MoveNext(out var mapuid, out var mapcomp))
        {
            if (mapcomp.MapPaused)
                continue;

            if (_tag.HasTag(mapuid, _theDarkMapTag))
            {
                mapId = mapcomp.MapId;

                var gridSet = _maps.GetAllGrids(mapcomp.MapId).ToList();
                grids = gridSet.Select(x => x.Owner).ToList();

                var ev = new RuleLoadedGridsEvent(mapId, grids);
                RaiseLocalEvent(uid, ref ev);

                base.Added(uid, comp, rule, args);

                return;
            }
        }

        if (comp.MapPath is { } path)
        {
            var opts = DeserializationOptions.Default with { InitializeMaps = true };
            if (!_mapLoader.TryLoadMap(path, out var map, out var gridSet, opts))
            {
                Log.Error($"Failed to load map from {path}!");
                ForceEndSelf(uid, rule);
                return;
            }

            grids = gridSet.Select(x => x.Owner).ToList();
            mapId = map.Value.Comp.MapId;
            _tag.AddTag(map.Value, _theDarkMapTag);
        }
        else
        {
            Log.Error($"No valid map prototype or map path associated with the rule {ToPrettyString(uid)}");
            ForceEndSelf(uid, rule);
            return;
        }

        var ev2 = new RuleLoadedGridsEvent(mapId, grids);
        RaiseLocalEvent(uid, ref ev2);

        base.Added(uid, comp, rule, args);
    }
}
