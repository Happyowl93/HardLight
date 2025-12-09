using System.Numerics;
using Content.Shared.Administration.Logs;
using Content.Shared.Damage.Components;
using Content.Shared.Database;
using Content.Shared.Weapons.Hitscan.Components;
using Content.Shared.Weapons.Hitscan.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Utility;

#region Starlight
using Content.Shared.Body.Components;
using Content.Shared.Mech.Components;
using Content.Shared.Weapons.Reflect;
using Robust.Shared.Prototypes;
#endregion Starlight

namespace Content.Shared.Weapons.Hitscan.Systems;

public sealed class HitscanBasicRaycastSystem : EntitySystem
{
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly ISharedAdminLogManager _log = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

#region Starlight
    [Dependency] private readonly IPrototypeManager _proto = default!;
#endregion Starlight

    private EntityQuery<HitscanBasicVisualsComponent> _visualsQuery;

    private EntityQuery<HitscanReflectComponent> _reflectQuery; // Starlight
    private EntityQuery<BloodstreamComponent> _bloodQuery; // Starlight

    public override void Initialize()
    {
        base.Initialize();

        _visualsQuery = GetEntityQuery<HitscanBasicVisualsComponent>();

        _reflectQuery = GetEntityQuery<HitscanReflectComponent>(); // Starlight

        SubscribeLocalEvent<HitscanBasicRaycastComponent, HitscanTraceEvent>(OnHitscanFired);
    }

    private void OnHitscanFired(Entity<HitscanBasicRaycastComponent> ent, ref HitscanTraceEvent args)
    {
        var shooter = args.Shooter ?? args.Gun;
        // Starlight start - handle the shooter being the mech, not the pilot
        if (shooter != null && TryComp<MechPilotComponent>(shooter, out var pilotA))
            shooter = pilotA.Mech;
        // Starlight end
        var mapCords = _transform.ToMapCoordinates(args.FromCoordinates);
        var ray = new CollisionRay(mapCords.Position, args.ShotDirection, (int) ent.Comp.CollisionMask);
        var rayCastResults = _physics.IntersectRay(mapCords.MapId, ray, ent.Comp.MaxDistance, shooter, false);

        var target = args.Target;
        // If you are in a container, use the raycast result
        // Otherwise:
        //  1.) Hit the first entity that you targeted.
        //  2.) Hit the first entity that doesn't require you to aim at it specifically to be hit.
        var result = _container.IsEntityOrParentInContainer(shooter)
            ? rayCastResults.FirstOrNull()
            : rayCastResults.FirstOrNull(hit => hit.HitEntity == target
                                                || CompOrNull<RequireProjectileTargetComponent>(hit.HitEntity)?.Active != true);

        var distanceTried = result?.Distance ?? ent.Comp.MaxDistance;

        // Do visuals without an event. They should always happen and putting it on the attempt event is weird!
        // If more stuff gets added here, it should probably be turned into an event.
        FireEffects(args.FromCoordinates, distanceTried, args.ShotDirection.ToAngle(), ent.Owner, result?.HitEntity); // Starlight - add HitEntity parameter

        // Admin logging
        if (result?.HitEntity != null)
        {
            _log.Add(LogType.HitScanHit,
                $"{ToPrettyString(shooter):user} hit {ToPrettyString(result.Value.HitEntity):target}"
                + $" using {ToPrettyString(args.Gun):entity}.");
        }

        var data = new HitscanRaycastFiredData
        {
            ShotDirection = args.ShotDirection,
            Gun = args.Gun,
            Shooter = args.Shooter,
            HitEntity = result?.HitEntity,
        };

        var attemptEvent = new AttemptHitscanRaycastFiredEvent { Data = data };
        RaiseLocalEvent(ent, ref attemptEvent);

        if (attemptEvent.Cancelled)
            return;

        var hitEvent = new HitscanRaycastFiredEvent { Data = data };
        RaiseLocalEvent(ent, ref hitEvent);
    }

