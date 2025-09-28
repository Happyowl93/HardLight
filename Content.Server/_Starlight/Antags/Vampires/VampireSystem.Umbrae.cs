using Content.Shared.Charges.Components;
using Content.Shared.Ensnaring.Components;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Flash.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Stealth.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Timing;
using Content.Shared._Starlight.Antags.Vampires;
using Content.Shared.Damage;
using Content.Shared.Humanoid;
using Content.Shared.Interaction;
using System.Numerics;
using Content.Shared.Physics;

namespace Content.Server._Starlight.Antags.Vampires;

public sealed partial class VampireSystem : EntitySystem
{
    private void InitializeUmbrae()
    {
        SubscribeLocalEvent<VampireComponent, VampireCloakOfDarknessActionEvent>(OnCloackOfDarkness);

        SubscribeLocalEvent<ShadowSnareEnsnareComponent, ComponentShutdown>(OnShadowSnareEnsnareShutdown);

        SubscribeLocalEvent<VampireComponent, VampireShadowSnareActionEvent>(OnShadowSnare);
        SubscribeLocalEvent<VampireComponent, VampireDarkPassageActionEvent>(OnDarkPassage);
        SubscribeLocalEvent<VampireComponent, VampireExtinguishActionEvent>(OnExtinguish);
        SubscribeLocalEvent<VampireComponent, VampireEternalDarknessActionEvent>(OnEternalDarkness);
        SubscribeLocalEvent<VampireComponent, VampireShadowAnchorActionEvent>(OnShadowAnchor);
        SubscribeLocalEvent<VampireComponent, VampireShadowBoxingActionEvent>(OnShadowBoxing);
        SubscribeLocalEvent<ShadowSnareTrapComponent, InteractUsingEvent>(OnShadowSnareTrapInteractUsing);
        SubscribeLocalEvent<ShadowSnareTrapComponent, StartCollideEvent>(OnShadowSnareTrapCollide);
    }

    private void OnCloackOfDarkness(EntityUid uid, VampireComponent comp, ref VampireCloakOfDarknessActionEvent args)
    {
        if (args.Handled)
            return;

        if (!ValidateVampireClass(uid, comp, VampireClassType.Umbrae))
            return;

        if (comp.CloakOfDarknessActive)
        {
            DeactivateCloakOfDarkness(uid, comp);
            _popup.PopupEntity("You step out of the shadows", uid, uid); // Rinary - move to locale
        }
        else
        {
            ActivateCloakOfDarkness(uid, comp);
            _popup.PopupEntity("You blend into the shadows!", uid, uid); // Rinary - move to locale
        }

        if (_actions.GetAction(comp.Actions.VampireCloakOfDarknessActionEntity) is { } action)
            _actions.SetToggled(action.AsNullable(), comp.CloakOfDarknessActive);

        args.Handled = true;
    }

    private void ActivateCloakOfDarkness(EntityUid uid, VampireComponent comp)
    {
        comp.CloakOfDarknessActive = true;
        comp.CloakOfDarknessLoopId++;
        Dirty(uid, comp);

        StartCloakOfDarknessLoop(uid, 0);
    }

    private void DeactivateCloakOfDarkness(EntityUid uid, VampireComponent comp)
    {
        comp.CloakOfDarknessActive = false;
        Dirty(uid, comp);

        RemComp<StealthComponent>(uid);
        _movementMod.TryUpdateMovementSpeedModDuration(uid, "VampireCloakSpeedBoost", TimeSpan.Zero, 1f); // Rinary - move to resources
    }

    private void StartCloakOfDarknessLoop(EntityUid uid, int tickCount)
    {
        const int MaxTicks = 3000; // +-100 minutes // Rinary - move to resources

        if (tickCount >= MaxTicks || !Exists(uid))
            return;

        if (!TryComp<VampireComponent>(uid, out var comp) || !comp.CloakOfDarknessActive)
            return;

        if (TryComp<MobStateComponent>(uid, out var mobState) &&
            mobState.CurrentState == Shared.Mobs.MobState.Dead)
        {
            DeactivateCloakOfDarkness(uid, comp);
            if (_actions.GetAction(comp.Actions.VampireCloakOfDarknessActionEntity) is { } action)
                _actions.SetToggled(action.AsNullable(), false);
            return;
        }

        var lightLevel = _shadekin.GetLightExposure(uid);

        ApplyCloakEffects(uid, comp, lightLevel);

        var expectedLoopId = comp.CloakOfDarknessLoopId;
        Timer.Spawn(TimeSpan.FromSeconds(2f), () =>
        {
            if (!Exists(uid) || !TryComp<VampireComponent>(uid, out var c2)) return;
            if (!c2.CloakOfDarknessActive || c2.CloakOfDarknessLoopId != expectedLoopId) return;
            StartCloakOfDarknessLoop(uid, tickCount + 1);
        });
    }

