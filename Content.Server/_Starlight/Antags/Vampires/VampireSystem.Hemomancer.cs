using System.Linq;
using Content.Shared._Starlight.Antags.Vampires;
using Content.Shared._Starlight.Antags.Vampires.Components;
using Content.Shared.Hands.Components;
using Content.Shared.Wieldable.Components;
using Content.Shared.Humanoid;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using System.Numerics;
using Robust.Shared.Timing;
using Content.Shared.Body.Components;
using Content.Shared.Damage.Components;
using Content.Shared.Mobs.Components;
using Content.Shared._Starlight.Antags.Vampires.Components.Classes;
using Robust.Shared.Prototypes;
using Content.Shared.Polymorph;
using Content.Shared.Fluids.Components;
using Content.Shared.Physics;

namespace Content.Server._Starlight.Antags.Vampires;

public sealed partial class VampireSystem : EntitySystem
{
    private const string BloodReagentId = "Blood";

    private void InitializeHemomancer()
    {
        SubscribeLocalEvent<VampireComponent, VampireHemomancerClawsActionEvent>(OnHemomancerClaws);
        SubscribeLocalEvent<VampireHemomancerTendrilsActionEvent>(OnHemomancerTendrils);
        SubscribeLocalEvent<VampireBloodBarrierActionEvent>(OnBloodBarrier);
        SubscribeLocalEvent<VampireComponent, VampireSanguinePoolActionEvent>(OnSanguinePool);
        SubscribeLocalEvent<VampireComponent, VampireBloodEruptionActionEvent>(OnBloodEruption);
        SubscribeLocalEvent<VampireComponent, VampireBloodBringersRiteActionEvent>(OnBloodBringersRite);
        SubscribeLocalEvent<HemomancerComponent, PolymorphedEvent>(OnHemomancerPolymorphed);
        SubscribeLocalEvent<SanguinePoolComponent, PolymorphedEvent>(OnSanguinePoolReverted);

        InitializeHemomancerPredatorSense();
    }

    private static readonly Vector2[] _tendrilOffsets = new Vector2[]
    {
        new(-1, -1), new(0, -1), new(1, -1),
        new(-1,  0), new(0,  0), new(1,  0),
        new(-1,  1), new(0,  1), new(1,  1),
    };

    private void OnHemomancerClaws(EntityUid uid, VampireComponent comp, ref VampireHemomancerClawsActionEvent args)
    {
        var action = args.Action.Owner;
        if (args.Handled 
            || !ValidateVampireAbility(uid, out var validatedComp, VampireClassType.Hemomancer, action))
            return;

        comp = validatedComp;

        if (comp.SpawnedClaws != null && EntityManager.EntityExists(comp.SpawnedClaws.Value))
        {
            EntityManager.QueueDeleteEntity(comp.SpawnedClaws.Value);
            comp.SpawnedClaws = null;
        }

        var coords = Transform(uid).Coordinates;
        var claws = EntityManager.SpawnEntity("VampiricClawsItem", coords);
        comp.SpawnedClaws = claws;

        if (!_hands.TryPickupAnyHand(uid, claws))
        {
            if (!_hands.TryForcePickupAnyHand(uid, claws))
            {
                if (TryComp<HandsComponent>(uid, out var handsComp))
                {
                    _wieldable.UnwieldAll((uid, handsComp), force: true);
                    foreach (var handName in handsComp.Hands.Keys.ToArray())
                        _hands.TryDrop((uid, handsComp), handName, checkActionBlocker: false);
                }

                _hands.TryPickupAnyHand(uid, claws);
            }
        }

        // Auto-wield if the claws have a wieldable component and are in hand now.
        if (TryComp<WieldableComponent>(claws, out var wieldable) && _hands.IsHolding(uid, claws, out _))
            _wieldable.TryWield(claws, wieldable, uid);

        args.Handled = true;
    }