    /// <summary>
    /// Create visual effects for the fired hitscan weapon.
    /// </summary>
    /// <param name="fromCoordinates">Location to start the effect.</param>
    /// <param name="distance">Distance of the hitscan shot.</param>
    /// <param name="shotAngle">Angle of the shot.</param>
    /// <param name="hitscanUid">The hitscan entity itself.</param>
    /// <param name="hitEntity">The entity hit by this hitscan.</param> // Starlight
    private void FireEffects(EntityCoordinates fromCoordinates, float distance, Angle shotAngle, EntityUid hitscanUid, EntityUid? hitEntity) // Starlight - add hitEntity parameter
    {
        if (distance == 0 || !_visualsQuery.TryComp(hitscanUid, out var vizComp))
            return;

        var sprites = new List<(NetCoordinates coordinates, Angle angle, SpriteSpecifier sprite, float scale)>();
        var fromXform = Transform(fromCoordinates.EntityId);

        // We'll get the effects relative to the grid / map of the firer
        // Look you could probably optimise this a bit with redundant transforms at this point.

        var gridUid = fromXform.GridUid;
        if (gridUid != fromCoordinates.EntityId && TryComp(gridUid, out TransformComponent? gridXform))
        {
            var (_, gridRot, gridInvMatrix) = _transform.GetWorldPositionRotationInvMatrix(gridXform);
            var map = _transform.ToMapCoordinates(fromCoordinates);
            fromCoordinates = new EntityCoordinates(gridUid.Value, Vector2.Transform(map.Position, gridInvMatrix));
            shotAngle -= gridRot;
        }
        else
        {
            shotAngle -= _transform.GetWorldRotation(fromXform);
        }

        if (distance >= 1f)
        {
            if (vizComp.MuzzleFlash != null)
            {
                var coords = fromCoordinates.Offset(shotAngle.ToVec().Normalized() / 2);
                var netCoords = GetNetCoordinates(coords);

                sprites.Add((netCoords, shotAngle, vizComp.MuzzleFlash, 1f));
            }

            if (vizComp.TravelFlash != null)
            {
                var coords = fromCoordinates.Offset(shotAngle.ToVec() * (distance + 0.5f) / 2);
                var netCoords = GetNetCoordinates(coords);

                sprites.Add((netCoords, shotAngle, vizComp.TravelFlash, distance - 1.5f));
            }
        }

        if (vizComp.ImpactFlash != null)
        {
            var coords = fromCoordinates.Offset(shotAngle.ToVec() * distance);
            var netCoords = GetNetCoordinates(coords);

            sprites.Add((netCoords, shotAngle.FlipPositive(), vizComp.ImpactFlash, 1f));
        }

        // Starlight start
        // if we hit someone with a physical projectile and they have blood, splash some
        var bloodDecals = new List<(NetCoordinates coordinates, Angle angle, Color color)>();
        if (
                hitEntity is not null
             && _reflectQuery.TryComp(hitscanUid, out var reflectComp)
             && reflectComp.ReflectiveType == ReflectType.NonEnergy
             && _bloodQuery.TryComp(hitEntity, out var bloodstream))
        {
            var color = _proto.Index(bloodstream.BloodReagent).SubstanceColor;
            // A flash of the neuralyzer, then a man in a black suit says that you didn’t see any “vector crutch” here, and if you did—read it again.
            var coords = fromCoordinates.Offset((shotAngle.ToVec() * (distance + 1.3f)) + new Vector2(-0.5f, -0.5f));
            var netCoords = GetNetCoordinates(coords);

            bloodDecals.Add((netCoords, shotAngle + Angle.FromDegrees(-45), color));
        }
        // Starlight end

        if (sprites.Count > 0)
        {
            RaiseNetworkEvent(new SharedGunSystem.HitscanEvent
            {
                Sprites = sprites,
                BloodDecals = bloodDecals, // Starlight
            }, Filter.Pvs(fromCoordinates, entityMan: EntityManager));
        }
    }
}
