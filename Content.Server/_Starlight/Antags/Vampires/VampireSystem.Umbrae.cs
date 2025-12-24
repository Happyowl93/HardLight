using Content.Shared.Charges.Components;
using Content.Shared.DoAfter;
using Content.Shared.Ensnaring.Components;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Flash.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Stealth.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Timing;
using Content.Shared._Starlight.Antags.Vampires;
using Content.Shared._Starlight.Antags.Vampires.Components;
using Content.Shared._Starlight.Antags.Vampires.Components.Classes;
using Content.Shared.Damage.Components;
using Content.Shared.Humanoid;
using Content.Shared.Interaction;
using System.Numerics;
using Content.Shared.Physics;
using Content.Server.Light.EntitySystems;
using Content.Server.Temperature.Systems;
using Content.Shared.Light.Components;
using Content.Shared.Temperature.Components;
using Robust.Shared.Audio;

namespace Content.Server._Starlight.Antags.Vampires;

public sealed partial class VampireSystem : EntitySystem
{
    [Dependency] private readonly PoweredLightSystem _poweredLightSystem = default!;
    [Dependency] private readonly TemperatureSystem _temperatureSystem = default!;

    private void InitializeUmbrae()
    {
        SubscribeLocalEvent<VampireComponent, VampireCloakOfDarknessActionEvent>(OnCloakOfDarkness);
        SubscribeLocalEvent<ShadowSnareEnsnareComponent, ComponentShutdown>(OnShadowSnareEnsnareShutdown);

        SubscribeLocalEvent<VampireComponent, VampireShadowSnareActionEvent>(OnShadowSnare);
        SubscribeLocalEvent<VampireComponent, VampireDarkPassageActionEvent>(OnDarkPassage);
        SubscribeLocalEvent<VampireComponent, VampireExtinguishActionEvent>(OnExtinguish);
        SubscribeLocalEvent<VampireComponent, VampireEternalDarknessActionEvent>(OnEternalDarkness);
        SubscribeLocalEvent<VampireComponent, VampireShadowAnchorActionEvent>(OnShadowAnchor);
        SubscribeLocalEvent<VampireComponent, VampireShadowAnchorDoAfterEvent>(OnShadowAnchorDoAfter);
        SubscribeLocalEvent<VampireComponent, VampireShadowBoxingActionEvent>(OnShadowBoxing);
        SubscribeLocalEvent<ShadowSnareTrapComponent, InteractUsingEvent>(OnShadowSnareTrapInteractUsing);
        SubscribeLocalEvent<ShadowSnareTrapComponent, StartCollideEvent>(OnShadowSnareTrapCollide);
    }

#region Cloak of Darkness
    private void OnCloakOfDarkness(EntityUid uid, VampireComponent comp, ref VampireCloakOfDarknessActionEvent args)
    {
        if (args.Handled
            || !comp.ActionEntities.TryGetValue("ActionVampireCloakOfDarkness", out var actionEntity)
            || !TryComp<UmbraeComponent>(uid, out var umbrae)
            || !ValidateVampireClass(uid, comp, VampireClassType.Umbrae))
            return;

        if (umbrae.CloakOfDarknessActive)
        {
            DeactivateCloakOfDarkness(uid, umbrae);
            _popup.PopupEntity(Loc.GetString("action-vampire-cloak-of-darkness-stop"), uid, uid);
        }
        else
        {
            ActivateCloakOfDarkness(uid, umbrae);
            _popup.PopupEntity(Loc.GetString("action-vampire-cloak-of-darkness-start"), uid, uid);
        }

        if (_actions.GetAction(actionEntity) is { } action)
            _actions.SetToggled(action.AsNullable(), umbrae.CloakOfDarknessActive);

        args.Handled = true;
    }

    private void ActivateCloakOfDarkness(EntityUid uid, UmbraeComponent comp)
    {
        comp.CloakOfDarknessActive = true;
        Dirty(uid, comp);

        var stealth = EnsureComp<StealthComponent>(uid);
        _stealth.SetEnabled(uid, true, stealth);    
        _stealth.SetVisibility(uid, -1f, stealth);
    }