    private void OnHemomancerTendrils(VampireHemomancerTendrilsActionEvent args)
    {
        var action = args.Action.Owner;
        if (args.Handled 
            || !TryComp<VampireComponent>(args.Performer, out var comp) 
            || !ValidateVampireClass(args.Performer, comp, VampireClassType.Hemomancer)
            )
            return;

        var targetCoords = args.Target;
        var tileCoords = targetCoords.WithPosition(targetCoords.Position.Floored() + new Vector2(args.PositionOffset, args.PositionOffset));

        if (_transform.GetGrid(tileCoords) is not { } gridUid
            || !TryComp<MapGridComponent>(gridUid, out var gridComp)
            || !_map.TryGetTileRef(gridUid, gridComp, tileCoords, out var tileRef)
            || _turf.IsSpace(tileRef)
            || IsTileBlockedByEntities(tileCoords))
        {
            _popup.PopupEntity(Loc.GetString("action-vampire-hemomancer-tendrils-wrong-place"), args.Performer, args.Performer);
            return;
        }

        if (!CheckAndConsumeActionCost(args.Performer, comp, action))
            return;

        args.Handled = true;

        if (args.SpawnVisuals)
            SpawnTendrilVisuals(tileCoords, args.TendrilsVisualPrototype);

        var delaySeconds = Math.Max(args.MinDelay, args.Delay);
        var slowDurationSeconds = Math.Max(args.MinSlowDuration, args.SlowDuration);
        var slowMultiplier = MathF.Max(args.MinSlowMultiplier, args.SlowMultiplier);
        var toxinDamage = args.ToxinDamage;
        var performerUid = args.Performer;
        var targetRange = args.TargetRange;
        var puddleId = args.TendrilsPuddlePrototype;

        Timer.Spawn(TimeSpan.FromSeconds(delaySeconds), () =>
        {
            if (!Exists(performerUid))
                return;

            var gridUid2 = _transform.GetGrid(tileCoords);
            if (gridUid2 == null || !TryComp<MapGridComponent>(gridUid2.Value, out var gridComp2))
                return;

            if (_map.TryGetTileRef(gridUid2.Value, gridComp2, tileCoords, out var centerTileRef)
                && !_turf.IsSpace(centerTileRef)
                && !IsTileBlockedByEntities(tileCoords))
                Spawn(puddleId, tileCoords);

            var hitEnemies = new HashSet<EntityUid>();
            var slowDuration = TimeSpan.FromSeconds(slowDurationSeconds);

            foreach (var offset in _tendrilOffsets)
            {
                var center = tileCoords.Offset(offset);
                if (!_map.TryGetTileRef(gridUid2.Value, gridComp2, center, out var tileRef2)
                    || _turf.IsSpace(tileRef2)
                    || IsTileBlockedByEntities(center))
                    continue;

                foreach (var ent in _lookup.GetEntitiesInRange(center, targetRange, LookupFlags.Dynamic | LookupFlags.Sundries))
                {
                    if (ent == performerUid || hitEnemies.Contains(ent) || !HasComp<HumanoidAppearanceComponent>(ent))
                        continue;

                    if (!TryComp<DamageableComponent>(ent, out var _))
                        continue;

                    ApplyDamage(ent, _poisonTypeId, toxinDamage, performerUid);
                    _movementMod.TryAddMovementSpeedModDuration(ent, Shared.Movement.Systems.MovementModStatusSystem.FlashSlowdown, slowDuration, slowMultiplier);
                    hitEnemies.Add(ent);
                }
            }
        });
    }

    private void SpawnTendrilVisuals(EntityCoordinates tileCoords, EntProtoId tendrilVisualId)
    {
        var gridUid = _transform.GetGrid(tileCoords);
        if (gridUid == null || !TryComp<MapGridComponent>(gridUid.Value, out var gridComp))
            return;

        foreach (var offset in _tendrilOffsets)
        {
            var coords = tileCoords.Offset(offset);
            if (!_map.TryGetTileRef(gridUid.Value, gridComp, coords, out var tileRef)
                || _turf.IsSpace(tileRef)
                || IsTileBlockedByEntities(coords))
                continue;

                EntityManager.SpawnEntity(tendrilVisualId, coords);
        }
    }

