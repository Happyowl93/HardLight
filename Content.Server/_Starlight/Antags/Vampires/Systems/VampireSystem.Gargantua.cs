using System.Numerics;
using Content.Server.Destructible;
using Content.Shared._Starlight.Antags.Vampires;
using Content.Shared._Starlight.Antags.Vampires.Components;
using Content.Shared._Starlight.Antags.Vampires.Components.Classes;
using Content.Shared.Access.Components;
using Content.Shared.CombatMode;
using Content.Shared.Damage;
using Content.Shared.Doors.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Maps;
using Content.Shared.Mobs.Components;
using Content.Shared.Movement.Pulling.Events;
using Content.Shared.Popups;
using Content.Shared.Tools.Components;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Timing;

namespace Content.Server._Starlight.Antags.Vampires;

public sealed partial class VampireSystem : EntitySystem
{
    private const string BloodSwellActionId = "ActionVampireBloodSwell";
    private const string BloodRushActionId = "ActionVampireBloodRush";
    private const string OverwhelmingForceActionId = "ActionVampireOverwhelmingForce";

    private void InitializeGargantua()
    {
        SubscribeLocalEvent<VampireComponent, VampireBloodSwellActionEvent>(OnBloodSwell);
        SubscribeLocalEvent<VampireComponent, VampireBloodRushActionEvent>(OnBloodRush);
        SubscribeLocalEvent<VampireComponent, VampireSeismicStompActionEvent>(OnSeismicStomp);
        SubscribeLocalEvent<VampireComponent, VampireOverwhelmingForceActionEvent>(OnOverwhelmingForce);
        SubscribeLocalEvent<VampireComponent, VampireDemonicGraspActionEvent>(OnDemonicGrasp);
        SubscribeLocalEvent<VampireComponent, VampireChargeActionEvent>(OnCharge);
        SubscribeLocalEvent<GargantuaComponent, ShotAttemptedEvent>(OnShotAttempted);
        SubscribeLocalEvent<GargantuaComponent, PullAttemptEvent>(OnPullAttempt);
        SubscribeLocalEvent<GargantuaComponent, InteractionAttemptEvent>(OnInteractionAttempt);
    }

    private void UpdateGargantua(float frameTime)
    {
        var query = EntityQueryEnumerator<GargantuaComponent, VampireComponent>();
        var now = _timing.CurTime;

        while (query.MoveNext(out var uid, out var gargantua, out var vampire))
        {
            if (gargantua.BloodSwellActive && gargantua.BloodSwellEndTime.HasValue && now >= gargantua.BloodSwellEndTime.Value)
            {
                EndBloodSwell(uid, gargantua, vampire);
            }

            if (gargantua.BloodRushActive && gargantua.BloodRushEndTime.HasValue && now >= gargantua.BloodRushEndTime.Value)
            {
                EndBloodRush(uid, gargantua);
            }

            if (gargantua.IsCharging)
            {
                ProcessChargeMovement(uid, gargantua, vampire, frameTime);
            }
        }
    }

    #region Blood Swell

    private void OnBloodSwell(EntityUid uid, VampireComponent comp, ref VampireBloodSwellActionEvent args)
    {
        if (args.Handled)
            return;

        if (!comp.ActionEntities.TryGetValue(BloodSwellActionId, out var actionEntity)
            || !CheckAndConsumeBloodCost(uid, comp, actionEntity))
            return;

        var gargantua = EnsureComp<GargantuaComponent>(uid);

        if (gargantua.BloodSwellActive)
        {
            // Already active, refresh duration
            gargantua.BloodSwellEndTime = _timing.CurTime + TimeSpan.FromSeconds(args.Duration);
        }
        else
        {
            gargantua.BloodSwellActive = true;
            gargantua.BloodSwellEndTime = _timing.CurTime + TimeSpan.FromSeconds(args.Duration);
            _popup.PopupEntity(Loc.GetString("vampire-blood-swell-start"), uid, uid);
        }

        Dirty(uid, gargantua);
        args.Handled = true;
    }

