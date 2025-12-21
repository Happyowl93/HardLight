using System.Numerics;
using Content.Server.Destructible;
using Content.Shared._Starlight.Antags.Vampires;
using Content.Shared._Starlight.Antags.Vampires.Components;
using Content.Shared._Starlight.Antags.Vampires.Components.Classes;
using Content.Shared.CombatMode;
using Content.Shared.Damage;
using Content.Shared.Damage.Events;
using Content.Shared.Ensnaring.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Movement.Pulling.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Prying.Components;
using Content.Shared.StatusEffectNew;
using Content.Shared.StatusEffectNew.Components;
using Content.Shared.Stunnable;
using Content.Shared.Movement.Components;
using Content.Shared.Bed.Sleep;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Timing;
using Robust.Shared.Audio;

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
        SubscribeLocalEvent<GargantuaComponent, GetMeleeDamageEvent>(OnBloodSwellMeleeDamage);
        SubscribeLocalEvent<GargantuaComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovementSpeed);
        SubscribeLocalEvent<GargantuaComponent, BeforeDamageChangedEvent>(OnBloodSwellIncomingDamage);
        SubscribeLocalEvent<GargantuaComponent, BeforeStaminaDamageEvent>(OnBloodSwellStaminaDamage);

        // Status effects are raised on the status effect entity, so hook globally
        SubscribeLocalEvent<StatusEffectComponent, StatusEffectAppliedEvent>(OnStatusEffectApplied);
    }

    private void UpdateGargantua(float frameTime)
    {
        var query = EntityQueryEnumerator<GargantuaComponent, VampireComponent>();
        var now = _timing.CurTime;

        while (query.MoveNext(out var uid, out var gargantua, out var vampire))
        {
            if (gargantua.BloodSwellActive && gargantua.BloodSwellEndTime.HasValue && now >= gargantua.BloodSwellEndTime.Value)
            {
                EndBloodSwell(uid, gargantua);
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

        gargantua.BloodSwellEnhancedThreshold = args.EnhancedThreshold;
        gargantua.BloodSwellMeleeBonusDamage = args.MeleeBonusDamage;
        
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

        _alerts.ShowAlert(uid, "VampireBloodSwell");
        Dirty(uid, gargantua);
        args.Handled = true;
    }

    private void EndBloodSwell(EntityUid uid, GargantuaComponent gargantua)
    {
        gargantua.BloodSwellActive = false;
        gargantua.BloodSwellEndTime = null;
        _alerts.ClearAlert(uid, "VampireBloodSwell");
        Dirty(uid, gargantua);
        _popup.PopupEntity(Loc.GetString("vampire-blood-swell-end"), uid, uid);
    }

    private void OnShotAttempted(EntityUid uid, GargantuaComponent component, ref ShotAttemptedEvent args)
    {
        if (component.BloodSwellActive)
        {   
            _popup.PopupEntity(Loc.GetString("vampire-blood-swell-cancel-shoot"), uid, uid);
            args.Cancel();
        }
    }

    private void OnBloodSwellMeleeDamage(EntityUid uid, GargantuaComponent component, ref GetMeleeDamageEvent args)
    {
        if (!component.BloodSwellActive)
            return;

        if (!TryComp<VampireComponent>(uid, out var vampire))
            return;

        if (args.Weapon != uid)
            return;

        // Bonus only for unarmed after 400 total blood
        if (vampire.TotalBlood < component.BloodSwellEnhancedThreshold)
            return;

        args.Damage.DamageDict.TryGetValue("Blunt", out var blunt);
        args.Damage.DamageDict["Blunt"] = blunt + component.BloodSwellMeleeBonusDamage;
    }

    private void OnRefreshMovementSpeed(EntityUid uid, GargantuaComponent component, RefreshMovementSpeedModifiersEvent args)
    {
        if (component.BloodRushActive)
        {
            args.ModifySpeed(component.BloodRushSpeedMultiplier, component.BloodRushSpeedMultiplier);
        }
    }

    private void OnBloodSwellIncomingDamage(EntityUid uid, GargantuaComponent component, ref BeforeDamageChangedEvent args)
    {
        if (!component.BloodSwellActive)
            return;

        static bool IsBrute(string id)
            => id is "Blunt" or "Slash" or "Piercing";

        static bool IsBurn(string id)
            => id is "Heat" or "Cold" or "Shock" or "Caustic";

        foreach (var (type, value) in args.Damage.DamageDict)
        {
            if (value <= 0)
                continue;

            if (IsBrute(type) || IsBurn(type))
                args.Damage.DamageDict[type] = value * 0.5f;
        }
    }

    private void OnBloodSwellStaminaDamage(EntityUid uid, GargantuaComponent component, ref BeforeStaminaDamageEvent args)
    {
        if (!component.BloodSwellActive)
            return;

        args.Value *= 0.5f;
    }

    private void OnStatusEffectApplied(EntityUid effectUid, StatusEffectComponent effect, ref StatusEffectAppliedEvent args)
    {
        if (!TryComp<GargantuaComponent>(args.Target, out var gargantua) || !gargantua.BloodSwellActive)
            return;

        var now = _timing.CurTime;

        if (!HasComp<StunnedStatusEffectComponent>(effectUid)
            && !HasComp<KnockdownStatusEffectComponent>(effectUid)
            && !HasComp<MovementModStatusEffectComponent>(effectUid)
            && !HasComp<ForcedSleepingStatusEffectComponent>(effectUid))
            return;

        if (effect.EndEffectTime is not { } end)
            return;

        var remaining = end - now;
        if (remaining <= TimeSpan.Zero)
            return;

        if (MetaData(effectUid).EntityPrototype is not { ID: var protoId })
            return;

        _statusEffects.TrySetStatusEffectDuration(args.Target, protoId, remaining / 2);
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

        // Pull tuning parameters from the action event (configured in the action prototype).
        gargantua.BloodRushSpeedMultiplier = args.SpeedMultiplier;

        if (gargantua.BloodRushActive)
        {
            // Already active, refresh duration
            gargantua.BloodRushEndTime = _timing.CurTime + TimeSpan.FromSeconds(args.Duration);
        }
        else
        {
            gargantua.BloodRushActive = true;
            gargantua.BloodRushEndTime = _timing.CurTime + TimeSpan.FromSeconds(args.Duration);

            // Refresh movement speed modifiers to apply the buff
            _movement.RefreshMovementSpeedModifiers(uid);

            _popup.PopupEntity(Loc.GetString("vampire-blood-rush-start"), uid, uid);
        }

        _alerts.ShowAlert(uid, "VampireBloodRush");
        Dirty(uid, gargantua);
        args.Handled = true;
    }

    private void EndBloodRush(EntityUid uid, GargantuaComponent gargantua)
    {
        gargantua.BloodRushActive = false;
        gargantua.BloodRushEndTime = null;
        _alerts.ClearAlert(uid, "VampireBloodRush");
        _movement.RefreshMovementSpeedModifiers(uid);
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

        // Spawn visual effect at vampire's position
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
            // Add PryingComponent to enable door prying
            var prying = EnsureComp<PryingComponent>(uid);
            prying.PryPowered = true;
            prying.Force = true;
            prying.SpeedModifier = 0.5f; // Fast prying
            
            _popup.PopupEntity(Loc.GetString("vampire-overwhelming-force-start"), uid, uid);
        }
        else
        {
            // Remove PryingComponent
            RemComp<PryingComponent>(uid);
            
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

    // Overwhelming Force now uses PryingComponent instead of manual door interaction

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

        var gridUid = xform.GridUid.Value;
        var direction = (args.Target.Position - xform.Coordinates.Position).Normalized();

        // Check if combat mode is active for pulling
        var shouldPull = TryComp<CombatModeComponent>(uid, out var combat) && combat.IsInCombatMode;

        // Calculate tiles along the path - capture values for lambda
        var maxTiles = (int)args.Range;
        var immobilizeDuration = args.ImmobilizeDuration;
        var delayPerTile = 50; // 50ms between each tile effect

        // Flag to stop spawning after hitting something
        var stopped = false;

        var SpawnSound = args.Sound;
        for (var i = 1; i <= maxTiles; i++)
        {
            var tileIndex = i;
            var tileCoords = xform.Coordinates.Offset(direction * tileIndex);

            Timer.Spawn(delayPerTile * i, () =>
            {
                if (!Exists(uid) || stopped)
                    return;

                // Check for walls/obstacles before spawning
                var blocked = false;
                var entitiesOnTile = _lookup.GetEntitiesInRange(tileCoords, 0.4f);
                foreach (var ent in entitiesOnTile)
                {
                    if (ent == uid)
                        continue;

                    // Check for static physics bodies (walls, structures)
                    if (TryComp<PhysicsComponent>(ent, out var physics) && physics.BodyType == BodyType.Static && physics.Hard)
                    {
                        blocked = true;
                        stopped = true;
                        break;
                    }
                }

                if (blocked)
                    return;

                // Spawn visual effect on the tile and play audio
                _audio.PlayPvs(SpawnSound, tileCoords, AudioParams.Default.WithVolume(3f));
                Spawn("VampireDemonicGraspEffect", tileCoords);

                // Check for mobs on this tile
                foreach (var target in entitiesOnTile)
                {
                    if (target == uid)
                        continue;

                    if (!HasComp<MobStateComponent>(target))
                        continue;

                    // apply paralyze in combat mode, otherwise immobilize
                    if (shouldPull)
                    {
                        _stun.TryAddParalyzeDuration(target, TimeSpan.FromSeconds(immobilizeDuration));
                    }
                    else
                    {
                        _stun.TryAddStunDuration(target, TimeSpan.FromSeconds(immobilizeDuration));

                        // Don't spawn the visual on targets that are already lying down
                        if (!HasComp<KnockedDownComponent>(target))
                        {
                            var attachCoords = new EntityCoordinates(target, Vector2.Zero);
                            EntityManager.SpawnAttachedTo("VampireImmobilizedEffect", attachCoords);
                        }
                    }

                    // Stop spawning further
                    stopped = true;

                    if (shouldPull && Exists(uid))
                    {
                        var vampireXform = Transform(uid);
                        var vampirePos = _transform.GetWorldPosition(vampireXform);
                        var targetXform = Transform(target);
                        var targetCurrentPos = _transform.GetWorldPosition(targetXform);
                        var pullDirection = (vampirePos - targetCurrentPos).Normalized();
                        var distance = (vampirePos - targetCurrentPos).Length();
                        _throwing.TryThrow(target, pullDirection * (distance - 1f), 8f, uid);
                        _popup.PopupEntity(Loc.GetString("vampire-demonic-grasp-pull"), uid, uid);
                    }

                    _popup.PopupEntity(Loc.GetString("vampire-demonic-grasp-hit"), target, target, PopupType.LargeCaution);
                    break;
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

        var gargantua = EnsureComp<GargantuaComponent>(uid);

        if (gargantua.IsCharging)
            return;

        if (!comp.ActionEntities.TryGetValue("ActionVampireCharge", out var actionEntity))
            return;

        if (TryComp<EnsnareableComponent>(uid, out var ensnareable) && ensnareable.IsEnsnared)
        {
            _popup.PopupEntity(Loc.GetString("vampire-legs-ensnared"), uid, uid, PopupType.Medium);
            return;
        }

        var xform = Transform(uid);
        var startPos = _transform.GetWorldPosition(xform);
        var targetPos = _transform.ToMapCoordinates(args.Target).Position;
        var delta = targetPos - startPos;
        var direction = delta.Normalized();

        if (direction == Vector2.Zero)
            return;

        if (!CheckAndConsumeBloodCost(uid, comp, actionEntity))
            return;

        if (!TryComp<PhysicsComponent>(uid, out var physics))
            return;

        gargantua.IsCharging = true;
        gargantua.ChargeDirection = new Angle(MathF.Atan2(direction.Y, direction.X));
        gargantua.ChargeDirectionVector = direction;
        gargantua.ChargeSpeed = args.ChargeSpeed;
        gargantua.ChargeCreatureDamage = args.CreatureDamage;
        gargantua.ChargeCreatureThrowDistance = args.CreatureThrowDistance;
        gargantua.ChargeStructuralDamage = args.StructuralDamage;
        gargantua.ChargeImpactSound = args.Sound;

        // Kick off movement immediately so the charge feels responsive
        _physics.SetLinearVelocity(uid, direction * args.ChargeSpeed, body: physics);

        _popup.PopupEntity(Loc.GetString("vampire-charge-start"), uid, uid);

        Dirty(uid, gargantua);
        args.Handled = true;
    }

    private void ProcessChargeMovement(EntityUid uid, GargantuaComponent gargantua, VampireComponent _, float __)
    {
        if (!TryComp<PhysicsComponent>(uid, out var physics))
        {
            EndCharge(uid, gargantua);
            return;
        }

        var xform = Transform(uid);

        // Check if were over void/space
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

        // Keep pushing forward at a constant speed
        _physics.SetLinearVelocity(uid, gargantua.ChargeDirectionVector * gargantua.ChargeSpeed, body: physics);

        // Check for obstacles ahead
        var movement = gargantua.ChargeDirectionVector * 0.5f; // Check 0.5 tiles ahead
        var obstacles = _lookup.GetEntitiesInRange(xform.Coordinates.Offset(movement), 0.5f);

        foreach (var obstacle in obstacles)
        {
            if (obstacle == uid)
                continue;

            // Never interact with contained entities
            // Otherwise we can "hit" our own items and cancel the charge.
            if (_container.IsEntityInContainer(obstacle))
                continue;

            // Check for mobs
            if (HasComp<MobStateComponent>(obstacle))
            {
                HandleChargeImpact(uid, obstacle, gargantua);
                EndCharge(uid, gargantua);
                return;
            }

            // Stop only on static obstacles that actually collide with us.
            // This prevents decorative/underfloor entities (e.g. wires) from canceling the charge
            if (TryComp<PhysicsComponent>(obstacle, out var obstaclePhysics)
                && obstaclePhysics.BodyType == BodyType.Static
                && obstaclePhysics.CanCollide
                && obstaclePhysics.Hard
                && (physics.CollisionMask & obstaclePhysics.CollisionLayer) != 0)
            {
                // Check if its a wall, destroy it
                if (HasComp<DestructibleComponent>(obstacle))
                    _destructible.DestroyEntity(obstacle);
                    _audio.PlayPvs(gargantua.ChargeImpactSound, obstacle);

                EndCharge(uid, gargantua);
                return;
            }

            // throw unanchored entities
            if (TryComp<PhysicsComponent>(obstacle, out var physicsUnanchored) && physicsUnanchored.BodyType != BodyType.Static)
            {
                _throwing.TryThrow(obstacle, gargantua.ChargeDirectionVector * gargantua.ChargeCreatureThrowDistance, 10f, uid);

                var damageSpec = new DamageSpecifier();
                damageSpec.DamageDict["Blunt"] = gargantua.ChargeCreatureDamage;
                _damageableSystem.TryChangeDamage(obstacle, damageSpec, true, origin: uid);
            }
        }
    }

    private void HandleChargeImpact(EntityUid uid, EntityUid target, GargantuaComponent gargantua)
    {
        if (gargantua.ChargeImpactSound != null)
            _audio.PlayPvs(gargantua.ChargeImpactSound, target);

        var damageSpec = new DamageSpecifier();
        damageSpec.DamageDict["Blunt"] = gargantua.ChargeCreatureDamage;
        _damageableSystem.TryChangeDamage(target, damageSpec, true, origin: uid);

        // Throw the target
        _throwing.TryThrow(target, gargantua.ChargeDirectionVector * gargantua.ChargeCreatureThrowDistance, 6f, uid);

        _stun.TryKnockdown(target, TimeSpan.FromSeconds(2), true);

        _popup.PopupEntity(Loc.GetString("vampire-charge-impact", ("target", target)), uid, uid);
    }

    private void EndCharge(EntityUid uid, GargantuaComponent gargantua)
    {
        gargantua.IsCharging = false;
        gargantua.ChargeSpeed = 0;
        gargantua.ChargeDirectionVector = default;
        gargantua.ChargeImpactSound = null;
        if (TryComp<PhysicsComponent>(uid, out var physics))
            _physics.SetLinearVelocity(uid, Vector2.Zero, body: physics);
        Dirty(uid, gargantua);
    }

    #endregion
}
