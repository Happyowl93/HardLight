using System;
using System.Linq;
using Content.Shared._Starlight.Antags.Vampires;
using Content.Shared._Starlight.Antags.Vampires.Components;
using Content.Shared.Hands.Components;
using Content.Shared.Wieldable.Components;
using Content.Shared.Damage;
using Content.Shared.Humanoid;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using System.Numerics;
using Robust.Shared.Timing;
using Content.Shared.Body.Components;
using Content.Shared.Damage.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Physics;
using Content.Shared._Starlight.Antags.Vampires.Components.Classes;
using Robust.Shared.Prototypes;
using Content.Shared.Polymorph;

namespace Content.Server._Starlight.Antags.Vampires;

public sealed partial class VampireSystem : EntitySystem
{
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
    }

    private static readonly Vector2[] _tendrilOffsets = new Vector2[]
    {
        new(-1, -1), new(0, -1), new(1, -1),
        new(-1,  0), new(0,  0), new(1,  0),
        new(-1,  1), new(0,  1), new(1,  1),
    };

    private void OnHemomancerClaws(EntityUid uid, VampireComponent comp, ref VampireHemomancerClawsActionEvent args)
    {
        if (args.Handled 
            || !comp.ActionEntities.TryGetValue("ActionVampireHemomancerClaws", out var action) 
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
        if (args.Handled)
            return;

        if (args.Handled 
            || !TryComp<VampireComponent>(args.Performer, out var comp) 
            || !TryComp<HemomancerComponent>(args.Performer, out var hemomancer)
            || !comp.ActionEntities.TryGetValue("ActionVampireHemomancerTendrils", out var action) 
            || !ValidateVampireClass(args.Performer, comp, VampireClassType.Hemomancer)
            || !CheckAndConsumeActionCost(args.Performer, comp, action))
            return;

        args.Handled = true;

        var targetCoords = args.Target;
        var tileCoords = targetCoords.WithPosition(targetCoords.Position.Floored() + new Vector2(args.PositionOffset, args.PositionOffset));

        if (!ValidateTendrilTarget(tileCoords, args.Performer))
            return;

        if (args.SpawnVisuals)
            SpawnTendrilVisuals(tileCoords, hemomancer.BloodTendrilsVisual);

        ScheduleTendrilEffect(args, tileCoords);
    }

    private bool ValidateTendrilTarget(EntityCoordinates tileCoords, EntityUid performer)
    {
        if (_transform.GetGrid(tileCoords) is not { } gridUid ||
            !TryComp<MapGridComponent>(gridUid, out var gridComp) ||
            !IsValidTile(tileCoords, gridUid, gridComp))
        {
            _popup.PopupEntity(Loc.GetString("action-vampire-hemomancer-tendrils-wrong-place"), performer, performer);
            return false;
        }
        return true;
    }

    private void SpawnTendrilVisuals(EntityCoordinates tileCoords, EntProtoId tendrilVisualId)
    {
        var gridUid = _transform.GetGrid(tileCoords);
        if (gridUid == null || !TryComp<MapGridComponent>(gridUid.Value, out var gridComp))
            return;

        foreach (var offset in _tendrilOffsets)
        {
            var coords = tileCoords.Offset(offset);
            if (IsValidTile(coords, gridUid.Value, gridComp))
                EntityManager.SpawnEntity(tendrilVisualId, coords);
        }
    }

    private void ScheduleTendrilEffect(VampireHemomancerTendrilsActionEvent args, EntityCoordinates tileCoords)
    {
        var delay = TimeSpan.FromSeconds(Math.Max(args.MinDelay, args.Delay));
        var slowDuration = TimeSpan.FromSeconds(Math.Max(args.MinSlowDuration, args.SlowDuration));
        var slowMultiplier = MathF.Max(args.MinSlowMultiplier, args.SlowMultiplier);
        var toxinDamage = args.ToxinDamage;
        var performerUid = args.Performer;

        var gridUid = _transform.GetGrid(tileCoords);
        if (gridUid == null || !TryComp<MapGridComponent>(gridUid.Value, out var gridComp))
            return;

        Timer.Spawn(delay, () => ExecuteTendrilEffect(performerUid, tileCoords, gridUid.Value, gridComp, toxinDamage, slowDuration, slowMultiplier, args.VisualSpawnDelay, args.TargetRange, args.PositionOffset));
    }

    private void ExecuteTendrilEffect(EntityUid performerUid, EntityCoordinates targetCoords, EntityUid gridUid, MapGridComponent gridComp,
        float toxinDamage, TimeSpan slowDuration, float slowMultiplier, float spawnDelay, float targetRange, float positionOffset)
    {
        if (!Exists(performerUid) || !TryComp<HemomancerComponent>(performerUid, out var hemomancer))
            return;

        // Spawn blood puddle at center
        if (IsValidTile(targetCoords, gridUid, gridComp))
            Spawn(hemomancer.BloodTendrilsPuddle, targetCoords);

        // Process damage and effects
        var hitEnemies = ProcessTendrilDamage(performerUid, targetCoords, gridUid, gridComp, toxinDamage, slowDuration, slowMultiplier, targetRange);

        // Schedule visual effects for hit enemies
        if (hitEnemies.Count > 0)
            Timer.Spawn(TimeSpan.FromSeconds(spawnDelay), () => SpawnTendrilEffectsOnEnemies(hitEnemies, gridUid, gridComp, positionOffset, hemomancer.BloodTendrilsVisual, hemomancer.BloodTendrilsPuddle));
    }

    private List<EntityUid> ProcessTendrilDamage(EntityUid performerUid, EntityCoordinates targetCoords, EntityUid gridUid,
        MapGridComponent gridComp, float toxinDamage, TimeSpan slowDuration, float slowMultiplier, float targetRange)
    {
        var hitEnemies = new List<EntityUid>();

        foreach (var offset in _tendrilOffsets)
        {
            var center = targetCoords.Offset(offset);
            if (!IsValidTile(center, gridUid, gridComp))
                continue;

            foreach (var ent in _lookup.GetEntitiesInRange(center, targetRange, LookupFlags.Dynamic | LookupFlags.Sundries))
            {
                if (ent == performerUid || !HasComp<HumanoidAppearanceComponent>(ent) ||
                    !TryComp<DamageableComponent>(ent, out var _) || hitEnemies.Contains(ent))
                    continue;

                ApplyDamage(ent, _poisonTypeId, toxinDamage, performerUid);
                _movementMod.TryAddMovementSpeedModDuration(ent, Shared.Movement.Systems.MovementModStatusSystem.FlashSlowdown, slowDuration, slowMultiplier);
                hitEnemies.Add(ent);
            }
        }

        return hitEnemies;
    }

    private void SpawnTendrilEffectsOnEnemies(List<EntityUid> hitEnemies, EntityUid gridUid, MapGridComponent gridComp, float positionOffset, EntProtoId tendrilVisualId, EntProtoId puddleId)
    {
        foreach (var enemy in hitEnemies)
        {
            if (!Exists(enemy))
                continue;

            var enemyCoords = Transform(enemy).Coordinates;
            EntityManager.SpawnEntity(tendrilVisualId, enemyCoords);

            var enemyTileCoords = enemyCoords.WithPosition(enemyCoords.Position.Floored() + new Vector2(positionOffset, positionOffset));
            if (IsValidTile(enemyTileCoords, gridUid, gridComp))
                Spawn(puddleId, enemyTileCoords);
        }
    }

    private void OnBloodBarrier(VampireBloodBarrierActionEvent args)
    {
        if (args.Handled 
            || !TryComp<HemomancerComponent>(args.Performer, out var hemomancer)
            || !TryComp<VampireComponent>(args.Performer, out var comp) 
            || !comp.ActionEntities.TryGetValue("ActionVampireBloodBarrier", out var action)
            || !ValidateVampireClass(args.Performer, comp, VampireClassType.Hemomancer) 
            || !CheckAndConsumeBloodCost(args.Performer, comp, action))
            return;

        args.Handled = true;

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

        var barrierPositions = new Vector2[]
        {
            tileCoords.Position + perpendicular,
            tileCoords.Position,
            tileCoords.Position - perpendicular
        };

        int successfulBarriers = 0;
        foreach (var pos in barrierPositions)
        {
            var barrierCoords = tileCoords.WithPosition(pos.Floored() + new Vector2(0.5f, 0.5f));

            if (!IsValidTile(barrierCoords, gridUid, gridComp))
                continue;

            var barrier = EntityManager.SpawnEntity(hemomancer.BloodBarrier, barrierCoords);

            var preventCollide = EnsureComp<PreventCollideComponent>(barrier);
            preventCollide.Uid = args.Performer;

            successfulBarriers++;

        }

        if (successfulBarriers == 0)
            _popup.PopupEntity(Loc.GetString("action-vampire-blood-barrier-wrong-place"), args.Performer, args.Performer);
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

        // Reminder да я хуй знает почему не пашет, потом разерусь
        // Dont allow pooling? in invalid tiles
        // var curCoords = Transform(uid).Coordinates;
        // if (!IsValidTile(curCoords))
        // {
        //     _popup.PopupEntity(Loc.GetString("action-vampire-sanguine-pool-invalid-tile"), uid, uid);
        //     return;
        // }

        if (!comp.ActionEntities.TryGetValue(hemomancer.SanguinePoolAction, out var action) 
            || !CheckAndConsumeBloodCost(uid, comp, action))
            return;

        if (TryActivateSanguinePool(uid, hemomancer, args))
            args.Handled = true;
    }

    private bool TryActivateSanguinePool(EntityUid uid, HemomancerComponent hemomancer, VampireSanguinePoolActionEvent args)
    {
        if (!_proto.TryIndex(hemomancer.SanguinePoolPolymorph, out var polymorphProto))
        {
            _sawmill?.Error($"Missing polymorph prototype '{hemomancer.SanguinePoolPolymorph}'.");
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
            Dirty(poolEntity.Value, poolComp);
        }

        Spawn(hemomancer.SanguinePoolEnterEffect, Transform(poolEntity.Value).Coordinates);
        _audio.PlayPvs(hemomancer.SanguinePoolEnterSound, uid);
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

        Spawn(hemomancer.SanguinePoolExitEffect, Transform(args.NewEntity).Coordinates);
        _audio.PlayPvs(hemomancer.SanguinePoolExitSound, args.NewEntity);
        _popup.PopupEntity(Loc.GetString("action-vampire-sanguine-pool-exit"), args.NewEntity, args.NewEntity);
    }

    private void OnBloodEruption(EntityUid uid, VampireComponent comp, ref VampireBloodEruptionActionEvent args)
    {
        if (args.Handled 
            || !comp.ActionEntities.TryGetValue("ActionVampireBloodEruption", out var action)
            || !CheckAndConsumeBloodCost(uid, comp, action))
            return;

        var coords = Transform(uid).Coordinates;

        var nearbyEntities = _lookup.GetEntitiesInRange(coords, args.Range);

        var bloodPuddlesWithTargets = new Dictionary<EntityUid, List<EntityUid>>();

        foreach (var entity in nearbyEntities)
        {
            if (entity == uid)
                continue;
                
            if (MetaData(entity).EntityPrototype?.ID != "PuddleBlood")
                continue;

            if (!TryComp(entity, out TransformComponent? xform) || !xform.Anchored)
                continue;

            if (_container.IsEntityOrParentInContainer(entity))
                continue;

            var puddleCoords = xform.Coordinates;
            var targetsNearPuddle = _lookup.GetEntitiesInRange(puddleCoords, args.TargetRange)
                .Where(target => target != uid
                                 && target != entity
                                 && HasComp<DamageableComponent>(target)
                                 && HasComp<BloodstreamComponent>(target)
                                 && !_container.IsEntityOrParentInContainer(target))
                .ToList();

            if (targetsNearPuddle.Count > 0)
                bloodPuddlesWithTargets[entity] = targetsNearPuddle;
        }

        foreach (var (puddleUid, targets) in bloodPuddlesWithTargets)
        {
            var puddleCoords = Transform(puddleUid).Coordinates;

            // // Spawn spike visual effect at each blood puddle нету пока, спрайтят
            // Spawn("VampireBloodSpikesVisual", puddleCoords);

            foreach (var targetUid in targets)
                ApplyDamage(targetUid, "Blunt", args.Damage, uid);
        }

        _popup.PopupEntity(Loc.GetString("action-vampire-blood-eruption-activated"), uid, uid);
        args.Handled = true;
    }

    private void OnBloodBringersRite(EntityUid uid, VampireComponent comp, ref VampireBloodBringersRiteActionEvent args)
    {
        if (args.Handled || !comp.ActionEntities.TryGetValue("ActionVampireBloodBringersRite", out var actionEntity) || !TryComp<HemomancerComponent>(uid, out var hemomancer))
            return;

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
            if (entity != uid && HasComp<HumanoidAppearanceComponent>(entity) && HasComp<BloodstreamComponent>(entity))
                currentTargets.Add(entity);

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