    private void EndBloodSwell(EntityUid uid, GargantuaComponent gargantua, VampireComponent vampire)
    {
        gargantua.BloodSwellActive = false;
        gargantua.BloodSwellEndTime = null;
        Dirty(uid, gargantua);
        _popup.PopupEntity(Loc.GetString("vampire-blood-swell-end"), uid, uid);
    }

    private void OnShotAttempted(EntityUid uid, GargantuaComponent component, ref ShotAttemptedEvent args)
    {
        if (component.BloodSwellActive)
        {
            args.Cancel();
        }
    }

    #endregion

    #region Blood Rush

    private void OnBloodRush(EntityUid uid, VampireComponent comp, ref VampireBloodRushActionEvent args)
    {
        if (args.Handled)
            return;

        if (!comp.ActionEntities.TryGetValue(BloodRushActionId, out var actionEntity)
            || !CheckAndConsumeBloodCost(uid, comp, actionEntity))
            return;

        var gargantua = EnsureComp<GargantuaComponent>(uid);

        if (gargantua.BloodRushActive)
        {
            // Already active, refresh duration
            gargantua.BloodRushEndTime = _timing.CurTime + TimeSpan.FromSeconds(args.Duration);
        }
        else
        {
            gargantua.BloodRushActive = true;
            gargantua.BloodRushEndTime = _timing.CurTime + TimeSpan.FromSeconds(args.Duration);

            // Apply speed buff
            _movementMod.TryAddMovementSpeedModDuration(uid, "VampireBloodRush", TimeSpan.FromSeconds(args.Duration), gargantua.BloodRushSpeedMultiplier);

            _popup.PopupEntity(Loc.GetString("vampire-blood-rush-start"), uid, uid);
        }

        Dirty(uid, gargantua);
        args.Handled = true;
    }

    private void EndBloodRush(EntityUid uid, GargantuaComponent gargantua)
    {
        gargantua.BloodRushActive = false;
        gargantua.BloodRushEndTime = null;
        Dirty(uid, gargantua);
        _popup.PopupEntity(Loc.GetString("vampire-blood-rush-end"), uid, uid);
    }

    #endregion

    #region Seismic Stomp

    private void OnSeismicStomp(EntityUid uid, VampireComponent comp, ref VampireSeismicStompActionEvent args)
    {
        if (args.Handled)
            return;

        if (!comp.ActionEntities.TryGetValue("ActionVampireSeismicStomp", out var actionEntity)
            || !CheckAndConsumeBloodCost(uid, comp, actionEntity))
            return;

        var xform = Transform(uid);
        var worldPos = _transform.GetWorldPosition(xform);

        _popup.PopupEntity(Loc.GetString("vampire-seismic-stomp-activate"), uid, uid);

        // Find all entities in radius
        var entities = _lookup.GetEntitiesInRange(xform.Coordinates, args.Radius);

        foreach (var target in entities)
        {
            if (target == uid)
                continue;

            // Only affect mobs
            if (!HasComp<MobStateComponent>(target))
                continue;

            var targetXform = Transform(target);
            var targetPos = _transform.GetWorldPosition(targetXform);
            var direction = targetPos - worldPos;

            if (direction == Vector2.Zero)
                direction = _rand.NextVector2();

            direction = direction.Normalized();

            // Knockdown the target
            _stun.TryKnockdown(target, TimeSpan.FromSeconds(2), true);

            // Throw them away from the vampire
            _throwing.TryThrow(target, direction * args.ThrowDistance, 5f, uid);
        }

        // Damage floor tiles
        if (xform.GridUid != null && TryComp<MapGridComponent>(xform.GridUid, out var grid))
        {
            // Get all tiles in radius and damage them
            var centerTile = _map.TileIndicesFor(xform.GridUid.Value, grid, xform.Coordinates);
            var radius = (int)Math.Ceiling(args.Radius);

            for (var x = -radius; x <= radius; x++)
            {
                for (var y = -radius; y <= radius; y++)
                {
                    var offset = new Vector2i(x, y);
                    if (offset.Length > args.Radius)
                        continue;

                    var tilePos = centerTile + offset;
                    var tile = _map.GetTileRef(xform.GridUid.Value, grid, tilePos);

                    if (tile.Tile.IsEmpty)
                        continue;

                    // Break the tile to plating
                    _tiles.DeconstructTile(tile);
                }
            }
        }

        // Spawn visual effect
        Spawn("VampireSeismicStompEffect", xform.Coordinates);

        args.Handled = true;
    }