    private void OnBloodBarrier(VampireBloodBarrierActionEvent args)
    {
        if (args.Handled 
            || !TryComp<VampireComponent>(args.Performer, out var comp) 
            || !ValidateVampireClass(args.Performer, comp, VampireClassType.Hemomancer) 
            )
            return;

        var targetCoords = args.Target;
        var tileCoords = targetCoords.WithPosition(targetCoords.Position.Floored() + new Vector2(0.5f, 0.5f));

        if (_transform.GetGrid(tileCoords) is not { } gridUid ||
            !TryComp<MapGridComponent>(gridUid, out var gridComp))
        {
            _popup.PopupEntity(Loc.GetString("action-vampire-blood-barrier-wrong-place"), args.Performer, args.Performer);
            return;
        }

        var performerTransform = Transform(args.Performer);
        var direction = performerTransform.LocalRotation.ToWorldVec();

        var perpendicular = new Vector2(-direction.Y, direction.X);

        var barrierCount = Math.Clamp(args.BarrierCount, 1, 9);
        var half = barrierCount / 2;
        var successfulPositions = new List<Vector2>(barrierCount);

        for (var i = -half; i <= half && successfulPositions.Count < barrierCount; i++)
        {
            var pos = tileCoords.Position + (perpendicular * i);
            var barrierCoords = tileCoords.WithPosition(pos.Floored() + new Vector2(0.5f, 0.5f));

            if (!IsValidTile(barrierCoords, gridUid, gridComp))
                continue;

            successfulPositions.Add(barrierCoords.Position);
        }

        if (successfulPositions.Count == 0)
        {
            _popup.PopupEntity(Loc.GetString("action-vampire-blood-barrier-wrong-place"), args.Performer, args.Performer);
            return;
        }

        if (!CheckAndConsumeBloodCost(args.Performer, comp, args.Action.Owner))
            return;

        args.Handled = true;

        foreach (var pos in successfulPositions)
        {
            var barrierCoords = tileCoords.WithPosition(pos);
            var barrier = EntityManager.SpawnEntity(args.BarrierPrototype, barrierCoords);
            var preventComp = EnsureComp<PreventCollideComponent>(barrier);
            preventComp.Uid = args.Performer;
            Dirty(barrier, preventComp);
        }
    }

    private void OnSanguinePool(EntityUid uid, VampireComponent comp, ref VampireSanguinePoolActionEvent args)
    {
        if (args.Handled || !TryComp<HemomancerComponent>(uid, out var hemomancer))
            return;

        if (hemomancer.InSanguinePool)
        {
            _popup.PopupEntity(Loc.GetString("action-vampire-sanguine-pool-already-in"), uid, uid);
            return;
        }

        // Don't allow pooling in space / invalid tiles
        var curCoords = Transform(uid).Coordinates;
        if (_transform.GetGrid(curCoords) is not { } gridUid
            || !TryComp<MapGridComponent>(gridUid, out var gridComp)
            || !_map.TryGetTileRef(gridUid, gridComp, curCoords, out var tileRef)
            || _turf.IsSpace(tileRef))
        {
            _popup.PopupEntity(Loc.GetString("action-vampire-sanguine-pool-invalid-tile"), uid, uid);
            return;
        }

        if (!CheckAndConsumeBloodCost(uid, comp, args.Action.Owner))
            return;

        if (TryActivateSanguinePool(uid, args))
            args.Handled = true;
    }

    private bool TryActivateSanguinePool(EntityUid uid, VampireSanguinePoolActionEvent args)
    {
        if (!_proto.TryIndex(args.PolymorphPrototype, out var polymorphProto))
        {
            _sawmill?.Error($"Missing polymorph prototype '{args.PolymorphPrototype}'.");
            return false;
        }

        var duration = Math.Max(1, args.Duration);
        var configuration = polymorphProto.Configuration with
        {
            Duration = duration
        };

        var poolEntity = _polymorph.PolymorphEntity(uid, configuration);
        if (poolEntity == null)
            return false;

        if (TryComp<SanguinePoolComponent>(poolEntity.Value, out var poolComp))
        {
            poolComp.TrailInterval = MathF.Max(0.1f, args.BloodDripInterval);
            poolComp.Accumulator = 0f;
            poolComp.ExitEffectPrototype = args.ExitEffectPrototype;
            poolComp.ExitSound = args.ExitSound;
            Dirty(poolEntity.Value, poolComp);
        }

        Spawn(args.EnterEffectPrototype, Transform(poolEntity.Value).Coordinates);
        _audio.PlayPvs(args.EnterSound, uid);
        _popup.PopupEntity(Loc.GetString("action-vampire-sanguine-pool-enter"), poolEntity.Value, poolEntity.Value);
        return true;
    }