    private void DeactivateCloakOfDarkness(EntityUid uid, UmbraeComponent comp)
    {
        comp.CloakOfDarknessActive = false;
        Dirty(uid, comp);

        RemComp<StealthComponent>(uid);
        _stealth.SetEnabled(uid, false);
    }
#endregion

#region Shadow Snare
    private void OnShadowSnare(EntityUid uid, VampireComponent comp, ref VampireShadowSnareActionEvent args)
    {
        if (args.Handled
            || !comp.ActionEntities.TryGetValue("ActionVampireShadowSnare", out var actionEntity)
            || !ValidateVampireClass(uid, comp, VampireClassType.Umbrae)
            )
            return;

        var target = args.Target;
        var place = target.WithPosition(target.Position.Floored() + new Vector2(args.PositionOffset, args.PositionOffset));

        if (!_transform.GetGrid(place).HasValue)
            return;

        if (!CheckAndConsumeBloodCost(uid, comp, actionEntity))
            return;

        if (!_playerShadowSnares.TryGetValue(uid, out var playerTraps))
        {
            playerTraps = new List<EntityUid>();
            _playerShadowSnares[uid] = playerTraps;
        }

        playerTraps.RemoveAll(trap => !Exists(trap) || !HasComp<ShadowSnareTrapComponent>(trap));

        if (playerTraps.Count >= args.MaxPerPlayer)
        {
            var oldestTrap = playerTraps[0];
            DeleteShadowSnare(oldestTrap);
            _popup.PopupEntity(Loc.GetString("vampire-shadow-snare-oldest-removed", ("default", "Your old shadow snare has dissipated.")), uid, uid);
        }

        var trap = EntityManager.SpawnEntity("VampireShadowSnareTrap", place);

        var stealth = EnsureComp<StealthComponent>(trap);
        _stealth.SetVisibility(trap, args.StealthVisibility, stealth);
        playerTraps.Add(trap);
        args.Handled = true;
    }
    private void DeleteShadowSnare(EntityUid trap)
    {
        foreach (var playerTraps in _playerShadowSnares.Values)
            playerTraps.Remove(trap);

        if (Exists(trap))
            QueueDel(trap);
    }
    private void OnShadowSnareEnsnareShutdown(EntityUid uid, ShadowSnareEnsnareComponent comp, ComponentShutdown args)
    {
        if (comp.Victim != default && HasComp<ShadowSnareBlindMarkerComponent>(comp.Victim))
        {
            if (HasComp<TemporaryBlindnessComponent>(comp.Victim))
                RemCompDeferred<TemporaryBlindnessComponent>(comp.Victim);

            RemCompDeferred<ShadowSnareBlindMarkerComponent>(comp.Victim);
        }
    }

    private void OnShadowSnareTrapInteractUsing(EntityUid uid, ShadowSnareTrapComponent comp, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        var used = args.Used;
        if (used == EntityUid.Invalid || !HasComp<FlashComponent>(used))
            return;

        if (TryComp<LimitedChargesComponent>(used, out var charges) && !_charges.IsEmpty((used, charges)))
            _charges.TryUseCharge((used, charges));
        args.Handled = true;
        DeleteShadowSnare(uid);
        var user = args.User;
        if (user != EntityUid.Invalid)
            _popup.PopupEntity(Loc.GetString("action-vampire-shadow-snare-scatter"), user, user);
    }

    private void OnShadowSnareTrapCollide(EntityUid uid, ShadowSnareTrapComponent comp, ref StartCollideEvent args)
    {
        if (!Exists(uid))
            return;

        var ent = args.OtherEntity;
        if (ent == uid
            || HasComp<VampireComponent>(ent)
            || !HasComp<HumanoidAppearanceComponent>(ent)
            || !TryComp<DamageableComponent>(ent, out var _))
            return;

        if (comp.TriggerSound != null)
            _audio.PlayPvs(comp.TriggerSound, ent);

        ApplyDamage(ent, _bruteGroupId, comp.TriggerBruteDamage);

        var hadBlind = HasComp<TemporaryBlindnessComponent>(ent);
        if (!hadBlind)
            AddComp<TemporaryBlindnessComponent>(ent);

        if (!HasComp<ShadowSnareBlindMarkerComponent>(ent))
            AddComp<ShadowSnareBlindMarkerComponent>(ent);

        Timer.Spawn(_shadowSnareBlindDuration, () => TryClearShadowSnareBlind(ent));

        var ensnareEnt = EntityManager.SpawnEntity(null, Transform(ent).Coordinates);
        _metaData.SetEntityName(ensnareEnt, Loc.GetString("ent-shadow-snare-ensnare"));
        var ensnaringComponent = EnsureComp<EnsnaringComponent>(ensnareEnt);
        var marker = EnsureComp<ShadowSnareEnsnareComponent>(ensnareEnt);
        marker.Victim = ent;

        ensnaringComponent.BreakoutTime = comp.BreakoutTime;
        ensnaringComponent.FreeTime = comp.FreeTime;
        ensnaringComponent.WalkSpeed = comp.WalkSpeedMultiplier;
        ensnaringComponent.SprintSpeed = comp.SprintSpeedMultiplier;
        ensnaringComponent.MaxEnsnares = comp.MaxEnsnares;
        ensnaringComponent.CanMoveBreakout = comp.CanMoveBreakout;

        EnsureComp<EnsnareableComponent>(ent);

        if (_ensnare.TryEnsnare(ent, ensnareEnt, ensnaringComponent))
            Timer.Spawn(TimeSpan.FromSeconds(0.25f), () => ShadowSnareBlindPoll(ent));
        else
            QueueDel(ensnareEnt);

        DeleteShadowSnare(uid);
    }