    #endregion

    #region Overwhelming Force

    private void OnOverwhelmingForce(EntityUid uid, VampireComponent comp, ref VampireOverwhelmingForceActionEvent args)
    {
        if (args.Handled)
            return;

        var gargantua = EnsureComp<GargantuaComponent>(uid);

        gargantua.OverwhelmingForceActive = !gargantua.OverwhelmingForceActive;

        if (gargantua.OverwhelmingForceActive)
        {
            _popup.PopupEntity(Loc.GetString("vampire-overwhelming-force-start"), uid, uid);
        }
        else
        {
            _popup.PopupEntity(Loc.GetString("vampire-overwhelming-force-stop"), uid, uid);
        }

        // Update action toggle state
        if (comp.ActionEntities.TryGetValue(OverwhelmingForceActionId, out var actionEntity)
            && _actions.GetAction(actionEntity) is { } action)
        {
            _actions.SetToggled(action.AsNullable(), gargantua.OverwhelmingForceActive);
        }

        Dirty(uid, gargantua);
        args.Handled = true;
    }

    private void OnPullAttempt(EntityUid uid, GargantuaComponent component, PullAttemptEvent args)
    {
        if (!component.OverwhelmingForceActive)
            return;

        // Prevent being pulled
        if (args.PulledUid == uid)
        {
            args.Cancelled = true;
            _popup.PopupEntity(Loc.GetString("vampire-overwhelming-force-too-heavy"), uid, args.PullerUid, PopupType.MediumCaution);
        }
    }

    private void OnInteractionAttempt(EntityUid uid, GargantuaComponent component, InteractionAttemptEvent args)
    {
        if (!component.OverwhelmingForceActive)
            return;

        if (args.Target == null)
            return;

        // Auto-pry doors
        if (!TryComp<DoorComponent>(args.Target, out var door))
            return;

        // Check if door is bolted or welded
        if (TryComp<DoorBoltComponent>(args.Target, out var bolt) && bolt.BoltsDown)
            return;

        if (TryComp<WeldableComponent>(args.Target, out var weld) && weld.IsWelded)
            return;

        // Check if we have access
        if (TryComp<AccessReaderComponent>(args.Target, out _))
        {
            // No access check needed - just pry it open
            if (door.State == DoorState.Closed)
            {
                // Consume blood
                if (TryComp<VampireComponent>(uid, out var vampire) && vampire.DrunkBlood >= 5)
                {
                    vampire.DrunkBlood -= 5;
                    Dirty(uid, vampire);
                    UpdateVampireAlert(uid);

                    _door.StartOpening(args.Target.Value);
                    _popup.PopupEntity(Loc.GetString("vampire-overwhelming-force-door-pried"), uid, uid);
                }
            }
        }
    }

    #endregion

    #region Demonic Grasp