    private void OnHemomancerPolymorphed(Entity<HemomancerComponent> ent, ref PolymorphedEvent args)
    {
        if (args.IsRevert || !HasComp<SanguinePoolComponent>(args.NewEntity))
            return;

        var (uid, comp) = ent;
        if (comp.InSanguinePool)
            return;

        comp.InSanguinePool = true;
        Dirty(uid, comp);
    }

    private void OnSanguinePoolReverted(Entity<SanguinePoolComponent> ent, ref PolymorphedEvent args)
    {
        if (!args.IsRevert || !Exists(args.NewEntity) || !TryComp<HemomancerComponent>(args.NewEntity, out var hemomancer))
            return;

        if (!hemomancer.InSanguinePool)
            return;

        hemomancer.InSanguinePool = false;
        Dirty(args.NewEntity, hemomancer);

        Spawn(ent.Comp.ExitEffectPrototype, Transform(args.NewEntity).Coordinates);
        _audio.PlayPvs(ent.Comp.ExitSound, args.NewEntity);
        _popup.PopupEntity(Loc.GetString("action-vampire-sanguine-pool-exit"), args.NewEntity, args.NewEntity);
    }

    private void OnBloodEruption(EntityUid uid, VampireComponent comp, ref VampireBloodEruptionActionEvent args)
    {
        if (args.Handled 
            || !CheckAndConsumeBloodCost(uid, comp, args.Action.Owner))
            return;

        var coords = Transform(uid).Coordinates;

        var nearbyEntities = _lookup.GetEntitiesInRange(coords, args.Range);

        var targetsToDamage = new HashSet<EntityUid>();
        var targetsToVisualize = new HashSet<EntityUid>();

        foreach (var entity in nearbyEntities)
        {
            if (entity == uid)
                continue;

            if (!IsBloodPuddle(entity))
                continue;

            if (!TryComp(entity, out TransformComponent? xform) || !xform.Anchored)
                continue;

            if (xform.GridUid is not { } gridUid || !TryComp<MapGridComponent>(gridUid, out var gridComp))
                continue;

            if (_container.IsEntityOrParentInContainer(entity))
                continue;

            var puddleCoords = xform.Coordinates;
            var puddleTile = _map.CoordinatesToTile(gridUid, gridComp, puddleCoords);
            var targetsNearPuddle = _lookup.GetEntitiesInRange(puddleCoords, args.TargetRange)
                .Where(target => target != uid
                                 && target != entity
                                 && HasComp<DamageableComponent>(target)
                                 && HasComp<BloodstreamComponent>(target)
                                 && !_container.IsEntityOrParentInContainer(target))
                .ToList();

            foreach (var target in targetsNearPuddle)
            {
                targetsToDamage.Add(target);

                if (!TryComp(target, out TransformComponent? targetXform))
                    continue;

                if (targetXform.GridUid != gridUid)
                    continue;

                var targetTile = _map.CoordinatesToTile(gridUid, gridComp, targetXform.Coordinates);
                if (targetTile == puddleTile)
                    targetsToVisualize.Add(target);
            }
        }

        foreach (var targetUid in targetsToDamage)
            ApplyDamage(targetUid, "Blunt", args.Damage, uid);

        foreach (var targetUid in targetsToVisualize)
        {
            if (!TryComp(targetUid, out TransformComponent? targetXform) || _container.IsEntityOrParentInContainer(targetUid))
                continue;

            var visual = Spawn("VampireBloodEruptionVisual", targetXform.Coordinates);
            _audio.PlayPvs(args.Sound, visual);
        }

        _popup.PopupEntity(Loc.GetString("action-vampire-blood-eruption-activated"), uid, uid);
        args.Handled = true;
    }