    private void TryClearShadowSnareBlind(EntityUid victim)
    {
        if (!Exists(victim)
            || !HasComp<ShadowSnareBlindMarkerComponent>(victim)
            || (TryComp<EnsnareableComponent>(victim, out var ens) && ens.IsEnsnared))
            return;

        if (HasComp<TemporaryBlindnessComponent>(victim))
            RemCompDeferred<TemporaryBlindnessComponent>(victim);

        RemCompDeferred<ShadowSnareBlindMarkerComponent>(victim);
    }

    private void ShadowSnareBlindPoll(EntityUid victim)
    {
        if (!Exists(victim)
            || !HasComp<ShadowSnareBlindMarkerComponent>(victim))
            return;
        if (TryComp<EnsnareableComponent>(victim, out var ens) && ens.IsEnsnared)
        {
            Timer.Spawn(TimeSpan.FromSeconds(0.25f), () => ShadowSnareBlindPoll(victim));
            return;
        }
        TryClearShadowSnareBlind(victim);
    }
#endregion

#region Dark Passage
    private void OnDarkPassage(EntityUid uid, VampireComponent comp, ref VampireDarkPassageActionEvent args)
    {
        if (args.Handled
            || !ValidateVampireClass(uid, comp, VampireClassType.Umbrae)
            )
            return;

        var target = args.Target;
        var curXform = Transform(uid);
        if (curXform.MapID != _transform.GetMapId(target)
            || !_transform.GetGrid(target).HasValue)
            return;

        if (!IsValidTile(target) || !_interaction.InRangeUnobstructed(uid, target, range: 100, collisionMask: CollisionGroup.Opaque, popup: false))
        {
            _popup.PopupEntity(Loc.GetString("action-vampire-dark-passage-wrong-place"), uid, uid);
            return;
        }

        if (!CheckAndConsumeBloodCost(uid, comp, args.Action.Owner))
            return;

        EntityManager.SpawnEntity(args.MistInPrototype, curXform.Coordinates);

        _transform.SetCoordinates(uid, target);
        _transform.AttachToGridOrMap(uid, curXform);

        EntityManager.SpawnEntity(args.MistOutPrototype, target);

        _popup.PopupEntity(Loc.GetString("action-vampire-dark-passage-activated"), uid, uid);
        _audio.PlayPvs(args.Sound, uid, AudioParams.Default.WithVolume(-1f));
        args.Handled = true;
    }
#endregion

#region Extinguish
    private void OnExtinguish(EntityUid uid, VampireComponent comp, ref VampireExtinguishActionEvent args)
    {
        if (args.Handled
            || !comp.ActionEntities.TryGetValue("ActionVampireExtinguish", out var actionEntity)
            || !ValidateVampireClass(uid, comp, VampireClassType.Umbrae)
            || !CheckAndConsumeBloodCost(uid, comp, actionEntity))
            return;

        var center = Transform(uid).Coordinates;

        var toProcess = _lookup.GetEntitiesInRange(center, args.Radius);
        var count = 0;
        foreach (var ent in toProcess)
        {
            if (ent == uid)
                continue;

            if (TryComp<PoweredLightComponent>(ent, out var light))
            {
                _poweredLightSystem.SetState(ent, false, light);
                count++;
            }
        }

        _popup.PopupEntity(Loc.GetString("action-vampire-extinguish-activated", ("count", count)), uid, uid);
        args.Handled = true;
    }
#endregion

#region Eternal Darkness
    private void OnEternalDarkness(EntityUid uid, VampireComponent comp, ref VampireEternalDarknessActionEvent args)
    {
        if (args.Handled
            || !TryComp<UmbraeComponent>(uid, out var umbrae)
            || !ValidateVampireClass(uid, comp, VampireClassType.Umbrae))
            return;

        if (!umbrae.EternalDarknessActive)
        {
            if (!comp.FullPower)
            {
                _popup.PopupEntity(Loc.GetString("action-vampire-not-enough-power"), uid, uid);
                args.Handled = true;
                return;
            }
            umbrae.EternalDarknessActive = true;
        }
        else
            umbrae.EternalDarknessActive = false;
        Dirty(uid, umbrae);

        if (_actions.GetAction(args.Action.Owner) is { } action)
            _actions.SetToggled(action.AsNullable(), umbrae.EternalDarknessActive);

        if (umbrae.EternalDarknessActive)
        {
            _popup.PopupEntity(Loc.GetString("action-vampire-eternal-darkness-start"), uid, uid);
            umbrae.EternalDarknessLoopId++;
            if (umbrae.EternalDarknessAuraEntity == null || !Exists(umbrae.EternalDarknessAuraEntity))
            {
                var aura = Spawn(args.AuraPrototype, Transform(uid).Coordinates);
                umbrae.EternalDarknessAuraEntity = aura;
                _transform.SetParent(aura, uid);
            }
            StartEternalDarknessLoop(uid, args.MaxTicks, 0, args.BloodPerTick, args.TempDropInterval, args.FreezeRadius, args.TargetFreezeTemp, args.TempDropPerInterval, args.LightOffRadius);
        }
        else
        {
            _popup.PopupEntity(Loc.GetString("action-vampire-eternal-darkness-stop"), uid, uid);
            if (umbrae.EternalDarknessAuraEntity != null && Exists(umbrae.EternalDarknessAuraEntity))
                QueueDel(umbrae.EternalDarknessAuraEntity.Value);
            umbrae.EternalDarknessAuraEntity = null;
        }

        args.Handled = true;
    }
    private void StartEternalDarknessLoop(EntityUid uid, int maxTicks, int tick, int bloodPerTick, int dropInterval, float freezeRadius, float targetTemp, float tempDrop, float radius)
    {
        if (tick >= maxTicks
            || !Exists(uid)
            || !TryComp<VampireComponent>(uid, out var comp)
            || !TryComp<UmbraeComponent>(uid, out var umbrae)
            || !umbrae.EternalDarknessActive
            || !ValidateEternalDarknessConditions(uid, comp, umbrae)
            || !ConsumeEternalDarknessBlood(uid, comp, umbrae, bloodPerTick))
            return;

        ProcessEternalDarknessEffects(uid, tick, dropInterval, freezeRadius, targetTemp, tempDrop, radius);
        ScheduleNextEternalDarknessTick(uid, umbrae, maxTicks, tick, bloodPerTick, dropInterval, freezeRadius, targetTemp, tempDrop, radius);
    }