    private void OnDemonicGrasp(EntityUid uid, VampireComponent comp, ref VampireDemonicGraspActionEvent args)
    {
        if (args.Handled)
            return;

        if (!comp.ActionEntities.TryGetValue("ActionVampireDemonicGrasp", out var actionEntity))
            return;

        if (!CheckAndConsumeBloodCost(uid, comp, actionEntity))
            return;

        var xform = Transform(uid);
        if (xform.GridUid == null)
            return;

        var grid = Comp<MapGridComponent>(xform.GridUid.Value);
        var gridUid = xform.GridUid.Value;
        var startPos = _transform.GetWorldPosition(xform);
        var targetPos = _transform.ToMapCoordinates(args.Target).Position;
        var direction = (targetPos - startPos).Normalized();

        // Check if combat mode is active for pulling
        var shouldPull = TryComp<CombatModeComponent>(uid, out var combat) && combat.IsInCombatMode;

        _popup.PopupEntity(Loc.GetString("vampire-demonic-grasp-cast"), uid, uid);

        // Calculate tiles along the path - capture values for lambda
        var maxTiles = (int)args.Range;
        var immobilizeDuration = args.ImmobilizeDuration;
        var delayPerTile = 50; // 50ms between each tile effect

        for (var i = 1; i <= maxTiles; i++)
        {
            var tileIndex = i;
            var tilePos = startPos + (direction * tileIndex);

            Timer.Spawn(delayPerTile * i, () =>
            {
                // Check if vampire still exists
                if (!Exists(uid))
                    return;

                // Get tile coordinates
                var tileCoords = new EntityCoordinates(gridUid, tilePos);

                // Spawn visual effect on the tile
                var effect = Spawn("VampireDemonicGraspEffect", tileCoords);

                // Check for mobs on this tile
                var entitiesOnTile = _lookup.GetEntitiesInRange(tileCoords, 0.5f);
                foreach (var target in entitiesOnTile)
                {
                    if (target == uid)
                        continue;

                    if (!HasComp<MobStateComponent>(target))
                        continue;

                    // Found a mob - immobilize them
                    _stun.TryAddParalyzeDuration(target, TimeSpan.FromSeconds(immobilizeDuration));

                    // Spawn immobilized effect on the target
                    var targetXform = Transform(target);
                    Spawn("VampireImmobilizedEffect", targetXform.Coordinates);

                    _popup.PopupEntity(Loc.GetString("vampire-demonic-grasp-hit"), target, target, PopupType.LargeCaution);

                    // If combat mode was active, pull the target to the vampire
                    if (shouldPull && Exists(uid))
                    {
                        var vampireXform = Transform(uid);
                        var vampirePos = _transform.GetWorldPosition(vampireXform);
                        var targetCurrentPos = _transform.GetWorldPosition(targetXform);
                        var pullDirection = (vampirePos - targetCurrentPos).Normalized();
                        var distance = (vampirePos - targetCurrentPos).Length();

                        _throwing.TryThrow(target, pullDirection * (distance - 1f), 8f, uid);

                        _popup.PopupEntity(Loc.GetString("vampire-demonic-grasp-pull"), uid, uid);
                    }
                }
            });
        }

        args.Handled = true;
    }

    #endregion

    #region Charge

    private void OnCharge(EntityUid uid, VampireComponent comp, ref VampireChargeActionEvent args)
    {
        if (args.Handled)
            return;

        if (!comp.ActionEntities.TryGetValue("ActionVampireCharge", out var actionEntity)
            || !CheckAndConsumeBloodCost(uid, comp, actionEntity))
            return;

        var gargantua = EnsureComp<GargantuaComponent>(uid);

        if (gargantua.IsCharging)
            return;

        var xform = Transform(uid);
        var startPos = _transform.GetWorldPosition(xform);
        var targetPos = _transform.ToMapCoordinates(args.Target).Position;
        var direction = (targetPos - startPos).Normalized();

        gargantua.IsCharging = true;
        gargantua.ChargeDirection = new Angle(MathF.Atan2(direction.Y, direction.X));
        gargantua.ChargeDirectionVector = direction;
        gargantua.ChargeSpeed = args.ChargeSpeed;
        gargantua.ChargeCreatureDamage = args.CreatureDamage;
        gargantua.ChargeCreatureThrowDistance = args.CreatureThrowDistance;
        gargantua.ChargeStructuralDamage = args.StructuralDamage;

        _popup.PopupEntity(Loc.GetString("vampire-charge-start"), uid, uid);

        Dirty(uid, gargantua);
        args.Handled = true;
    }

