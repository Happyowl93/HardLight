using System.Linq;
using Content.Server._Starlight.Antags.Vampires.Components;
using Content.Shared._Starlight.Antags.Vampires;
using Content.Shared._Starlight.Antags.Vampires.Components;
using Content.Shared._Starlight.Antags.Vampires.Components.Classes;
using Content.Shared.Bed.Sleep;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.CombatMode.Pacification;
using Content.Shared.DoAfter;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement;
using Content.Shared.Mobs.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Stealth.Components;
using Content.Shared.Stunnable;
using Robust.Shared.Timing;
using Content.Shared.Mindshield.Components;

namespace Content.Server._Starlight.Antags.Vampires;

public sealed partial class VampireSystem : EntitySystem
{
    private const string EnthrallActionId = "ActionVampireEnthrall";
    private const string RallyThrallsActionId = "ActionVampireRallyThralls";
    private const string BloodBondActionId = "ActionVampireBloodBond";
    private const string MassHysteriaActionId = "ActionVampireMassHysteria";

    private void InitializeDantalion()
    {
        SubscribeLocalEvent<VampireComponent, VampireEnthrallActionEvent>(OnEnthrall);
        SubscribeLocalEvent<VampireComponent, VampireEnthrallDoAfterEvent>(OnEnthrallDoAfter);
        SubscribeLocalEvent<VampireThrallComponent, ComponentShutdown>(OnThrallShutdown);
        SubscribeLocalEvent<DantalionComponent, ComponentShutdown>(OnDantalionShutdown);

        SubscribeLocalEvent<VampireComponent, VampirePacifyActionEvent>(OnPacify);
        SubscribeLocalEvent<VampireComponent, VampireSubspaceSwapActionEvent>(OnSubspaceSwap);
        SubscribeLocalEvent<VampireComponent, VampireDecoyActionEvent>(OnDecoy);
        SubscribeLocalEvent<VampireDecoyComponent, DamageChangedEvent>(OnDecoyDamaged);

        SubscribeLocalEvent<VampireComponent, VampireRallyThrallsActionEvent>(OnRallyThralls);
        SubscribeLocalEvent<VampireComponent, VampireBloodBondActionEvent>(OnBloodBond);
        SubscribeLocalEvent<VampireComponent, VampireMassHysteriaActionEvent>(OnMassHysteria);
    }

    private void OnEnthrall(EntityUid uid, VampireComponent comp, ref VampireEnthrallActionEvent args)
    {
        if (args.Handled || !Exists(args.Target))
            return;

        if (!TryGetEnthrallData(uid, comp, out var dantalion, out _, out var bloodCost))
            return;

        var target = args.Target;

        if (!IsValidEnthrallTarget(uid, target))
        {
            _popup.PopupEntity(Loc.GetString("vampire-enthrall-invalid"), uid, uid, PopupType.MediumCaution);
            return;
        }
    

        if (!HasThrallCapacity(comp, dantalion))
        {
            _popup.PopupEntity(Loc.GetString("vampire-enthrall-limit"), uid, uid, PopupType.MediumCaution);
            return;
        }

        if (comp.DrunkBlood < bloodCost)
        {
            _popup.PopupEntity(Loc.GetString("vampire-not-enough-blood"), uid, uid, PopupType.MediumCaution);
            return;
        }

        var doAfter = new DoAfterArgs(EntityManager, uid, TimeSpan.FromSeconds(args.ChannelTime), new VampireEnthrallDoAfterEvent { BloodCost = bloodCost }, uid, target)
        {
            DistanceThreshold = 2.5f,
            BreakOnDamage = true,
            BreakOnMove = true,
            BreakOnWeightlessMove = true,
            MovementThreshold = 0.1f,
            RequireCanInteract = true,
            BlockDuplicate = true,
            CancelDuplicate = true
        };

        if (!_doAfter.TryStartDoAfter(doAfter))
            return;

        args.Handled = true;
        _popup.PopupEntity(Loc.GetString("vampire-enthrall-start", ("target", Identity.Entity(target, EntityManager))), uid, uid);
    }

    private void OnEnthrallDoAfter(EntityUid uid, VampireComponent comp, ref VampireEnthrallDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Target == null)
            return;

        if (!TryComp(uid, out DantalionComponent? dantalion))
            return;

        var target = args.Target.Value;