    private void ApplyCloakEffects(EntityUid uid, VampireComponent comp, float lightLevel)
    {

        float stealthModifier;
        float speedModifier;

        switch (lightLevel)
        {
            case <= 1f:
                stealthModifier = -1f;
                speedModifier = 1.4f;
                break;
            case <= 5f:
                stealthModifier = -0.7f;
                speedModifier = 1.3f;
                break;
            case <= 10f:
                stealthModifier = 0.4f;
                speedModifier = 1.2f;
                break;
            default:
                stealthModifier = 0.2f;
                speedModifier = 1.1f;
                break;
        }

        var stealth = EnsureComp<StealthComponent>(uid);
        _stealth.SetVisibility(uid, stealthModifier, stealth);

        _movementMod.TryUpdateMovementSpeedModDuration(uid, "VampireCloakSpeedBoost", TimeSpan.MaxValue, speedModifier);
    }

    private void OnShadowSnare(EntityUid uid, VampireComponent comp, ref VampireShadowSnareActionEvent args)
    {
        if (args.Handled)
            return;

        if (!ValidateVampireClass(uid, comp, VampireClassType.Umbrae))
            return;

        if (!CheckAndConsumeBloodCost(uid, comp, comp.Actions.ShadowSnareActionEntity))
            return;

        if (!_playerShadowSnares.TryGetValue(uid, out var playerTraps))
        {
            playerTraps = new List<EntityUid>();
            _playerShadowSnares[uid] = playerTraps;
        }

        playerTraps.RemoveAll(trap => !Exists(trap) || !_shadowSnares.ContainsKey(trap));

        if (playerTraps.Count >= args.MaxPerPlayer)
        {
            var oldestTrap = playerTraps[0];
            DeleteShadowSnare(oldestTrap);
            _popup.PopupEntity(Loc.GetString("vampire-shadow-snare-oldest-removed", ("default", "Your old shadow snare has dissipated.")), uid, uid);
        }

        var target = args.Target;
        var place = target.WithPosition(target.Position.Floored() + new Vector2(args.PositionOffset, args.PositionOffset));
        if (!_transform.GetGrid(place).HasValue)
        {
            return;
        }

        var trap = EntityManager.SpawnEntity("VampireShadowSnareTrap", place);
        EnsureComp<ShadowSnareTrapComponent>(trap);

        var stealth = EnsureComp<StealthComponent>(trap);
        _stealth.SetVisibility(trap, args.StealthVisibility, stealth);

        _shadowSnares[trap] = new ShadowSnareData(trap, args.BaseHealth);
        playerTraps.Add(trap);
        StartShadowSnareLoop(trap, args.TickInterval, 0, args.DamageDark, args.DamageNormal, args.DamageBright);
        args.Handled = true;
    }

    private void OnDarkPassage(EntityUid uid, VampireComponent comp, ref VampireDarkPassageActionEvent args)
    {
        if (args.Handled)
            return;

        if (!ValidateVampireClass(uid, comp, VampireClassType.Umbrae))
            return;

        if (!CheckAndConsumeBloodCost(uid, comp, comp.Actions.DarkPassageActionEntity))
            return;

        var target = args.Target;
        var curXform = Transform(uid);
        if (curXform.MapID != _transform.GetMapId(target))
            return;

        if (!_transform.GetGrid(target).HasValue)
            return;

        if (!IsValidTile(target) || !_interaction.InRangeUnobstructed(uid, target, range: 1000f, collisionMask: CollisionGroup.Opaque, popup: false))
        {
            _popup.PopupEntity("The darkness here is impenetrable...", uid, uid);
            return;
        }

        EntityManager.SpawnEntity("VampireDarkPassageMistIn", curXform.Coordinates);

        _transform.SetCoordinates(uid, target);
        _transform.AttachToGridOrMap(uid, curXform);

        EntityManager.SpawnEntity("VampireDarkPassageMistOut", target);

        _popup.PopupEntity("You slipped through the darkness...", uid, uid);
        args.Handled = true;
    }