    private bool ValidateEternalDarknessConditions(EntityUid uid, VampireComponent comp, UmbraeComponent umbrae)
    {
        if (TryComp<MobStateComponent>(uid, out var mob) && mob.CurrentState == Shared.Mobs.MobState.Dead)
        {
            DeactivateEternalDarkness(uid, comp, umbrae);
            return false;
        }
        return true;
    }

    private bool ConsumeEternalDarknessBlood(EntityUid uid, VampireComponent comp, UmbraeComponent umbrae, int bloodPerTick)
    {
        if (comp.DrunkBlood < bloodPerTick)
        {
            DeactivateEternalDarkness(uid, comp, umbrae, Loc.GetString("action-vampire-eternal-darkness-not-enough-blood"));
            return false;
        }

        comp.DrunkBlood -= bloodPerTick;
        Dirty(uid, comp);
        UpdateVampireAlert(uid);
        return true;
    }

    private void DeactivateEternalDarkness(EntityUid uid, VampireComponent comp, UmbraeComponent umbrae, string? message = null)
    {
        umbrae.EternalDarknessActive = false;

        if (comp.ActionEntities.TryGetValue("ActionVampireEternalDarkness", out var actionEntity) && _actions.GetAction(actionEntity) is { } action)
            _actions.SetToggled(action.AsNullable(), false);

        if (message != null)
            _popup.PopupEntity(message, uid, uid);

        Dirty(uid, umbrae);
    }