    private bool IsBloodPuddle(EntityUid uid)
    {
        if (!TryComp<PuddleComponent>(uid, out var puddle))
            return false;

        if (!_solution.TryGetSolution(uid, puddle.SolutionName, out _, out var solution))
            return false;

        return solution.ContainsReagent(BloodReagentId, null);
    }

    private void OnBloodBringersRite(EntityUid uid, VampireComponent comp, ref VampireBloodBringersRiteActionEvent args)
    {
        if (args.Handled || !comp.ActionEntities.TryGetValue("ActionVampireBloodBringersRite", out var actionEntity) || !TryComp<HemomancerComponent>(uid, out var hemomancer))
            return;

        if (!comp.FullPower)
        {
            _popup.PopupEntity(Loc.GetString("action-vampire-not-enough-power"), uid, uid);
            args.Handled = true;
            return;
        }
        
        if (hemomancer.BloodBringersRiteActive)
        {
            DeactivateBloodBringersRite(uid, hemomancer);
            _popup.PopupEntity(Loc.GetString("action-vampire-blood-bringers-rite-stop"), uid, uid);
        }
        else
        {
            if (!comp.FullPower)
            {
                _popup.PopupEntity(Loc.GetString("action-vampire-blood-bringers-rite-not-enough-power"), uid, uid);
                return;
            }
            if (comp.DrunkBlood < args.Cost)
            {
                _popup.PopupEntity(Loc.GetString("action-vampire-blood-brighters-rite-not-enough-blood"), uid, uid);
                return;
            }

            ActivateBloodBringersRite(uid, hemomancer, args.ToggleInterval, args.Cost, args.Range, args.Damage, args.HealBrute, args.HealBurn, args.HealStamina);
            _popup.PopupEntity(Loc.GetString("action-vampire-blood-bringers-rite-start"), uid, uid);
        }

        if (_actions.GetAction(actionEntity) is { } action)
            _actions.SetToggled(action.AsNullable(), hemomancer.BloodBringersRiteActive);

        args.Handled = true;
    }

    private void ActivateBloodBringersRite(EntityUid uid, HemomancerComponent comp, float interval, int cost, float range, float damage, float healBrute, float healBurn, float healStamina)
    {
        comp.BloodBringersRiteActive = true;
        comp.BloodBringersRiteLoopId++;

        var drainBeamComp = EnsureComp<VampireDrainBeamComponent>(uid);
        drainBeamComp.ActiveBeams.Clear();

        Dirty(uid, comp);

        StartBloodBringersRiteLoop(uid, interval, 0, cost, range, damage, healBrute, healBurn, healStamina);
    }

    private void DeactivateBloodBringersRite(EntityUid uid, HemomancerComponent comp)
    {
        comp.BloodBringersRiteActive = false;

        if (TryComp<VampireDrainBeamComponent>(uid, out var drainBeamComp))
        {
            foreach (var connection in drainBeamComp.ActiveBeams.Values)
            {
                var removeEvent = new VampireDrainBeamEvent(GetNetEntity(connection.Source), GetNetEntity(connection.Target), false);
                RaiseNetworkEvent(removeEvent);
            }
            drainBeamComp.ActiveBeams.Clear();
        }

        Dirty(uid, comp);
    }