    private void OnExtinguish(EntityUid uid, VampireComponent comp, ref VampireExtinguishActionEvent args)
    {
        if (args.Handled)
            return;

        if (!ValidateVampireClass(uid, comp, VampireClassType.Umbrae))
            return;

        if (!CheckAndConsumeBloodCost(uid, comp, comp.Actions.ExtinguishActionEntity))
            return;

        var center = Transform(uid).Coordinates;

        var toProcess = _lookup.GetEntitiesInRange(center, args.Radius);
        var count = 0;
        foreach (var ent in toProcess)
        {
            if (ent == uid)
                continue;

            if (TryComp<Shared.Light.Components.PoweredLightComponent>(ent, out var light))
            {
                EntityManager.System<Light.EntitySystems.PoweredLightSystem>().SetState(ent, false, light);
                count++;
            }
        }

        _popup.PopupEntity($"You absorbed the light around you...({count})", uid, uid);
        args.Handled = true;
    }

    private void OnEternalDarkness(EntityUid uid, VampireComponent comp, ref VampireEternalDarknessActionEvent args)
    {
        if (args.Handled)
            return;

        if (!ValidateVampireClass(uid, comp, VampireClassType.Umbrae))
            return;

        if (!comp.EternalDarknessActive)
        {
            if (!comp.FullPower)
            {
                _popup.PopupEntity("Your power is insufficient (need >1000 total blood & 8 unique victims).", uid, uid);
                args.Handled = true;
                return;
            }
            comp.EternalDarknessActive = true;
        }
        else
        {
            comp.EternalDarknessActive = false;
        }
        Dirty(uid, comp);

        if (_actions.GetAction(comp.Actions.EternalDarknessActionEntity) is { } action)
            _actions.SetToggled(action.AsNullable(), comp.EternalDarknessActive);

        if (comp.EternalDarknessActive)
        {
            _popup.PopupEntity("You conjured eternal darkness...", uid, uid);
            comp.EternalDarknessLoopId++;
            if (comp.EternalDarknessAuraEntity == null || !Exists(comp.EternalDarknessAuraEntity))
            {
                var aura = Spawn("VampireEternalDarknessAura", Transform(uid).Coordinates);
                comp.EternalDarknessAuraEntity = aura;
                var xformSys = EntityManager.System<SharedTransformSystem>();
                xformSys.SetParent(aura, uid);
            }
            StartEternalDarknessLoop(uid, args.MaxTicks, 0, args.BloodPerTick, args.TempDropInterval, args.FreezeRadius, args.TargetFreezeTemp, args.TempDropPerInterval, args.LightOffRadius);
        }
        else
        {
            _popup.PopupEntity("The eternal darkness has dissipated...", uid, uid);
            if (comp.EternalDarknessAuraEntity != null && Exists(comp.EternalDarknessAuraEntity))
            {
                QueueDel(comp.EternalDarknessAuraEntity.Value);
            }
            comp.EternalDarknessAuraEntity = null;
        }

        args.Handled = true;
    }
    private void StartEternalDarknessLoop(EntityUid uid, int maxTicks, int tick, int bloodPerTick, int dropInterval, float freezeRadius, float targetTemp, float tempDrop, float radius)
    {
        if (tick >= maxTicks || !Exists(uid))
            return;

        if (!TryComp<VampireComponent>(uid, out var comp) || !comp.EternalDarknessActive)
            return;

        if (!ValidateEternalDarknessConditions(uid, comp))
            return;

        if (!ConsumeEternalDarknessBlood(uid, comp, bloodPerTick))
            return;

        ProcessEternalDarknessEffects(uid, comp, tick, dropInterval, freezeRadius, targetTemp, tempDrop, radius);
        ScheduleNextEternalDarknessTick(uid, comp, maxTicks, tick, bloodPerTick, dropInterval, freezeRadius, targetTemp, tempDrop, radius);
    }

    private bool ValidateEternalDarknessConditions(EntityUid uid, VampireComponent comp)
    {
        if (TryComp<MobStateComponent>(uid, out var mob) && mob.CurrentState == Shared.Mobs.MobState.Dead)
        {
            DeactivateEternalDarkness(uid, comp);
            return false;
        }
        return true;
    }