    private void ProcessChargeMovement(EntityUid uid, GargantuaComponent gargantua, VampireComponent _, float frameTime)
    {
        var xform = Transform(uid);

        // Check if we're over void/space
        if (xform.GridUid == null || !TryComp<MapGridComponent>(xform.GridUid, out var grid))
        {
            EndCharge(uid, gargantua);
            return;
        }

        var tileRef = _map.GetTileRef(xform.GridUid.Value, grid, xform.Coordinates);
        if (tileRef.Tile.IsEmpty)
        {
            EndCharge(uid, gargantua);
            return;
        }

        // Move in the charge direction
        var movement = gargantua.ChargeDirectionVector * gargantua.ChargeSpeed * frameTime;
        var newPos = _transform.GetWorldPosition(xform) + movement;

        // Check for obstacles
        var moverCoords = _transform.GetMoverCoordinates(uid);
        var offsetCoords = moverCoords.Offset(movement);
        var nextTilePos = _map.TileIndicesFor(xform.GridUid.Value, grid, offsetCoords);
        var obstacles = _lookup.GetEntitiesInRange(xform.Coordinates.Offset(movement), 0.5f);

        foreach (var obstacle in obstacles)
        {
            if (obstacle == uid)
                continue;

            // Check for mobs
            if (HasComp<MobStateComponent>(obstacle))
            {
                HandleChargeImpact(uid, obstacle, gargantua);
                continue;
            }

            // Check for anchored entities (structures, walls, etc.)
            if (TryComp<PhysicsComponent>(obstacle, out var physics) && physics.BodyType == BodyType.Static)
            {
                // Check if it's a wall - destroy it
                if (HasComp<DestructibleComponent>(obstacle))
                {
                    // Apply massive damage to destroy
                    var damageSpec = new DamageSpecifier();
                    damageSpec.DamageDict["Structural"] = gargantua.ChargeStructuralDamage;
                    _damageableSystem.TryChangeDamage(obstacle, damageSpec, true, origin: uid);
                }

                EndCharge(uid, gargantua);
                return;
            }

            // Unanchored objects - throw them
            if (TryComp<PhysicsComponent>(obstacle, out var physicsUnanchored) && physicsUnanchored.BodyType != BodyType.Static)
            {
                _throwing.TryThrow(obstacle, gargantua.ChargeDirectionVector * gargantua.ChargeCreatureThrowDistance, 10f, uid);

                var damageSpec = new DamageSpecifier();
                damageSpec.DamageDict["Blunt"] = gargantua.ChargeCreatureDamage;
                _damageableSystem.TryChangeDamage(obstacle, damageSpec, true, origin: uid);
            }
        }

        // Move the vampire
        _transform.SetWorldPosition(uid, newPos);
    }

    private void HandleChargeImpact(EntityUid uid, EntityUid target, GargantuaComponent gargantua)
    {
        // Apply damage
        var damageSpec = new DamageSpecifier();
        damageSpec.DamageDict["Blunt"] = gargantua.ChargeCreatureDamage;
        _damageableSystem.TryChangeDamage(target, damageSpec, true, origin: uid);

        // Throw the target
        _throwing.TryThrow(target, gargantua.ChargeDirectionVector * gargantua.ChargeCreatureThrowDistance, 10f, uid);

        // Stun
        _stun.TryKnockdown(target, TimeSpan.FromSeconds(2), true);

        _popup.PopupEntity(Loc.GetString("vampire-charge-impact", ("target", target)), uid, uid);
    }

    private void EndCharge(EntityUid uid, GargantuaComponent gargantua)
    {
        gargantua.IsCharging = false;
        gargantua.ChargeSpeed = 0;
        gargantua.ChargeDirectionVector = default;
        Dirty(uid, gargantua);
    }

    #endregion
}