    private void ProcessEternalDarknessEffects(EntityUid uid, int tick, int dropInterval, float freezeRadius, float targetTemp, float tempDrop, float radius)
    {
        var vampXform = Transform(uid);
        var center = _transform.GetWorldPosition(vampXform);

        var doCoolingThisTick = (tick % dropInterval) == 0;
        if (doCoolingThisTick)
            ProcessTemperatureEffects(uid, vampXform, center, freezeRadius, targetTemp, tempDrop);

        ProcessLightEffects(vampXform, radius);
    }

    private void ProcessTemperatureEffects(EntityUid uid, TransformComponent vampXform, Vector2 center, float freezeRadius, float targetTemp, float tempDrop)
    {
        foreach (var ent in _lookup.GetEntitiesInRange(vampXform.Coordinates, freezeRadius))
        {
            if (ent == uid || !HasComp<HumanoidAppearanceComponent>(ent) || HasComp<VampireComponent>(ent))
                continue;

            if (!TryComp<TemperatureComponent>(ent, out var temp))
                continue;

            var targetXform = Transform(ent);
            var distance = (_transform.GetWorldPosition(targetXform) - center).Length();

            if (distance > freezeRadius || temp.CurrentTemperature <= targetTemp)
                continue;

            var remaining = temp.CurrentTemperature - targetTemp;
            var drop = Math.Min(tempDrop, remaining);

            _temperatureSystem.ForceChangeTemperature(ent, temp.CurrentTemperature - drop, temp);
        }
    }

    private void ProcessLightEffects(TransformComponent vampXform, float radius)
    {
        foreach (var ent in _lookup.GetEntitiesInRange(vampXform.Coordinates, radius))
            if (TryComp<PoweredLightComponent>(ent, out var light))
                _poweredLightSystem.SetState(ent, false, light);
    }

    private void ScheduleNextEternalDarknessTick(EntityUid uid, UmbraeComponent umbrae, int maxTicks, int tick, int bloodPerTick, int dropInterval, float freezeRadius, float targetTemp, float tempDrop, float radius)
    {
        var expectedLoopId = umbrae.EternalDarknessLoopId;
        Timer.Spawn(TimeSpan.FromSeconds(1), () =>
        {
            if (!Exists(uid) || !TryComp<UmbraeComponent>(uid, out var c2))
                return;

            if (!c2.EternalDarknessActive || c2.EternalDarknessLoopId != expectedLoopId)
                return;

            StartEternalDarknessLoop(uid, maxTicks, tick + 1, bloodPerTick, dropInterval, freezeRadius, targetTemp, tempDrop, radius);
        });
    }
#endregion

#region Shadow Anchor
    private void OnShadowAnchor(EntityUid uid, VampireComponent comp, ref VampireShadowAnchorActionEvent args)
    {
        if (args.Handled
            || !TryComp<UmbraeComponent>(uid, out var umbrae)
            || !ValidateVampireClass(uid, comp, VampireClassType.Umbrae))
            return;

        // If an anchor already exists, this activation returns to it and deletes it
        if (umbrae.SpawnedShadowAnchorBeacon != null && Exists(umbrae.SpawnedShadowAnchorBeacon))
        {
            ReturnToShadowAnchor(uid, umbrae);
            args.Handled = true;
            return;
        }

        // Prevent starting multiple plasement DoAfters
        if (umbrae.ShadowAnchorPlacementInProgress)
        {
            args.Handled = true;
            return;
        }

        // consume blood it only after the doAfter complete
        if (!TryComp<VampireActionComponent>(args.Action.Owner, out var vac))
            return;

        if (comp.TotalBlood < vac.BloodToUnlock)
            return;

        var bloodCost = (int) vac.BloodCost;
        if (bloodCost > 0 && comp.DrunkBlood < bloodCost)
        {
            _popup.PopupEntity(Loc.GetString("vampire-not-enough-blood"), uid, uid);
            return;
        }

        // Cache prototype to use when the DoAfter finishes
        umbrae.ShadowAnchorBeaconPrototype = args.BeaconPrototype;

        // remember tile where ability was originaly activated
        var pressedCoords = Transform(uid).Coordinates;
        var tileCoords = pressedCoords.WithPosition(pressedCoords.Position.Floored() + new Vector2(0.5f, 0.5f));

        var ev = new VampireShadowAnchorDoAfterEvent(GetNetCoordinates(tileCoords), bloodCost, args.AutoReturnDelay);
        var doAfter = new DoAfterArgs(EntityManager, uid, TimeSpan.FromSeconds(args.PlaceDelay), ev, uid)
        {
            DistanceThreshold = null,
            BreakOnDamage = false,
            BreakOnMove = false,
            RequireCanInteract = false,
            BlockDuplicate = true,
            CancelDuplicate = true
        };

        umbrae.ShadowAnchorPlacementInProgress = true;

        if (!_doAfter.TryStartDoAfter(doAfter))
        {
            umbrae.ShadowAnchorPlacementInProgress = false;
            return;
        }

        args.Handled = true;
    }

