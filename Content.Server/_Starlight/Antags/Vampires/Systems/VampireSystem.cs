using Content.Server.Actions;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Body.Systems;
using Content.Server.Objectives.Components;
using Content.Server.Objectives;
using Content.Server.Objectives.Systems;
using Content.Server.Polymorph.Systems;
using Content.Shared._Starlight.Antags.Vampires;
using Content.Shared._Starlight.Antags.Vampires.Components;
using Content.Shared._Starlight.Antags.Vampires.Components.Classes;
using Content.Shared._Starlight.Antags.Vampires.Prototypes;
using Content.Shared.Alert;
using Content.Shared.Charges.Systems;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Flash;
using Content.Shared.Ensnaring;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.FixedPoint;
using Content.Shared.Maps;
using Content.Shared.Mind;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Examine;
using Content.Shared.Movement.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Objectives.Components;
using Content.Shared.Popups;
using Content.Shared.StatusEffectNew;
using Content.Shared.Stealth;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Content.Shared.Wieldable;
using Content.Shared.Prayer;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Content.Server.Destructible;
using Content.Shared.CollectiveMind;
using Content.Server._Starlight.Antags.Vampires.Systems;

namespace Content.Server._Starlight.Antags.Vampires.Systems;

public sealed partial class VampireSystem : EntitySystem
{
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly BloodstreamSystem _blood = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly SharedStaminaSystem _stamina = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly ExamineSystemShared _examine = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly IComponentFactory _componentFactory = default!;
    [Dependency] private readonly NumberObjectiveSystem _number = default!;
    [Dependency] private readonly ObjectivesSystem _objectives = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly TargetObjectiveSystem _targetObjectives = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedWieldableSystem _wieldable = default!;
    [Dependency] private readonly MovementModStatusSystem _movementMod = default!;
    [Dependency] private readonly IRobustRandom _rand = default!;
    [Dependency] private readonly TileSystem _tiles = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedStealthSystem _stealth = default!;
    [Dependency] private readonly TurfSystem _turf = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;
    [Dependency] private readonly SharedEnsnareableSystem _ensnare = default!;
    [Dependency] private readonly SharedChargesSystem _charges = default!;
    [Dependency] private readonly ILogManager _log = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly PolymorphSystem _polymorph = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly FlammableSystem _flammable = default!;
    [Dependency] private readonly MobThresholdSystem _mobThreshold = default!;
    [Dependency] private readonly SharedFlashSystem _flash = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movement = default!;
    [Dependency] private readonly DestructibleSystem _destructible = default!;
    [Dependency] private readonly SharedCollectiveMindSystem _collectiveMind = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;

    private ISawmill? _sawmill;
    private static readonly ProtoId<DamageGroupPrototype> _bruteGroupId = "Brute";
    private static readonly ProtoId<DamageGroupPrototype> _burnGroupId = "Burn";
    private static readonly ProtoId<DamageGroupPrototype> _geneticGroupId = "Genetic";
    private static readonly ProtoId<DamageTypePrototype> _poisonTypeId = "Poison";
    private static readonly ProtoId<DamageTypePrototype> _oxyLossTypeId = "Asphyxiation";
    private static readonly SoundSpecifier _spaceBurnSound = new SoundPathSpecifier("/Audio/Effects/lightburn.ogg");

    public override void Initialize()
    {
        base.Initialize();
        _sawmill = _log.GetSawmill("Vampire");

        SubscribeLocalEvent<VampireComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<VampireComponent, ComponentShutdown>(OnShutdown);
        InitializeAbilities();
        InitializeObjectives();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_timing.IsFirstTimePredicted)
            return;

        var query = EntityQueryEnumerator<VampireComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (_timing.CurTime <= comp.NextUpdate)
                continue;

            comp.NextUpdate = _timing.CurTime + comp.UpdateDelay;
            var bloodChanged = ProcessBloodDecay(uid, comp);

            if (bloodChanged || ShouldRefreshActions(comp))
                RefreshAllActions(uid, comp);