    private bool ConsumeEternalDarknessBlood(EntityUid uid, VampireComponent comp, int bloodPerTick)
    {
        if (comp.DrunkBlood < bloodPerTick)
        {
            DeactivateEternalDarkness(uid, comp, "You have run out of blood to sustain eternal darkness.");
            return false;
        }

        comp.DrunkBlood -= bloodPerTick;
        Dirty(uid, comp);
        UpdateVampireAlert(uid);
        return true;
    }

    private void DeactivateEternalDarkness(EntityUid uid, VampireComponent comp, string? message = null)
    {
        comp.EternalDarknessActive = false;

        if (_actions.GetAction(comp.Actions.EternalDarknessActionEntity) is { } action)
            _actions.SetToggled(action.AsNullable(), false);

        if (message != null)
            _popup.PopupEntity(message, uid, uid);

        Dirty(uid, comp);
    }

    private void ProcessEternalDarknessEffects(EntityUid uid, VampireComponent comp, int tick, int dropInterval, float freezeRadius, float targetTemp, float tempDrop, float radius)
    {
        var vampXform = Transform(uid);
        var xformSys = EntityManager.System<SharedTransformSystem>();
        var center = xformSys.GetWorldPosition(vampXform);

        var doCoolingThisTick = (tick % dropInterval) == 0;
        if (doCoolingThisTick)
        {
            ProcessTemperatureEffects(uid, vampXform, center, xformSys, freezeRadius, targetTemp, tempDrop);
        }

        ProcessLightEffects(vampXform, radius);
    }

    private void ProcessTemperatureEffects(EntityUid uid, TransformComponent vampXform, Vector2 center, SharedTransformSystem xformSys, float freezeRadius, float targetTemp, float tempDrop)
    {
        foreach (var ent in _lookup.GetEntitiesInRange(vampXform.Coordinates, freezeRadius))
        {
            if (ent == uid || !HasComp<HumanoidAppearanceComponent>(ent) || HasComp<VampireComponent>(ent))
                continue;

            if (!TryComp<Temperature.Components.TemperatureComponent>(ent, out var temp))
                continue;

            var targetXform = Transform(ent);
            var distance = (xformSys.GetWorldPosition(targetXform) - center).Length();

            if (distance > freezeRadius || temp.CurrentTemperature <= targetTemp)
                continue;

            var remaining = temp.CurrentTemperature - targetTemp;
            var drop = Math.Min(tempDrop, remaining);

            var tempSys = EntityManager.System<Temperature.Systems.TemperatureSystem>();
            tempSys.ForceChangeTemperature(ent, temp.CurrentTemperature - drop, temp);
        }
    }

    private void ProcessLightEffects(TransformComponent vampXform, float radius)
    {
        foreach (var ent in _lookup.GetEntitiesInRange(vampXform.Coordinates, radius))
        {
            if (TryComp<Shared.Light.Components.PoweredLightComponent>(ent, out var light))
            {
                EntityManager.System<Light.EntitySystems.PoweredLightSystem>().SetState(ent, false, light);
            }
        }
    }

    private void ScheduleNextEternalDarknessTick(EntityUid uid, VampireComponent comp, int maxTicks, int tick, int bloodPerTick, int dropInterval, float freezeRadius, float targetTemp, float tempDrop, float radius)
    {
        var expectedLoopId = comp.EternalDarknessLoopId;
        Timer.Spawn(TimeSpan.FromSeconds(1), () =>
        {
            if (!Exists(uid) || !TryComp<VampireComponent>(uid, out var c2))
                return;

            if (!c2.EternalDarknessActive || c2.EternalDarknessLoopId != expectedLoopId)
                return;

            StartEternalDarknessLoop(uid, maxTicks, tick + 1, bloodPerTick, dropInterval, freezeRadius, targetTemp, tempDrop, radius);
        });
    }

    private void StartShadowSnareLoop(EntityUid trap, float interval, int tick, float damageDark, float damageNormal, float damageBright)
    {
        if (!Exists(trap) || !_shadowSnares.ContainsKey(trap))
            return;

        if (!_shadowSnares.TryGetValue(trap, out var data))
            return;

        var light = _shadekin.GetLightExposure(trap);
        float decay = light switch
        {
            <= 5f => damageDark,
            <= 10f => damageNormal,
            _ => damageBright
        };

        var newHealth = data.Health - (int)decay;
        if (newHealth <= 0)
        {
            DeleteShadowSnare(trap);
            return;
        }
        _shadowSnares[trap] = data with { Health = newHealth };

        Timer.Spawn(TimeSpan.FromSeconds(interval), () => StartShadowSnareLoop(trap, interval, tick + 1, damageDark, damageNormal, damageBright));
    }