    private void OnShadowAnchorDoAfter(EntityUid uid, VampireComponent comp, ref VampireShadowAnchorDoAfterEvent args)
    {
        if (!TryComp<UmbraeComponent>(uid, out var umbrae))
            return;

        // Always clear placement lock when the DoAfter resolves
        umbrae.ShadowAnchorPlacementInProgress = false;

        if (args.Handled || args.Cancelled)
            return;

        if (!ValidateVampireClass(uid, comp, VampireClassType.Umbrae))
            return;

        if (!CheckAndConsumeBloodCost(uid, comp, null, args.BloodCost))
            return;

        // safety check
        if (umbrae.SpawnedShadowAnchorBeacon != null && Exists(umbrae.SpawnedShadowAnchorBeacon))
            return;

        var coords = GetCoordinates(args.TargetCoordinates);
        var newBeacon = EntityManager.SpawnEntity(umbrae.ShadowAnchorBeaconPrototype, coords);
        umbrae.SpawnedShadowAnchorBeacon = newBeacon;
        umbrae.ShadowAnchorLoopId++;
        var expectedLoopId = umbrae.ShadowAnchorLoopId;
        Dirty(uid, umbrae);

        _popup.PopupEntity(Loc.GetString("action-vampire-shadow-anchor-installed"), uid, uid);

        // Auto-return if vampire doesnt use the ability agaib
        Timer.Spawn(TimeSpan.FromSeconds(args.AutoReturnDelay), () => AutoReturnToShadowAnchor(uid, expectedLoopId));
    }

    private void AutoReturnToShadowAnchor(EntityUid uid, int expectedLoopId)
    {
        if (!Exists(uid) || !TryComp<UmbraeComponent>(uid, out var umbrae))
            return;

        if (umbrae.ShadowAnchorLoopId != expectedLoopId)
            return;

        if (umbrae.SpawnedShadowAnchorBeacon == null || !Exists(umbrae.SpawnedShadowAnchorBeacon))
            return;

        ReturnToShadowAnchor(uid, umbrae);
    }