    private void StartBloodBringersRiteLoop(EntityUid uid, float interval, int tickCount, int cost, float range, float damage, float healBrute, float healBurn, float healStamina)
    {
        const int MaxTicks = 150;

        if (tickCount >= MaxTicks 
            || !Exists(uid) 
            || !TryComp<VampireComponent>(uid, out var comp) 
            || !comp.ActionEntities.TryGetValue("ActionVampireBloodBringersRite", out var actionEntity)
            || !TryComp<HemomancerComponent>(uid, out var hemomancer) 
            || !hemomancer.BloodBringersRiteActive)
            return;

        if (TryComp<MobStateComponent>(uid, out var mobState) &&
            mobState.CurrentState == Shared.Mobs.MobState.Dead)
        {
            DeactivateBloodBringersRite(uid, hemomancer);
            return;
        }

        if (comp.DrunkBlood < cost)
        {
            DeactivateBloodBringersRite(uid, hemomancer);
            _popup.PopupEntity(Loc.GetString("action-vampire-blood-bringers-rite-stop-blood"), uid, uid);

            if (_actions.GetAction(actionEntity) is { } action)
                _actions.SetToggled(action.AsNullable(), false);
            return;
        }

        comp.DrunkBlood -= cost;
        Dirty(uid, comp);
        UpdateVampireAlert(uid);

        var coords = Transform(uid).Coordinates;
        var currentTargets = new List<EntityUid>();
        var nearbyEntities = _lookup.GetEntitiesInRange(coords, range);

        foreach (var entity in nearbyEntities)
        {
            if (entity == uid)
                continue;

            if (_container.IsEntityOrParentInContainer(entity))
                continue;

            if (TryComp<MobStateComponent>(entity, out var state) && state.CurrentState == Shared.Mobs.MobState.Dead)
                continue;

            if (!HasComp<HumanoidAppearanceComponent>(entity) || !HasComp<BloodstreamComponent>(entity))
                continue;

            // Prevent drain beams working through walls
            if (!_examine.InRangeUnOccluded(uid, entity, range))
                continue;

            currentTargets.Add(entity);
        }

        UpdateDrainBeamNetwork(uid, currentTargets, range);

        foreach (var target in currentTargets)
        {
            ApplyDamage(target, "Blunt", damage, uid);

            ApplyHealing(uid, _bruteGroupId, healBrute, true);
            ApplyHealing(uid, _burnGroupId, healBurn, true);
            if (TryComp<StaminaComponent>(uid, out var stam))
                _stamina.TakeStaminaDamage(uid, -healStamina, stam);
        }

        var expectedLoopId = hemomancer.BloodBringersRiteLoopId;

        Timer.Spawn(TimeSpan.FromSeconds(interval), () =>
        {
            if (!Exists(uid) || !TryComp<HemomancerComponent>(uid, out var c2)) return;
            if (!c2.BloodBringersRiteActive || c2.BloodBringersRiteLoopId != expectedLoopId) return;
            StartBloodBringersRiteLoop(uid, interval, tickCount + 1, cost, range, damage, healBrute, healBurn, healStamina);
        });
    }

    private void UpdateDrainBeamNetwork(EntityUid vampire, List<EntityUid> targets, float range)
    {
        if (!TryComp<VampireDrainBeamComponent>(vampire, out var drainBeamComp))
            return;

        var requiredTargets = new HashSet<EntityUid>(targets);

        var toRemove = new List<EntityUid>();
        foreach (var (targetKey, connection) in drainBeamComp.ActiveBeams)
        {
            if (connection.Source != vampire)
            {
                var removeLegacy = new VampireDrainBeamEvent(GetNetEntity(connection.Source), GetNetEntity(connection.Target), false);
                RaiseNetworkEvent(removeLegacy);
                toRemove.Add(targetKey);
                continue;
            }

            if (!requiredTargets.Contains(connection.Target))
            {
                var removeEvent = new VampireDrainBeamEvent(GetNetEntity(connection.Source), GetNetEntity(connection.Target), false);
                RaiseNetworkEvent(removeEvent);

                toRemove.Add(targetKey);
            }
        }

        foreach (var key in toRemove)
            drainBeamComp.ActiveBeams.Remove(key);

        foreach (var target in requiredTargets)
        {
            if (!drainBeamComp.ActiveBeams.ContainsKey(target))
            {
                var connection = new DrainBeamConnection(vampire, target, range);
                drainBeamComp.ActiveBeams[target] = connection;

                var createEvent = new VampireDrainBeamEvent(GetNetEntity(vampire), GetNetEntity(target), true);
                RaiseNetworkEvent(createEvent);
            }
        }
    }
}