    private void DeleteShadowSnare(EntityUid trap)
    {
        if (_shadowSnares.Remove(trap))
        {
            foreach (var playerTraps in _playerShadowSnares.Values)
            {
                playerTraps.Remove(trap);
            }

            if (Exists(trap))
                QueueDel(trap);
        }
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

        if (TryComp<LimitedChargesComponent>(used, out var charges))
        {
            if (!_charges.IsEmpty((used, charges)))
            {
                _charges.TryUseCharge((used, charges));
            }
        }
        args.Handled = true;
        DeleteShadowSnare(uid);
        var user = args.User;
        if (user != EntityUid.Invalid)
            _popup.PopupEntity("You scattered shadow trap", user, user);
    }

    private void OnShadowSnareTrapCollide(EntityUid uid, ShadowSnareTrapComponent comp, ref StartCollideEvent args)
    {
        if (!Exists(uid) || !_shadowSnares.ContainsKey(uid))
            return;

        var ent = args.OtherEntity;
        if (ent == uid)
            return;
        if (HasComp<VampireComponent>(ent))
            return;
        if (!HasComp<HumanoidAppearanceComponent>(ent))
            return;
        if (!TryComp<DamageableComponent>(ent, out var _))
            return;

        ApplyDamage(ent, _bruteGroupId, comp.ShadowSnareTriggerBrute);

        var hadBlind = HasComp<TemporaryBlindnessComponent>(ent);
        if (!hadBlind)
            AddComp<TemporaryBlindnessComponent>(ent);

        if (!HasComp<ShadowSnareBlindMarkerComponent>(ent))
            AddComp<ShadowSnareBlindMarkerComponent>(ent);

        Timer.Spawn(_shadowSnareBlindDuration, () => TryClearShadowSnareBlind(ent));

        var ensnareEnt = EntityManager.SpawnEntity(null, Transform(ent).Coordinates);
        var ensnaringComponent = EnsureComp<EnsnaringComponent>(ensnareEnt);
        var marker = EnsureComp<ShadowSnareEnsnareComponent>(ensnareEnt);
        marker.Victim = ent;

        ensnaringComponent.BreakoutTime = 5f;
        ensnaringComponent.FreeTime = 3.5f;
        ensnaringComponent.WalkSpeed = comp.ShadowSnareSlowMultiplier;
        ensnaringComponent.SprintSpeed = comp.ShadowSnareSlowMultiplier;
        ensnaringComponent.MaxEnsnares = 1;
        ensnaringComponent.CanMoveBreakout = true;

        EnsureComp<EnsnareableComponent>(ent);

        if (_ensnare.TryEnsnare(ent, ensnareEnt, ensnaringComponent))
        {
            Timer.Spawn(TimeSpan.FromSeconds(0.25f), () => ShadowSnareBlindPoll(ent));
        }
        else
        {
            QueueDel(ensnareEnt);
        }

        DeleteShadowSnare(uid);
    }

    private void TryClearShadowSnareBlind(EntityUid victim)
    {
        if (!Exists(victim))
            return;

        if (!HasComp<ShadowSnareBlindMarkerComponent>(victim))
            return;

        if (TryComp<EnsnareableComponent>(victim, out var ens) && ens.IsEnsnared)
            return;

        if (HasComp<TemporaryBlindnessComponent>(victim))
            RemCompDeferred<TemporaryBlindnessComponent>(victim);

        RemCompDeferred<ShadowSnareBlindMarkerComponent>(victim);
    }

    private void ShadowSnareBlindPoll(EntityUid victim)
    {
        if (!Exists(victim))
            return;
        if (!HasComp<ShadowSnareBlindMarkerComponent>(victim))
            return;
        if (TryComp<EnsnareableComponent>(victim, out var ens) && ens.IsEnsnared)
        {
            Timer.Spawn(TimeSpan.FromSeconds(0.25f), () => ShadowSnareBlindPoll(victim));
            return;
        }
        TryClearShadowSnareBlind(victim);
    }