    private void ReturnToShadowAnchor(EntityUid uid, UmbraeComponent umbrae)
    {
        if (umbrae.SpawnedShadowAnchorBeacon == null || !Exists(umbrae.SpawnedShadowAnchorBeacon))
        {
            umbrae.SpawnedShadowAnchorBeacon = null;
            Dirty(uid, umbrae);
            return;
        }

        var beacon = umbrae.SpawnedShadowAnchorBeacon.Value;
        var coords = Transform(beacon).Coordinates;
        _transform.SetCoordinates(uid, coords);
        _transform.AttachToGridOrMap(uid, Transform(uid));

        QueueDel(beacon);
        umbrae.SpawnedShadowAnchorBeacon = null;
        umbrae.ShadowAnchorLoopId++;
        Dirty(uid, umbrae);

        _popup.PopupEntity(Loc.GetString("action-vampire-shadow-anchor-returned"), uid, uid);
    }
#endregion

#region Shadow Boxing
    private void OnShadowBoxing(EntityUid uid, VampireComponent comp, ref VampireShadowBoxingActionEvent args)
    {
        if (args.Handled
            || !comp.ActionEntities.TryGetValue("ActionVampireShadowBoxing", out var actionEntity)
            || !TryComp<UmbraeComponent>(uid, out var umbrae))
            return;

        if (!ValidateVampireClass(uid, comp, VampireClassType.Umbrae))
            return;

        var target = args.Target;
        if (target == uid
            || !Exists(target)
            || !HasComp<HumanoidAppearanceComponent>(target)
            || !TryComp<DamageableComponent>(target, out _))
            return;

        var now = _timing.CurTime;
        var totalDuration = TimeSpan.FromSeconds(10);

        if (!umbrae.ShadowBoxingActive)
        {
            if (!CheckAndConsumeBloodCost(uid, comp, actionEntity))
                return;

            umbrae.ShadowBoxingActive = true;
            umbrae.ShadowBoxingEndTime = now + totalDuration;
            _popup.PopupEntity(Loc.GetString("action-vampire-shadow-boxing-start"), uid, uid);
        }
        else
        {
            if (umbrae.ShadowBoxingEndTime.HasValue && now >= umbrae.ShadowBoxingEndTime.Value)
            {
                umbrae.ShadowBoxingActive = false;
                umbrae.ShadowBoxingTarget = null;
                umbrae.ShadowBoxingEndTime = null;
                Dirty(uid, comp);
                _popup.PopupEntity(Loc.GetString("action-vampire-shadow-boxing-stop"), uid, uid);
                return;
            }
        }

        umbrae.ShadowBoxingTarget = target;
        Dirty(uid, umbrae);

        var arguments = args;

        void TickLoop()
        {
            if (!Exists(uid) || !TryComp<UmbraeComponent>(uid, out var c) || !c.ShadowBoxingActive)
                return;

            var currentNow = _timing.CurTime;
            if (!c.ShadowBoxingEndTime.HasValue || currentNow >= c.ShadowBoxingEndTime.Value)
            {
                c.ShadowBoxingActive = false;
                c.ShadowBoxingTarget = null;
                c.ShadowBoxingEndTime = null;
                c.ShadowBoxingLoopRunning = false;
                Dirty(uid, c);
                _popup.PopupEntity(Loc.GetString("action-vampire-shadow-boxing-ends"), uid, uid);
                return;
            }
            // ehh.. well its something
            var tgt = c.ShadowBoxingTarget;
            if (tgt == null || !Exists(tgt.Value))
            {
                Timer.Spawn(TimeSpan.FromSeconds(arguments.Interval), TickLoop);
                return;
            }

            if (!TryComp<DamageableComponent>(tgt.Value, out _))
            {
                Timer.Spawn(TimeSpan.FromSeconds(arguments.Interval), TickLoop);
                return;
            }
            if (TryComp<MobStateComponent>(tgt.Value, out var mob) && mob.CurrentState == Shared.Mobs.MobState.Dead)
            {
                Timer.Spawn(TimeSpan.FromSeconds(arguments.Interval), TickLoop);
                return;
            }

            var curDist = (_transform.GetWorldPosition(Transform(uid)) - _transform.GetWorldPosition(Transform(tgt.Value))).Length();
            if (curDist <= arguments.Range)
            {
                ApplyDamage(tgt.Value, "Blunt", arguments.BrutePerTick, uid);
                if (arguments.HitSound != null)
                    _audio.PlayPvs(arguments.HitSound, tgt.Value);
                var punchEffect = Spawn("WeaponArcPunch", Transform(tgt.Value).Coordinates);
                _transform.SetParent(punchEffect, tgt.Value);
                RaiseNetworkEvent(new VampireShadowBoxingPunchEvent(GetNetEntity(uid), GetNetEntity(tgt.Value)));
            }

            Timer.Spawn(TimeSpan.FromSeconds(arguments.Interval), TickLoop);
        }

        if (!umbrae.ShadowBoxingLoopRunning)
        {
            umbrae.ShadowBoxingLoopRunning = true;
            Timer.Spawn(TimeSpan.Zero, () =>
            {
                void WrappedTick()
                {
                    if (!Exists(uid) || !TryComp<UmbraeComponent>(uid, out var c) || !c.ShadowBoxingActive)
                    {
                        if (TryComp<UmbraeComponent>(uid, out var c2))
                            c2.ShadowBoxingLoopRunning = false;
                        return;
                    }
                    TickLoop();
                }
                WrappedTick();
            });
        }
        args.Handled = true;
    }
}
#endregion