            TryGrantClassAbilities(uid, comp);
            HandleClassSelection(uid, comp);
            EnsureRejuvenateUpgrade(uid, comp);
            HandleHolyWater(uid, comp);
            HandleHolyPlace(uid, comp);
        }

        var sunlightQuery = EntityQueryEnumerator<VampireSunlightComponent, TransformComponent>();
        while (sunlightQuery.MoveNext(out var uid, out var sunlight, out var xform))
        {
            if (!TryComp<VampireComponent>(uid, out var vampire))
                continue;

            HandleSpaceExposure(uid, vampire, sunlight, xform, frameTime);
        }
    }

    private void HandleSpaceExposure(EntityUid uid, VampireComponent vampire, VampireSunlightComponent sunlight, TransformComponent xform, float frameTime)
    {
        if (_container.IsEntityInContainer(uid))
        {
            ResetSpaceExposure(sunlight);
            return;
        }

        if (!IsInSpace(xform))
        {
            ResetSpaceExposure(sunlight);
            return;
        }

        if (TryComp<MobStateComponent>(uid, out var mobState) &&
            mobState.CurrentState == Shared.Mobs.MobState.Dead)
        {
            ResetSpaceExposure(sunlight);
            return;
        }

        var frameSpan = TimeSpan.FromSeconds(frameTime);
        sunlight.TimeInSpace += frameSpan;

        if (sunlight.TimeInSpace < sunlight.GracePeriod)
            return;

        if (_timing.CurTime >= sunlight.NextWarningPopup)
        {
            _popup.PopupEntity(Loc.GetString(sunlight.WarningPopup), uid, uid, PopupType.LargeCaution);
            sunlight.NextWarningPopup = _timing.CurTime + sunlight.WarningPopupCooldown;
        }

        sunlight.DamageAccumulator += frameSpan;

        var interval = sunlight.DamageInterval;
        if (interval < TimeSpan.FromSeconds(0.1f))
            interval = TimeSpan.FromSeconds(0.1f);

        while (sunlight.DamageAccumulator >= interval)
        {
            sunlight.DamageAccumulator -= interval;

            if (!ProcessSpaceExposureTick(uid, vampire, sunlight))
                return;
        }
    }

    private void ResetSpaceExposure(VampireSunlightComponent sunlight)
    {
        sunlight.TimeInSpace = TimeSpan.Zero;
        sunlight.DamageAccumulator = TimeSpan.Zero;
        sunlight.NextWarningPopup = TimeSpan.Zero;
    }

    private bool ProcessSpaceExposureTick(EntityUid uid, VampireComponent vampire, VampireSunlightComponent sunlight)
    {
        var hadBlood = vampire.DrunkBlood > 0;

        if (hadBlood)
        {
            DrainBlood(uid, vampire, sunlight);

        }
        else
        {
            if (!ApplyGeneticSpaceDamage(uid, sunlight))
                return false;
        }

        var damageable = CompOrNull<DamageableComponent>(uid);
        var thresholds = CompOrNull<MobThresholdsComponent>(uid);
        var healthy = IsAboveHalfHealth(uid, damageable, thresholds);

        var chance = hadBlood ? sunlight.BloodEffectChance : sunlight.BloodlessEffectChance;
        TryApplySpaceDamage(uid, healthy, chance, sunlight);

        return true;
    }

    private void DrainBlood(EntityUid uid, VampireComponent vampire, VampireSunlightComponent sunlight)
    {
        var drain = Math.Min(sunlight.BloodDrainPerInterval, vampire.DrunkBlood);
        if (drain <= 0)
            return;

        vampire.DrunkBlood -= drain;
        Dirty(uid, vampire);
    }

    private bool ApplyGeneticSpaceDamage(EntityUid uid, VampireSunlightComponent sunlight)
    {
        var damageGroup = GetCachedDamageGroup(_geneticGroupId);
        if (damageGroup == null)
            return true;

        var spec = new DamageSpecifier();
        spec += new DamageSpecifier(damageGroup, sunlight.GeneticDamagePerInterval);
        _damageableSystem.TryChangeDamage(uid, spec, true);

        if (!TryComp(uid, out DamageableComponent? damageable) ||
            damageable == null ||
            !damageable.DamagePerGroup.TryGetValue(_geneticGroupId, out var geneticDamage))
        {
            return true;
        }

        _audio.PlayPvs(_spaceBurnSound, uid);

        if (geneticDamage < sunlight.GeneticDustThreshold)
            return true;

        DustEntity(uid);
        return false;
    }

    private void TryApplySpaceDamage(EntityUid uid, bool isHealthy, float chance, VampireSunlightComponent sunlight)
    {
        if (!_rand.Prob(Math.Clamp(chance, 0f, 1f)))
            return;

        if (isHealthy)
        {
            ApplyDamage(uid, "Heat", sunlight.BurnDamage);
        }
        else
        {
            _flammable.AdjustFireStacks(uid, sunlight.FireStacksOnIgnite, ignite: true);
        }

        _audio.PlayPvs(_spaceBurnSound, uid);
    }

    private bool IsAboveHalfHealth(EntityUid uid, DamageableComponent? damageable, MobThresholdsComponent? thresholds)
    {
        damageable ??= CompOrNull<DamageableComponent>(uid);
        thresholds ??= CompOrNull<MobThresholdsComponent>(uid);

        if (damageable == null)
            return true;

        if (!_mobThreshold.TryGetDeadThreshold(uid, out var deadThreshold, thresholds) ||
            deadThreshold == null ||
            deadThreshold.Value == FixedPoint2.Zero)
        {
            return true;
        }

        var max = deadThreshold.Value.Float();
        if (max <= 0f)
            return true;

        var current = damageable.TotalDamage.Float();
        return current <= max * 0.5f;
    }

    private void DustEntity(EntityUid uid)
    {
        var coords = Transform(uid).Coordinates;
        QueueDel(uid);
        Spawn("Ash", coords);
        _popup.PopupEntity(Loc.GetString("admin-smite-turned-ash-other", ("name", uid)), uid, PopupType.LargeCaution);
    }

    private bool IsInSpace(TransformComponent xform)
    {
        if (xform.GridUid == null)
            return true;

        if (!TryComp(xform.GridUid.Value, out MapGridComponent? grid))
            return true;

        if (!_map.TryGetTileRef(xform.GridUid.Value, grid, xform.Coordinates, out var tileRef))
            return true;

        return _turf.IsSpace(tileRef);
    }

    private bool ProcessBloodDecay(EntityUid uid, VampireComponent comp)
    {
        var before = comp.BloodFullness;
        if (before <= 0)
            return false;

        comp.BloodFullness = MathF.Max(0, before - comp.FullnessDecayPerSecond);
        var changed = !MathF.Abs(comp.BloodFullness - before).Equals(0f);

        if (changed)
        {
            Dirty(uid, comp);
            UpdateVampireFedAlert(uid, comp);
        }

        return changed;
    }

    private bool ShouldRefreshActions(VampireComponent comp) => Math.Abs(comp.TotalBlood - comp.LastRefreshedBloodLevel) >= comp.ActionRefreshThreshold;

    private void RefreshAllActions(EntityUid uid, VampireComponent comp)
    {
        comp.LastRefreshedBloodLevel = comp.TotalBlood;
        foreach (var (_, actionEntity) in comp.ActionEntities)
            TryRefreshVampireAction(uid, actionEntity);
    }

    private void HandleClassSelection(EntityUid uid, VampireComponent comp)
    {
        if (HasChosenClass(uid))
            return;

        var classSelectAction = comp.ClassSelectActionId;

        if (comp.TotalBlood >= comp.ClassSelectThreshold && !comp.ActionEntities.ContainsKey(classSelectAction))
        {
            EntityUid? actionEntity = null;
            _actions.AddAction(uid, ref actionEntity, classSelectAction, uid);
            if (actionEntity != null)
            {
                comp.ActionEntities[classSelectAction] = actionEntity.Value;
                Dirty(uid, comp);
            }
        }

        if (comp.ActionEntities.TryGetValue(classSelectAction, out var classSelectActionEntity))
            TryRefreshVampireAction(uid, classSelectActionEntity);
    }

    private void OnStartup(EntityUid uid, VampireComponent comp, ComponentStartup args)
    {
        EnsureComp<VampireSunlightComponent>(uid);
        foreach (var actionId in comp.BaseVampireActions)
        {
            EntityUid? action = null;

            _actions.AddAction(uid, ref action, actionId, uid);

            if (action != null)
                comp.ActionEntities[actionId] = action.Value;
        }
        RemComp<HungerComponent>(uid);
        RemComp<ThirstComponent>(uid);

        _alerts.ClearAlertCategory(uid, "Hunger");

        UpdateVampireAlert(uid);
        UpdateVampireFedAlert(uid, comp);

    }

    private void OnShutdown(EntityUid uid, VampireComponent comp, ComponentShutdown args)
    {
        if (TryComp<VampireDrainBeamComponent>(uid, out var drainBeamComp))
        {
            foreach (var connection in drainBeamComp.ActiveBeams.Values)
            {
                var removeEvent = new VampireDrainBeamEvent(GetNetEntity(connection.Source), GetNetEntity(connection.Target), false);
                RaiseNetworkEvent(removeEvent);
            }
            drainBeamComp.ActiveBeams.Clear();
        }

        if (TryComp<UmbraeComponent>(uid, out var umbrae))
        {
            umbrae.ShadowBoxingActive = false;
            umbrae.ShadowBoxingTarget = null;
            umbrae.ShadowBoxingEndTime = null;
            umbrae.ShadowBoxingLoopRunning = false;
        }

        if (_playerShadowSnares.TryGetValue(uid, out var snares))
        {
            foreach (var trap in snares.ToArray())
            {
                if (Exists(trap))
                    QueueDel(trap);
            }
            _playerShadowSnares.Remove(uid);
        }
    }

    partial void UpdateVampireAlert(EntityUid uid);
    partial void UpdateVampireFedAlert(EntityUid uid, VampireComponent? comp);

    private void TryRefreshVampireAction(EntityUid owner, EntityUid? actionEntity)
    {
        if (actionEntity == null
            || _actions.GetAction(actionEntity) is not { } action
            || !TryComp<VampireComponent>(owner, out var vamp))
            return;

        if (!TryComp<VampireActionComponent>(actionEntity.Value, out var vac))
        {
            _actions.SetEnabled(action.AsNullable(), true);
            return;
        }

        var enabled = vamp.TotalBlood >= vac.BloodToUnlock &&
             (vac.RequiredClass == null || ValidateVampireClass(owner, vamp, vac.RequiredClass));

        _actions.SetEnabled(action.AsNullable(), enabled);
    }

    private void TryGrantClassAbilities(EntityUid uid, VampireComponent comp)
    {
        if (string.IsNullOrWhiteSpace(comp.ChosenClassId))
            return;

        if (!_proto.TryIndex<VampireClassPrototype>(comp.ChosenClassId, out var classProto))
            return;

        foreach (var actionId in classProto.Actions)
            GrantAbility(uid, comp, actionId);
    }

    private void GrantAbility(EntityUid uid, VampireComponent comp, EntProtoId actionId)
    {
        if (comp.ActionEntities.ContainsKey(actionId))
            return;

        EntityUid? field = null;
        GrantAbility(uid, comp, ref field, actionId);
    }

    private void GrantAbility(EntityUid uid, VampireComponent comp, ref EntityUid? field, EntProtoId actionId)
    {
        if (field != null)
            return;

        var threshold = GetActionBloodThreshold(actionId);

        if (comp.TotalBlood >= threshold)
        {
            _actions.AddAction(uid, ref field, actionId, uid);
            if (field != null)
            {
                comp.ActionEntities[actionId] = field.Value;
                Dirty(uid, comp);
            }
        }
    }

    private int GetActionBloodThreshold(EntProtoId actionId)
    {
        if (_proto.TryIndex<EntityPrototype>(actionId, out var proto) &&
            proto.TryGetComponent<VampireActionComponent>(out var vac, _componentFactory))
            return vac.BloodToUnlock;
        return 0;
    }
    private void EnsureRejuvenateUpgrade(EntityUid uid, VampireComponent comp)
    {
        if (comp.RejuvenateActions.Count < 2)
        {
            _sawmill?.Error($"Vampire {ToPrettyString(uid)} missing rejuvenate action config");
            return;
        }

        var rejuvenateI = comp.RejuvenateActions[0];
        var rejuvenateII = comp.RejuvenateActions[1];

        var unlockThreshold = GetActionBloodThreshold(rejuvenateII);
        if (comp.TotalBlood < unlockThreshold)
            return;

        if (!comp.ActionEntities.ContainsKey(rejuvenateII))
        {
            EntityUid? action = null;
            _actions.AddAction(uid, ref action, rejuvenateII, uid);
            if (action != null)
                comp.ActionEntities[rejuvenateII] = action.Value;
        }

        TryRefreshVampireAction(uid, comp.ActionEntities[rejuvenateII]);
        if (comp.ActionEntities.TryGetValue(rejuvenateI, out var firstAction))
        {
            _actions.RemoveAction(uid, firstAction);
            comp.ActionEntities.Remove(rejuvenateI);
        }

        Dirty(uid, comp);
    }

    private void HandleHolyWater(EntityUid uid, VampireComponent comp)
    {
        if (comp.UniqueHumanoidVictims < 1)
            return;

        if (_timing.CurTime < comp.NextHolyWaterTick)
            return;

        var holywater = _solution.GetTotalPrototypeQuantity(uid, comp.HolyWaterReagentId);
        if (holywater <= FixedPoint2.Zero)
            return;

        if (TryComp(uid, out MobStateComponent? mobState) && mobState.CurrentState == Shared.Mobs.MobState.Dead)
            return;

        comp.NextHolyWaterTick = _timing.CurTime + comp.HolyTickDelay;

        if (comp.DrunkBlood > 0)
        {
            comp.DrunkBlood = Math.Max(0, comp.DrunkBlood - 3);
            Dirty(uid, comp);

            ApplyGroupDamage(uid, _bruteGroupId, 3f);

            if (TryComp(uid, out StaminaComponent? stamina))
                _stamina.TakeStaminaDamage(uid, 5f, stamina);

            return;
        }

        ApplyGroupDamage(uid, _burnGroupId, 2f);
        if (_rand.Prob(0.25f))
            _flammable.AdjustFireStacks(uid, 2f, ignite: true);
    }

    private void HandleHolyPlace(EntityUid uid, VampireComponent comp)
    {
        if (comp.UniqueHumanoidVictims < 1)
            return;

        if (_timing.CurTime < comp.NextHolyPlaceTick)
            return;

        if (!IsInHolyPlace(uid, comp))
            return;

        if (TryComp(uid, out MobStateComponent? mobState) && mobState.CurrentState == Shared.Mobs.MobState.Dead)
            return;

        comp.NextHolyPlaceTick = _timing.CurTime + comp.HolyTickDelay;

        if (_timing.CurTime >= comp.NextHolyPlacePopup)
        {
            _popup.PopupEntity(Loc.GetString("vampire-holy-place-burn"), uid, uid, PopupType.MediumCaution);
            comp.NextHolyPlacePopup = _timing.CurTime + TimeSpan.FromSeconds(5);
        }

        var health = GetApproximateHealth(uid);
        if (health <= 50f)
        {
            _flammable.AdjustFireStacks(uid, 3f, ignite: true);
            return;
        }

        ApplyDamage(uid, "Heat", 3f);
    }

    private bool IsInHolyPlace(EntityUid uid, VampireComponent comp)
    {
        if (_container.IsEntityInContainer(uid))
            return false;

        var coords = Transform(uid).Coordinates;
        foreach (var ent in _lookup.GetEntitiesInRange(coords, comp.HolyPlaceRange, LookupFlags.Static | LookupFlags.Dynamic | LookupFlags.Sundries))
        {
            if (ent == uid)
                continue;

            if (HasComp<PrayableComponent>(ent))
                return true;
        }

        return false;
    }

    private float GetApproximateHealth(EntityUid uid)
    {
        if (!TryComp(uid, out DamageableComponent? damageable))
            return 100f;

        if (!_mobThreshold.TryGetDeadThreshold(uid, out var deadThreshold, CompOrNull<MobThresholdsComponent>(uid))
            || deadThreshold == null
            || deadThreshold.Value == FixedPoint2.Zero)
        {
            return 100f - damageable.TotalDamage.Float();
        }

        return deadThreshold.Value.Float() - damageable.TotalDamage.Float();
    }

    private void ApplyGroupDamage(EntityUid uid, ProtoId<DamageGroupPrototype> groupId, float amount)
    {
        var group = GetCachedDamageGroup(groupId);
        if (group == null)
            return;

        var spec = new DamageSpecifier();
        spec += new DamageSpecifier(group, FixedPoint2.New(amount));
        _damageableSystem.TryChangeDamage(uid, spec, true);
    }

    private void OpenClassUi(EntityUid uid, VampireComponent comp)
    {
        if (!string.IsNullOrWhiteSpace(comp.ChosenClassId))
            return;
        _ui.CloseUi(uid, VampireClassUiKey.Key);
        _ui.OpenUi(uid, VampireClassUiKey.Key, uid);
    }

    private void OnVampireClassChosen(EntityUid uid, VampireComponent comp, VampireClassChosenBuiMsg msg)
    {
        if (!string.IsNullOrWhiteSpace(comp.ChosenClassId))
        {
            _ui.CloseUi(uid, VampireClassUiKey.Key);
            return;
        }

        if (string.IsNullOrWhiteSpace(msg.Choice) || !_proto.TryIndex<VampireClassPrototype>(msg.Choice, out var classProto))
        {
            _ui.CloseUi(uid, VampireClassUiKey.Key);
            return;
        }

        var reg = _componentFactory.GetRegistration(classProto.ClassComponent, ignoreCase: true);
        var classComp = _componentFactory.GetComponent(reg.Type);
        EntityManager.AddComponent(uid, classComp);

        comp.ChosenClassId = classProto.ID;

        var classSelectAction = comp.ClassSelectActionId;
        if (comp.ActionEntities.TryGetValue(classSelectAction, out var actionEntity))
        {
            _actions.RemoveAction(uid, actionEntity);
            comp.ActionEntities.Remove(classSelectAction);
        }

        _ui.CloseUi(uid, VampireClassUiKey.Key);

        Dirty(uid, comp);
    }

    private void OnVampireClassClosed(EntityUid uid, VampireComponent comp, VampireClassClosedBuiMsg _)
    {
        if (!string.IsNullOrWhiteSpace(comp.ChosenClassId))
            return;

        _sawmill?.Debug($"Vampire class UI closed without selection for {ToPrettyString(uid)} (blood={comp.TotalBlood})");
    }

    #region Objectives
    private void InitializeObjectives()
        => SubscribeLocalEvent<BloodDrainConditionComponent, ObjectiveGetProgressEvent>(OnBloodDrainGetProgress);

    private void OnBloodDrainGetProgress(EntityUid uid, BloodDrainConditionComponent comp, ref ObjectiveGetProgressEvent args)
    {
        var target = _number.GetTarget(uid);
        if (args.Mind.OwnedEntity != null && TryComp<VampireComponent>(args.Mind.OwnedEntity.Value, out var vampComp))
            args.Progress = target > 0 ? MathF.Min(vampComp.TotalBlood / target, 1f) : 1f;
        else
            args.Progress = 0f;
    }

    #endregion
}