    private void OnShadowAnchor(EntityUid uid, VampireComponent comp, ref VampireShadowAnchorActionEvent args)
    {
        if (args.Handled)
            return;

        if (!ValidateVampireClass(uid, comp, VampireClassType.Umbrae))
            return;

        if (!CheckAndConsumeBloodCost(uid, comp, comp.Actions.ShadowAnchorActionEntity))
            return;

        if (comp.SpawnedShadowAnchorBeacon != null && Exists(comp.SpawnedShadowAnchorBeacon))
        {
            var beacon = comp.SpawnedShadowAnchorBeacon.Value;
            var coords = Transform(beacon).Coordinates;
            _transform.SetCoordinates(uid, coords);
            _transform.AttachToGridOrMap(uid, Transform(uid));
            QueueDel(beacon);
            comp.SpawnedShadowAnchorBeacon = null;
            _popup.PopupEntity("You returned to the shadow anchor", uid, uid);
            args.Handled = true;
            return;
        }

        var cur = Transform(uid).Coordinates;
        var newBeacon = EntityManager.SpawnEntity("VampireShadowAnchorBeacon", cur);
        comp.SpawnedShadowAnchorBeacon = newBeacon;
        _popup.PopupEntity("You've secured a spot in the shadows", uid, uid);
        args.Handled = true;
    }

    private void OnShadowBoxing(EntityUid uid, VampireComponent comp, ref VampireShadowBoxingActionEvent args)
    {
        if (args.Handled)
            return;

        if (!ValidateVampireClass(uid, comp, VampireClassType.Umbrae))
            return;

        var target = args.Target;
        if (target == EntityUid.Invalid || target == uid)
            return;
        if (!Exists(target) || !HasComp<HumanoidAppearanceComponent>(target))
            return;
        if (!TryComp<DamageableComponent>(target, out _))
            return;

        var now = _timing.CurTime;
        var totalDuration = TimeSpan.FromSeconds(10);

        if (!comp.ShadowBoxingActive)
        {
            if (!CheckAndConsumeBloodCost(uid, comp, comp.Actions.ShadowBoxingActionEntity))
                return;

            comp.ShadowBoxingActive = true;
            comp.ShadowBoxingEndTime = now + totalDuration;
            _popup.PopupEntity("You begin shadow boxing", uid, uid);
        }
        else
        {
            if (comp.ShadowBoxingEndTime.HasValue && now >= comp.ShadowBoxingEndTime.Value)
            {
                comp.ShadowBoxingActive = false;
                comp.ShadowBoxingTarget = null;
                comp.ShadowBoxingEndTime = null;
                Dirty(uid, comp);
                _popup.PopupEntity("Shadow boxing has been stoped", uid, uid);
                return;
            }
        }

        comp.ShadowBoxingTarget = target;
        Dirty(uid, comp);

        var arguments = args;

        void TickLoop()
        {
            if (!Exists(uid) || !TryComp<VampireComponent>(uid, out var c) || !c.ShadowBoxingActive)
                return;

            var currentNow = _timing.CurTime;
            if (!c.ShadowBoxingEndTime.HasValue || currentNow >= c.ShadowBoxingEndTime.Value)
            {
                c.ShadowBoxingActive = false;
                c.ShadowBoxingTarget = null;
                c.ShadowBoxingEndTime = null;
                c.ShadowBoxingLoopRunning = false;
                Dirty(uid, c);
                _popup.PopupEntity("Shadow boxing ends.", uid, uid);
                return;
            }

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

            var xformSys = EntityManager.System<SharedTransformSystem>();
            var curDist = (xformSys.GetWorldPosition(Transform(uid)) - xformSys.GetWorldPosition(Transform(tgt.Value))).Length();
            if (curDist <= arguments.Range)
            {
                ApplyDamage(tgt.Value, "Blunt", arguments.BrutePerTick, uid);
                RaiseNetworkEvent(new VampireShadowBoxingPunchEvent(GetNetEntity(uid), GetNetEntity(tgt.Value)));
            }

            Timer.Spawn(TimeSpan.FromSeconds(arguments.Interval), TickLoop);
        }

        if (!comp.ShadowBoxingLoopRunning)
        {
            comp.ShadowBoxingLoopRunning = true;
            Timer.Spawn(TimeSpan.Zero, () =>
            {
                void WrappedTick()
                {
                    if (!Exists(uid) || !TryComp<VampireComponent>(uid, out var c) || !c.ShadowBoxingActive)
                    {
                        if (TryComp<VampireComponent>(uid, out var c2))
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