        if (!IsValidEnthrallTarget(uid, target))
        {
            _popup.PopupEntity(Loc.GetString("vampire-enthrall-invalid"), uid, uid, PopupType.SmallCaution);
            return;
        }

        if (!HasThrallCapacity(comp, dantalion))
        {
            _popup.PopupEntity(Loc.GetString("vampire-enthrall-limit"), uid, uid, PopupType.SmallCaution);
            return;
        }

        if (!CheckAndConsumeBloodCost(uid, comp, null, args.BloodCost))
            return;

        var thrallComp = EnsureComp<VampireThrallComponent>(target);
        thrallComp.Master = uid;
        Dirty(target, thrallComp);

        dantalion.Thralls.Add(target);

        _popup.PopupEntity(Loc.GetString("vampire-enthrall-success", ("target", Identity.Entity(target, EntityManager))), uid, uid);
        _popup.PopupEntity(Loc.GetString("vampire-enthrall-target"), target, target, PopupType.Medium);
        args.Handled = true;
    }

    private void OnThrallShutdown(EntityUid uid, VampireThrallComponent component, ComponentShutdown args)
    {
        if (component.Master is not { } master || !TryComp(master, out DantalionComponent? dantalion))
            return;

        if (!dantalion.Thralls.Remove(uid))
            return;

        if (!TerminatingOrDeleted(uid))
            _popup.PopupEntity(Loc.GetString("vampire-thrall-released"), uid, uid, PopupType.SmallCaution);
    }

    private void OnDantalionShutdown(EntityUid uid, DantalionComponent component, ComponentShutdown args)
        => ReleaseAllThralls(uid, component);

    private void ReleaseAllThralls(EntityUid uid, DantalionComponent component)
    {
        if (component.Thralls.Count == 0)
            return;

        foreach (var thrall in component.Thralls.ToArray())
            ReleaseThrall(uid, component, thrall);
    }

    private void ReleaseThrall(EntityUid master, DantalionComponent component, EntityUid thrall)
    {
        if (!TryComp<VampireThrallComponent>(thrall, out var thrallComp) || thrallComp.Master != master)
        {
            component.Thralls.Remove(thrall);
            return;
        }

        RemComp<VampireThrallComponent>(thrall);
    }

    private bool TryGetEnthrallData(EntityUid uid, VampireComponent comp, out DantalionComponent dantalion, out EntityUid actionEntity, out int bloodCost)
    {
        bloodCost = 0;
        actionEntity = EntityUid.Invalid;
        dantalion = default!;

        if (comp.ChosenClass != VampireClassType.Dantalion)
            return false;

        if (!TryComp(uid, out DantalionComponent? dantalionComp))
            return false;

        dantalion = dantalionComp!;

        if (!comp.ActionEntities.TryGetValue(EnthrallActionId, out actionEntity))
            return false;

        if (!TryComp<VampireActionComponent>(actionEntity, out var actionComp))
            return false;

        bloodCost = (int)Math.Max(actionComp.BloodCost, 0);
        return true;
    }

    private bool IsValidEnthrallTarget(EntityUid uid, EntityUid target)
    {
        if (!Exists(target) || target == uid)
            return false;

        if (!HasComp<HumanoidAppearanceComponent>(target))
            return false;

        if (!TryComp<MobStateComponent>(target, out var mobState) || mobState.CurrentState == Shared.Mobs.MobState.Dead)
            return false;

        if (HasComp<VampireComponent>(target) || HasComp<VampireThrallComponent>(target))
            return false;

        if(HasComp<MindShieldComponent>(target))
            return false;

        return true;
    }

    private bool HasThrallCapacity(VampireComponent comp, DantalionComponent dantalion)
        => dantalion.Thralls.Count < GetThrallLimit(comp, dantalion);

    private int GetThrallLimit(VampireComponent comp, DantalionComponent dantalion)
    {
        var limit = dantalion.BaseThrallLimit;

        if (comp.TotalBlood >= 400)
            limit++;

        if (comp.TotalBlood >= 600)
            limit++;

        if (comp.FullPower)
            limit++;

        return limit;
    }
    public void OnPacify(EntityUid uid, VampireComponent comp, ref VampirePacifyActionEvent args)
    {
        if (args.Handled || !Exists(args.Target))
            return;

        var target = args.Target;

        if (!IsValidEnthrallTarget(uid, target))
        {
            _popup.PopupEntity(Loc.GetString("vampire-pacify-invalid"), uid, uid, PopupType.MediumCaution);
            return;
        }

        if (!comp.ActionEntities.TryGetValue("ActionVampirePacify", out var actionEntity)
            || !CheckAndConsumeBloodCost(uid, comp, actionEntity))
            return;

        var duration = TimeSpan.FromSeconds(args.PacifyDuration);

        EnsureComp<PacifiedComponent>(target);

        Timer.Spawn(duration, () =>
        {
            if (Exists(target))
                RemComp<PacifiedComponent>(target);
        });

        _popup.PopupEntity(Loc.GetString("vampire-pacify-success", ("target", Identity.Entity(target, EntityManager))), uid, uid);
        _popup.PopupEntity(Loc.GetString("vampire-pacify-target", ("duration", Math.Round(args.PacifyDuration))), target, target, PopupType.Medium);
        args.Handled = true;
    }

    private void OnSubspaceSwap(EntityUid uid, VampireComponent comp, ref VampireSubspaceSwapActionEvent args)
    {
        if (args.Handled || !Exists(args.Target))
            return;

        var target = args.Target;

        if (HasComp<VampireThrallComponent>(target))
        {
            _popup.PopupEntity(Loc.GetString("vampire-subspace-swap-thrall"), uid, uid, PopupType.MediumCaution);
            return;
        }

        if (!TryComp<MobStateComponent>(target, out var targetMobState) || targetMobState.CurrentState == Shared.Mobs.MobState.Dead)
        {
            _popup.PopupEntity(Loc.GetString("vampire-subspace-swap-dead"), uid, uid, PopupType.MediumCaution);
            return;
        }

        if (!comp.ActionEntities.TryGetValue("ActionVampireSubspaceSwap", out var actionEntity)
            || !CheckAndConsumeBloodCost(uid, comp, actionEntity))
            return;

        if (!TryComp<MobStateComponent>(uid, out var performerMobState) || performerMobState.CurrentState == Shared.Mobs.MobState.Dead)
            return;

        if (!_transform.SwapPositions(uid, target))
        {
            _popup.PopupEntity(Loc.GetString("vampire-subspace-swap-failed"), uid, uid, PopupType.SmallCaution);
            return;
        }

        var slowSeconds = Math.Max(0f, args.SlowDuration);
        if (slowSeconds > 0f)
        {
            var multiplier = Math.Clamp(args.SlowMultiplier, 0.05f, 1f);
            _movementMod.TryAddMovementSpeedModDuration(target, MovementModStatusSystem.FlashSlowdown, TimeSpan.FromSeconds(slowSeconds), multiplier);
        }

        ApplyHysteriaVision(target, uid, args.HysteriaDuration);

        _popup.PopupEntity(Loc.GetString("vampire-subspace-swap-success", ("target", Identity.Entity(target, EntityManager))), uid, uid);
        _popup.PopupEntity(Loc.GetString("vampire-subspace-swap-target"), target, target, PopupType.Medium);
        args.Handled = true;
    }

    private void OnDecoy(EntityUid uid, VampireComponent comp, ref VampireDecoyActionEvent args)
    {
        if (args.Handled)
            return;

        if (!comp.ActionEntities.TryGetValue("ActionVampireDecoy", out var actionEntity)
            || !CheckAndConsumeBloodCost(uid, comp, actionEntity))
            return;

        var stealth = EnsureComp<StealthComponent>(uid);
        _stealth.SetEnabled(uid, true, stealth);
        _stealth.SetVisibility(uid, -1f, stealth);

        var invisDuration = Math.Max(0f, args.InvisibilityDuration);
        if (invisDuration > 0f)
        {
            Timer.Spawn(TimeSpan.FromSeconds(invisDuration), () =>
            {
                if (Exists(uid))
                    _stealth.SetEnabled(uid, false);
            });
        }

        var xform = Transform(uid);
        var spawnCoords = _transform.GetMapCoordinates(xform);

        var decoy = EntityManager.SpawnEntity("VampireDecoyEntity", spawnCoords);

        if (TryComp<VampireDecoyAppearanceComponent>(decoy, out var decoyAppearance))
        {
            decoyAppearance.Source = uid;
            Dirty(decoy, decoyAppearance);
        }

        if (TryComp(uid, out MetaDataComponent? performerMeta))
            _metaData.SetEntityName(decoy, performerMeta.EntityName);

        var decoyComp = EnsureComp<VampireDecoyComponent>(decoy);
        decoyComp.Detonated = false;

        // Set lifetime
        var life = Math.Max(0f, args.DecoyDuration);
        if (life > 0f)
        {
            var timed = EnsureComp<Robust.Shared.Spawners.TimedDespawnComponent>(decoy);
            timed.Lifetime = life;
        }

        args.Handled = true;
    }

    private void OnDecoyDamaged(EntityUid uid, VampireDecoyComponent component, DamageChangedEvent args)
    {
        if (component.Detonated)
            return;

        if (args.DamageDelta == null || !args.DamageDelta.AnyPositive())
            return;

        component.Detonated = true;
        TriggerDecoyFlash(uid);
    }

    #region Rally Thralls

    private void OnRallyThralls(EntityUid uid, VampireComponent comp, ref VampireRallyThrallsActionEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<DantalionComponent>(uid, out var dantalion))
            return;

        if (!comp.ActionEntities.TryGetValue(RallyThrallsActionId, out var actionEntity)
            || !CheckAndConsumeBloodCost(uid, comp, actionEntity))
            return;

        var coords = Transform(uid).Coordinates;
        var ralliedCount = 0;

        foreach (var thrall in dantalion.Thralls.ToArray())
        {
            if (!Exists(thrall))
                continue;

            var thrallCoords = Transform(thrall).Coordinates;
            if (!thrallCoords.TryDistance(EntityManager, _transform, coords, out var distance) || distance > args.Range)
                continue;

            // Remove stun
            if (HasComp<StunnedComponent>(thrall))
                RemComp<StunnedComponent>(thrall);

            // Remove sleep
            if (HasComp<SleepingComponent>(thrall))
                RemComp<SleepingComponent>(thrall);

            // Restore stamina
            if (TryComp<StaminaComponent>(thrall, out var stamina))
            {
                _stamina.TakeStaminaDamage(thrall, -stamina.StaminaDamage, stamina);
            }
            
            EntityManager.SpawnAttachedTo(dantalion.rallyOverlayEffect, thrallCoords);

            ralliedCount++;
        }

        if (ralliedCount > 0)
        {
            _popup.PopupEntity(Loc.GetString("vampire-rally-thralls-success", ("count", ralliedCount)), uid, uid);
        }
        else
        {
            _popup.PopupEntity(Loc.GetString("vampire-rally-thralls-none"), uid, uid, PopupType.SmallCaution);
        }

        args.Handled = true;
    }

    #endregion

    #region Blood Bond

    private void OnBloodBond(EntityUid uid, VampireComponent comp, ref VampireBloodBondActionEvent args)
    {
        if (args.Handled)
            return;

        if (!comp.ActionEntities.TryGetValue(BloodBondActionId, out var actionEntity)
            || !TryComp<DantalionComponent>(uid, out var dantalion))
            return;

        if (dantalion.BloodBondActive)
        {
            DeactivateBloodBond(uid, dantalion);
            _popup.PopupEntity(Loc.GetString("vampire-blood-bond-stop"), uid, uid);
        }
        else
        {
            if (dantalion.Thralls.Count == 0)
            {
                _popup.PopupEntity(Loc.GetString("vampire-blood-bond-no-thralls"), uid, uid, PopupType.MediumCaution);
                return;
            }

            ActivateBloodBond(uid, comp, dantalion, args.Range, args.BloodCostPerSecond, args.TickInterval);
            _popup.PopupEntity(Loc.GetString("vampire-blood-bond-start"), uid, uid);
        }

        if (_actions.GetAction(actionEntity) is { } action)
            _actions.SetToggled(action.AsNullable(), dantalion.BloodBondActive);

        args.Handled = true;
    }

    private void ActivateBloodBond(EntityUid uid, VampireComponent _, DantalionComponent dantalion, float range, float bloodCostPerSecond, float tickInterval)
    {
        dantalion.BloodBondActive = true;
        dantalion.BloodBondLoopId++;
        dantalion.BloodBondLinkedThralls.Clear();

        var beamComp = EnsureComp<VampireBloodBondBeamComponent>(uid);
        beamComp.ActiveBeams.Clear();

        Dirty(uid, dantalion);

        StartBloodBondLoop(uid, range, bloodCostPerSecond, tickInterval);
    }

    private void DeactivateBloodBond(EntityUid uid, DantalionComponent dantalion)
    {
        dantalion.BloodBondActive = false;
        dantalion.BloodBondLinkedThralls.Clear();

        if (TryComp<VampireBloodBondBeamComponent>(uid, out var beamComp))
        {
            foreach (var connection in beamComp.ActiveBeams.Values)
            {
                var removeEvent = new VampireBloodBondBeamEvent(GetNetEntity(connection.Source), GetNetEntity(connection.Target), false);
                RaiseNetworkEvent(removeEvent);
            }
            beamComp.ActiveBeams.Clear();
        }

        Dirty(uid, dantalion);
    }

    private void StartBloodBondLoop(EntityUid uid, float range, float bloodCostPerSecond, float tickInterval)
    {
        if (!Exists(uid)
            || !TryComp<VampireComponent>(uid, out var comp)
            || !TryComp<DantalionComponent>(uid, out var dantalion)
            || !dantalion.BloodBondActive)
            return;

        if (TryComp<MobStateComponent>(uid, out var mobState) &&
            mobState.CurrentState == Shared.Mobs.MobState.Dead)
        {
            DeactivateBloodBond(uid, dantalion);
            if (comp.ActionEntities.TryGetValue(BloodBondActionId, out var actionEntity) &&
                _actions.GetAction(actionEntity) is { } action)
                _actions.SetToggled(action.AsNullable(), false);
            return;
        }

        var bloodCost = (int)Math.Ceiling(bloodCostPerSecond * tickInterval);
        if (comp.DrunkBlood < bloodCost)
        {
            DeactivateBloodBond(uid, dantalion);
            _popup.PopupEntity(Loc.GetString("vampire-blood-bond-stop-blood"), uid, uid);
            if (comp.ActionEntities.TryGetValue(BloodBondActionId, out var actionEntity) &&
                _actions.GetAction(actionEntity) is { } action)
                _actions.SetToggled(action.AsNullable(), false);
            return;
        }

        comp.DrunkBlood -= bloodCost;
        Dirty(uid, comp);
        UpdateVampireAlert(uid);

        // Find thralls in range
        var coords = Transform(uid).Coordinates;
        var linkedThralls = new List<EntityUid>();

        foreach (var thrall in dantalion.Thralls.ToArray())
        {
            if (!Exists(thrall))
                continue;

            var thrallCoords = Transform(thrall).Coordinates;
            if (!thrallCoords.TryDistance(EntityManager, _transform, coords, out var distance) || distance > range)
                continue;

            if (TryComp<MobStateComponent>(thrall, out var thrallMobState) &&
                thrallMobState.CurrentState != Shared.Mobs.MobState.Dead)
            {
                linkedThralls.Add(thrall);
            }
        }

        dantalion.BloodBondLinkedThralls = linkedThralls.ToHashSet();
        UpdateBloodBondBeamNetwork(uid, linkedThralls, range);

        // Apply damage sharing logic
        if (linkedThralls.Count > 0)
        {
            ApplyBloodBondDamageSharing(uid, linkedThralls);
        }

        var expectedLoopId = dantalion.BloodBondLoopId;

        Timer.Spawn(TimeSpan.FromSeconds(tickInterval), () =>
        {
            if (!Exists(uid) || !TryComp<DantalionComponent>(uid, out var d2)) return;
            if (!d2.BloodBondActive || d2.BloodBondLoopId != expectedLoopId) return;
            StartBloodBondLoop(uid, range, bloodCostPerSecond, tickInterval);
        });
    }
    /// <summary>
    /// Applies damage sharing among the vampire and linked thralls
    /// </summary>
    private void ApplyBloodBondDamageSharing(EntityUid vampire, List<EntityUid> thralls)
    {
        var participants = new List<EntityUid> { vampire };
        participants.AddRange(thralls);

        // Calculate average health ratio
        var totalHealthRatio = 0f;
        var validParticipants = new List<(EntityUid entity, DamageableComponent damageable, float healthRatio)>();

        foreach (var participant in participants)
        {
            if (!TryComp<DamageableComponent>(participant, out var damageable))
                continue;

            var totalDamage = damageable.TotalDamage.Float();
            var maxHealth = 100f; // Default max health

            var healthRatio = Math.Max(0f, 1f - (totalDamage / maxHealth));
            totalHealthRatio += healthRatio;
            validParticipants.Add((participant, damageable, healthRatio));
        }

        if (validParticipants.Count < 2)
            return;

        var averageHealthRatio = totalHealthRatio / validParticipants.Count;

        // Apply healing/damage based on deviation from average
        foreach (var (entity, _, healthRatio) in validParticipants)
        {
            var deviation = healthRatio - averageHealthRatio;
            var adjustmentAmount = Math.Abs(deviation) * 5f; // 5 damage/heal per tick per deviation unit

            if (adjustmentAmount < 0.1f)
                continue;

            if (deviation > 0)
            {
                // Health above average - take damage
                ApplyDamage(entity, "Blunt", adjustmentAmount, vampire);
            }
            else
            {
                // Health below average - heal
                ApplyHealing(entity, _bruteGroupId, adjustmentAmount * 0.7f, true);
                ApplyHealing(entity, _burnGroupId, adjustmentAmount * 0.3f, true);
            }
        }
    }

    private void UpdateBloodBondBeamNetwork(EntityUid vampire, List<EntityUid> targets, float range)
    {
        if (!TryComp<VampireBloodBondBeamComponent>(vampire, out var beamComp))
            return;

        var requiredTargets = new HashSet<EntityUid>(targets);

        var toRemove = new List<EntityUid>();
        foreach (var (targetKey, connection) in beamComp.ActiveBeams)
        {
            if (connection.Source != vampire)
            {
                var removeLegacy = new VampireBloodBondBeamEvent(GetNetEntity(connection.Source), GetNetEntity(connection.Target), false);
                RaiseNetworkEvent(removeLegacy);
                toRemove.Add(targetKey);
                continue;
            }

            if (!requiredTargets.Contains(connection.Target))
            {
                var removeEvent = new VampireBloodBondBeamEvent(GetNetEntity(connection.Source), GetNetEntity(connection.Target), false);
                RaiseNetworkEvent(removeEvent);
                toRemove.Add(targetKey);
            }
        }

        foreach (var key in toRemove)
            beamComp.ActiveBeams.Remove(key);

        foreach (var target in requiredTargets)
        {
            if (!beamComp.ActiveBeams.ContainsKey(target))
            {
                var connection = new BloodBondBeamConnection(vampire, target, range);
                beamComp.ActiveBeams[target] = connection;

                var createEvent = new VampireBloodBondBeamEvent(GetNetEntity(vampire), GetNetEntity(target), true);
                RaiseNetworkEvent(createEvent);
            }
        }
    }

    #endregion

    #region Mass Hysteria

    private void OnMassHysteria(EntityUid uid, VampireComponent comp, ref VampireMassHysteriaActionEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<DantalionComponent>(uid, out var dantalion))
            return;

        if (!comp.ActionEntities.TryGetValue(MassHysteriaActionId, out var actionEntity)
            || !CheckAndConsumeBloodCost(uid, comp, actionEntity))
            return;

        var coords = Transform(uid).Coordinates;

        // Find all humanoids in range
        var query = EntityQueryEnumerator<HumanoidAppearanceComponent, MobStateComponent, TransformComponent>();

        while (query.MoveNext(out var target, out _, out var mobState, out var xform))
        {
            if (target == uid)
                continue;

            // Skip dead entities
            if (mobState.CurrentState == Shared.Mobs.MobState.Dead)
                continue;

            if (!xform.Coordinates.TryDistance(EntityManager, _transform, coords, out var distance) || distance > args.Range)
                continue;

            if (TryComp<VampireThrallComponent>(target, out var _))
                continue;

            // Apply flash effect
            _flash.Flash(target, uid, null, TimeSpan.FromSeconds(args.FlashDuration), 0.8f, false);

            // Apply hysteria effect
            ApplyHysteriaVision(target, uid, args.HysteriaDuration);

        }

        args.Handled = true;
    }

    /// <summary>
    /// Applies hysteria vision effect to a target, making them see other humanoids as monsters.
    /// </summary>
    private void ApplyHysteriaVision(EntityUid target, EntityUid source, float duration)
    {
        var hysteria = EnsureComp<HysteriaVisionComponent>(target);
        hysteria.EndTime = _timing.CurTime + TimeSpan.FromSeconds(duration);
        hysteria.Source = source;
        Dirty(target, hysteria);
    }

    #endregion
}