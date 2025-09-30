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
using Robust.Shared.Physics;
using Content.Shared.Body.Components;
using Content.Shared.Damage.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Physics;
using Content.Shared.Stealth.Components;
using Robust.Shared.Audio;
using Content.Shared._Starlight.Antags.Vampires.Components.Classes;

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
    }

    private static readonly Vector2[] _tendrilOffsets = new Vector2[]
    {
        new(-1, -1), new(0, -1), new(1, -1),
        new(-1,  0), new(0,  0), new(1,  0),
        new(-1,  1), new(0,  1), new(1,  1),
    };

    private void OnHemomancerClaws(EntityUid uid, VampireComponent comp, ref VampireHemomancerClawsActionEvent args)
    {
        if (args.Handled)
            return;

        if (!ValidateVampireAbility(uid, out var validatedComp, VampireClassType.Hemomancer, comp.Actions.HemomancerClawsActionEntity))
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

        if (!TryComp<VampireComponent>(args.Performer, out var comp)
            || !ValidateVampireClass(args.Performer, comp, VampireClassType.Hemomancer)
            || !CheckAndConsumeActionCost(args.Performer, comp, comp.Actions.HemomancerTendrilsActionEntity))
            return;

        args.Handled = true;

        var targetCoords = args.Target;
        var tileCoords = targetCoords.WithPosition(targetCoords.Position.Floored() + new Vector2(args.PositionOffset, args.PositionOffset));

        if (!ValidateTendrilTarget(tileCoords, args.Performer))
            return;

        if (args.SpawnVisuals)
            SpawnTendrilVisuals(tileCoords);

        ScheduleTendrilEffect(args, tileCoords);
    }

    private bool ValidateTendrilTarget(EntityCoordinates tileCoords, EntityUid performer)
    {
        if (_transform.GetGrid(tileCoords) is not { } gridUid ||
            !TryComp<MapGridComponent>(gridUid, out var gridComp) ||
            !IsValidTile(tileCoords, gridUid, gridComp))
        {
            _popup.PopupEntity("Cannot cast there.", performer, performer);
            return false;
        }
        return true;
    }

    private void SpawnTendrilVisuals(EntityCoordinates tileCoords)
    {
        var gridUid = _transform.GetGrid(tileCoords);
        if (gridUid == null || !TryComp<MapGridComponent>(gridUid.Value, out var gridComp))
            return;

        foreach (var offset in _tendrilOffsets)
        {
            var coords = tileCoords.Offset(offset);
            if (IsValidTile(coords, gridUid.Value, gridComp))
                EntityManager.SpawnEntity("VampireBloodTendrilVisual", coords);
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
        if (!Exists(performerUid))
            return;

        // Spawn blood puddle at center
        if (IsValidTile(targetCoords, gridUid, gridComp))
            Spawn("PuddleBlood", targetCoords);

        // Process damage and effects
        var hitEnemies = ProcessTendrilDamage(performerUid, targetCoords, gridUid, gridComp, toxinDamage, slowDuration, slowMultiplier, targetRange);

        // Schedule visual effects for hit enemies
        if (hitEnemies.Count > 0)
            Timer.Spawn(TimeSpan.FromSeconds(spawnDelay), () => SpawnTendrilEffectsOnEnemies(hitEnemies, gridUid, gridComp, positionOffset));
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

    private void SpawnTendrilEffectsOnEnemies(List<EntityUid> hitEnemies, EntityUid gridUid, MapGridComponent gridComp, float positionOffset)
    {
        foreach (var enemy in hitEnemies)
        {
            if (!Exists(enemy))
                continue;

            var enemyCoords = Transform(enemy).Coordinates;
            EntityManager.SpawnEntity("VampireBloodTendrilVisual", enemyCoords);

            var enemyTileCoords = enemyCoords.WithPosition(enemyCoords.Position.Floored() + new Vector2(positionOffset, positionOffset));
            if (IsValidTile(enemyTileCoords, gridUid, gridComp))
                Spawn("PuddleBlood", enemyTileCoords);
        }
    }

    private void OnBloodBarrier(VampireBloodBarrierActionEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<VampireComponent>(args.Performer, out var comp))
            return;

        if (!ValidateVampireClass(args.Performer, comp, VampireClassType.Hemomancer))
            return;

        if (!CheckAndConsumeBloodCost(args.Performer, comp, comp.Actions.BloodBarrierActionEntity))
            return;

        args.Handled = true;

        var targetCoords = args.Target;
        var tileCoords = targetCoords.WithPosition(targetCoords.Position.Floored() + new Vector2(0.5f, 0.5f));

        if (_transform.GetGrid(tileCoords) is not { } gridUid ||
            !TryComp<MapGridComponent>(gridUid, out var gridComp))
        {
            _popup.PopupEntity("Cannot place barriers there", args.Performer, args.Performer);
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

            var barrier = EntityManager.SpawnEntity("VampireBloodBarrier", barrierCoords);

            var preventCollide = EnsureComp<PreventCollideComponent>(barrier);
            preventCollide.Uid = args.Performer;

            successfulBarriers++;

        }

        if (successfulBarriers == 0)
            _popup.PopupEntity("Cannot place barriers there.", args.Performer, args.Performer);
    }

    // Rinary у меня лапки, помоги с этим говном пж
    // Сделай так, чтобы вампир мог превратиться в кровавую лужу на 8 секунд
    // В этой форме он невидим, неуязвим и может проходить через всё, кроме стен и тайлов космоса
    // Но не может атаковать, пить кровь и использовать способности
    // Реализация ниже, ну говно если так выразится
    // Jaunt в теории надо оформить, но как сделать так чтобы он через стены не проходил я хз
    private void OnSanguinePool(EntityUid uid, VampireComponent comp, ref VampireSanguinePoolActionEvent args)
    {
        if (args.Handled || !TryComp<HemomancerComponent>(uid, out var hemomancer))
            return;

        if (hemomancer.InSanguinePool)
        {
            _popup.PopupEntity("You are already in sanguine pool form!", uid, uid);
            return;
        }

        // Dont allow pooling? in invalid tiles
        var curCoords = Transform(uid).Coordinates;
        if (!IsValidTile(curCoords))
        {
            _popup.PopupEntity("You cannot become a blood pool here.", uid, uid);
            return;
        }

        if (!CheckAndConsumeBloodCost(uid, comp, comp.Actions.SanguinePoolActionEntity))
            return;

        EnterSanguinePool(uid, hemomancer, args.Duration, args.BloodDripInterval);
        args.Handled = true;
    }

    private void EnterSanguinePool(EntityUid uid, HemomancerComponent comp, int duration, float interval)
    {
        comp.InSanguinePool = true;
        Dirty(uid, comp);

        Spawn("VampireSanguinePoolOut", Transform(uid).Coordinates);

        var stealth = EnsureComp<StealthComponent>(uid);
        _stealth.SetVisibility(uid, -1f, stealth);

        if (!HasComp<GodmodeComponent>(uid))
        {
            EnsureComp<GodmodeComponent>(uid);
            comp.PoolOwnedGodmode = true;
        }
        else
            comp.PoolOwnedGodmode = false;

        if (TryComp<FixturesComponent>(uid, out var fixtures) && fixtures.FixtureCount > 0)
        {
            comp.PoolOriginalMasks = new();
            comp.PoolOriginalLayers = new();
            foreach (var (id, fix) in fixtures.Fixtures)
            {
                comp.PoolOriginalMasks[id] = fix.CollisionMask;
                comp.PoolOriginalLayers[id] = fix.CollisionLayer;
                var newMask = (int)CollisionGroup.Impassable | (int)CollisionGroup.GhostImpassable;
                _physics.SetCollisionMask(uid, id, fix, newMask, fixtures);
                var newLayer = 0;
                _physics.SetCollisionLayer(uid, id, fix, newLayer, fixtures);
            }
        }

        Timer.Spawn(TimeSpan.FromSeconds(duration), () =>
        {
            if (Exists(uid) && TryComp<HemomancerComponent>(uid, out var hemomancer))
                ExitSanguinePool(uid, hemomancer);
        });

        _popup.PopupEntity("You transform into a pool of blood!", uid, uid);

        var enterSound = new SoundPathSpecifier("/Audio/_Starlight/Effects/vampire/enter_blood.ogg");
        _audio.PlayPvs(enterSound, uid, AudioParams.Default.WithVolume(-2f));

        StartSanguinePoolBloodDrip(uid, interval, 0);
    }

    private void ExitSanguinePool(EntityUid uid, HemomancerComponent comp)
    {
        if (!comp.InSanguinePool)
            return;

        comp.InSanguinePool = false;
        Dirty(uid, comp);

        Spawn("VampireSanguinePoolIn", Transform(uid).Coordinates);

        if (HasComp<StealthComponent>(uid))
            RemComp<StealthComponent>(uid);

        // invul
        if (comp.PoolOwnedGodmode && HasComp<GodmodeComponent>(uid))
            RemComp<GodmodeComponent>(uid);
        comp.PoolOwnedGodmode = false;

        if (TryComp<FixturesComponent>(uid, out var fixtures) &&
            comp.PoolOriginalMasks != null && comp.PoolOriginalLayers != null)
        {
            foreach (var (id, fix) in fixtures.Fixtures)
            {
                if (comp.PoolOriginalMasks.TryGetValue(id, out var mask))
                    _physics.SetCollisionMask(uid, id, fix, mask, fixtures);
                if (comp.PoolOriginalLayers.TryGetValue(id, out var layer))
                    _physics.SetCollisionLayer(uid, id, fix, layer, fixtures);
            }
            comp.PoolOriginalMasks = null;
            comp.PoolOriginalLayers = null;
        }

        _popup.PopupEntity("You reform from the blood pool!", uid, uid);
        var exitSound = new SoundPathSpecifier("/Audio/_Starlight/Effects/vampire/exit_blood.ogg");
        _audio.PlayPvs(exitSound, uid, AudioParams.Default.WithVolume(-2f));
    }

    private void StartSanguinePoolBloodDrip(EntityUid uid, float interval, int tickCount = 0)
    {
        const int MaxTicks = 80;

        if (tickCount >= MaxTicks || !Exists(uid))
            return;

        if (!TryComp<HemomancerComponent>(uid, out var h) || !h.InSanguinePool)
            return;

        var coords = Transform(uid).Coordinates;
        if (IsValidTile(coords))
            Spawn("PuddleBlood", coords);

        Timer.Spawn(TimeSpan.FromSeconds(interval), () => StartSanguinePoolBloodDrip(uid, interval, tickCount + 1));
    }

    private void OnBloodEruption(EntityUid uid, VampireComponent comp, ref VampireBloodEruptionActionEvent args)
    {
        if (args.Handled)
            return;

        if (!CheckAndConsumeBloodCost(uid, comp, comp.Actions.BloodEruptionActionEntity))
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
                .Where(target => target != uid && target != entity && HasComp<DamageableComponent>(target))
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

        _popup.PopupEntity("You cause blood to erupt in spikes around you!", uid, uid);
        args.Handled = true;
    }

    private void OnBloodBringersRite(EntityUid uid, VampireComponent comp, ref VampireBloodBringersRiteActionEvent args)
    {
        if (args.Handled || !TryComp<HemomancerComponent>(uid, out var hemomancer))
            return;

        if (hemomancer.BloodBringersRiteActive)
        {
            DeactivateBloodBringersRite(uid, hemomancer);
            _popup.PopupEntity("Blood Bringers Rite deactivated", uid, uid); // Rinary - move to locale
        }
        else
        {
            if (!comp.FullPower)
            {
                _popup.PopupEntity("You lack full vampiric power (need above 1000 total blood & 8 unique victims)", uid, uid); // Rinary - move to locale
                return;
            }
            if (comp.DrunkBlood < args.Cost)
            {
                _popup.PopupEntity("Not enough blood to activate Blood Bringers Rite", uid, uid); // Rinary - move to locale
                return;
            }

            ActivateBloodBringersRite(uid, hemomancer, args.ToggleInterval, args.Cost, args.Range, args.Damage, args.HealBrute, args.HealBurn, args.HealStamina);
            _popup.PopupEntity("Blood Bringers Rite activated!", uid, uid); // Rinary - move to locale
        }

        if (_actions.GetAction(comp.Actions.BloodBringersRiteActionEntity) is { } action)
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

        if (tickCount >= MaxTicks || !Exists(uid))
            return;

        if (!TryComp<VampireComponent>(uid, out var comp) || !TryComp<HemomancerComponent>(uid, out var hemomancer) || !hemomancer.BloodBringersRiteActive)
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
            _popup.PopupEntity("Blood Bringers Rite deactivated - not enough blood", uid, uid);

            if (_actions.GetAction(comp.Actions.BloodBringersRiteActionEntity) is { } action)
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