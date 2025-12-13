using Content.Server.Actions;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Body.Systems;
using Content.Server.Doors.Systems;
using Content.Server.Objectives.Components;
using Content.Server.Objectives.Systems;
using Content.Server.Polymorph.Systems;
using Content.Shared._Starlight.Antags.Vampires;
using Content.Shared._Starlight.Antags.Vampires.Components;
using Content.Shared._Starlight.Antags.Vampires.Components.Classes;
using Content.Shared.Alert;
using Content.Shared.Charges.Systems;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Flash;
using Content.Shared.Ensnaring;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.FixedPoint;
using Content.Shared.Maps;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Objectives.Components;
using Content.Shared.Popups;
using Content.Shared.StatusEffectNew;
using Content.Shared.Stealth;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Content.Shared.Wieldable;
using Robust.Shared.Audio;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Starlight.Antags.Vampires;

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
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly IComponentFactory _componentFactory = default!;
    [Dependency] private readonly NumberObjectiveSystem _number = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedWieldableSystem _wieldable = default!;
    [Dependency] private readonly MovementModStatusSystem _movementMod = default!;
    [Dependency] private readonly ShadekinSystem _shadekin = default!;
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
    [Dependency] private readonly DoorSystem _door = default!;

    private ISawmill? _sawmill;
    private static readonly ProtoId<DamageGroupPrototype> _bruteGroupId = "Brute";
    private static readonly ProtoId<DamageGroupPrototype> _burnGroupId = "Burn";
    private static readonly ProtoId<DamageGroupPrototype> _geneticGroupId = "Genetic";
    private static readonly ProtoId<DamageTypePrototype> _poisonTypeId = "Poison";
    private static readonly ProtoId<DamageTypePrototype> _oxyLossTypeId = "Asphyxiation";
    private static readonly EntProtoId _pacifiedStatusEffectId = "StatusEffectPacified";
    private static readonly SoundSpecifier _spaceBurnSound = new SoundPathSpecifier("/Audio/Effects/lightburn.ogg");

    public override void Initialize()
    {
        base.Initialize();
        _sawmill = _log.GetSawmill("Vampire");

        SubscribeLocalEvent<VampireComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<VampireComponent, ComponentShutdown>(OnShutdown);
        InitializeAbilities();
        InitializeHemomancer();
        InitializeUmbrae();
        InitializeDantalion();
        InitializeGargantua();
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

            if (TryComp<UmbraeComponent>(uid, out var umbrae))
            {
                UpdateCloakToggleState(comp, umbrae);
                UpdateCloakOfDarkness(uid, comp, umbrae);
            }

            TryGrantClassAbilities(uid, comp);
            HandleClassSelection(uid, comp);
            EnsureRejuvenateUpgrade(uid, comp);
        }

        var sunlightQuery = EntityQueryEnumerator<VampireSunlightComponent, TransformComponent>();
        while (sunlightQuery.MoveNext(out var uid, out var sunlight, out var xform))
        {
            if (!TryComp<VampireComponent>(uid, out var vampire))
                continue;

            HandleSpaceExposure(uid, vampire, sunlight, xform, frameTime);
        }

        UpdateGargantua(frameTime);
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

        sunlight.TimeInSpace += frameTime;

        if (sunlight.TimeInSpace < sunlight.GracePeriod)
            return;

        if (_timing.CurTime >= sunlight.NextWarningPopup)
        {
            _popup.PopupEntity(Loc.GetString(sunlight.WarningPopup), uid, uid, PopupType.LargeCaution);
            sunlight.NextWarningPopup = _timing.CurTime + TimeSpan.FromSeconds(sunlight.WarningPopupCooldown);
        }
        sunlight.DamageAccumulator += frameTime;
        var interval = Math.Max(sunlight.DamageInterval, 0.1f);
        while (sunlight.DamageAccumulator >= interval)
        {
            sunlight.DamageAccumulator -= interval;

            if (!ProcessSpaceExposureTick(uid, vampire, sunlight))
                return;
        }
    }

    private void ResetSpaceExposure(VampireSunlightComponent sunlight)
    {
        sunlight.TimeInSpace = 0f;
        sunlight.DamageAccumulator = 0f;
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
        spec += new DamageSpecifier(damageGroup, FixedPoint2.New(sunlight.GeneticDamagePerInterval));
        _damageableSystem.TryChangeDamage(uid, spec, true);

        if (!TryComp(uid, out DamageableComponent? damageable) ||
            !damageable.DamagePerGroup.TryGetValue(_geneticGroupId, out var geneticDamage))
        {
            return true;
        }

        if (geneticDamage.Float() < sunlight.GeneticDustThreshold)
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

    private void UpdateCloakToggleState(VampireComponent vampire, UmbraeComponent umbrae)
    {
        if (vampire.ActionEntities.TryGetValue("ActionVampireCloakOfDarkness", out var actionEntity)
            && _actions.GetAction(actionEntity) is { } cloakAction)
            _actions.SetToggled(cloakAction.AsNullable(), umbrae.CloakOfDarknessActive);
    }

    private void UpdateCloakOfDarkness(EntityUid uid, VampireComponent vampire, UmbraeComponent umbrae)
    {
        if (!umbrae.CloakOfDarknessActive)
            return;

        if (TryComp<MobStateComponent>(uid, out var mobState)
            && mobState.CurrentState == Shared.Mobs.MobState.Dead)
        {
            DeactivateCloakOfDarkness(uid, umbrae);
            if (vampire.ActionEntities.TryGetValue("ActionVampireCloakOfDarkness", out var actionEntity)
                && _actions.GetAction(actionEntity) is { } action)
            {
                _actions.SetToggled(action.AsNullable(), false);
            }
            return;
        }

        var lightLevel = _shadekin.GetLightExposure(uid);
        ApplyCloakEffects(uid, lightLevel);
    }

    private void HandleClassSelection(EntityUid uid, VampireComponent comp)
    {
        if (comp.ChosenClass != VampireClassType.None)
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

        if (TryComp<DantalionComponent>(uid, out var dantalion))
            ReleaseAllThralls(uid, dantalion);
    }

    partial void SubscribeAbilities();
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
                     (vac.RequiredClass == null || vamp.ChosenClass == vac.RequiredClass);

        _actions.SetEnabled(action.AsNullable(), enabled);
    }

    private void TryGrantClassAbilities(EntityUid uid, VampireComponent comp)
    {
        if (comp.ChosenClass == VampireClassType.None)
            return;

        switch (comp.ChosenClass)
        {
            case VampireClassType.Hemomancer:
                foreach (var actionId in comp.HemomancerActions)
                    GrantAbility(uid, comp, actionId);
                break;
            case VampireClassType.Umbrae:
                foreach (var actionId in comp.UmbraeActions)
                    GrantAbility(uid, comp, actionId);
                break;
            case VampireClassType.Dantalion:
                foreach (var actionId in comp.DantalionActions)
                    GrantAbility(uid, comp, actionId);
                break;
            case VampireClassType.Gargantua:
                foreach (var actionId in comp.GargantuaActions)
                    GrantAbility(uid, comp, actionId);
                break;
        }
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

        if (comp.ActionEntities.TryGetValue(rejuvenateII, out var actionEntity)
            && TryComp<VampireActionComponent>(actionEntity, out var rejuvII)
            && comp.TotalBlood < rejuvII.BloodToUnlock)
            return;
        else if (comp.TotalBlood < comp.RejuvenateIIThreshold)
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

    private void OpenClassUi(EntityUid uid, VampireComponent comp)
    {
        if (comp.ChosenClass != VampireClassType.None)
            return;
        _ui.CloseUi(uid, VampireClassUiKey.Key);
        _ui.OpenUi(uid, VampireClassUiKey.Key, uid);
    }

    private void OnVampireClassChosen(EntityUid uid, VampireComponent comp, VampireClassChosenBuiMsg msg)
    {
        if (comp.ChosenClass != VampireClassType.None)
        {
            _ui.CloseUi(uid, VampireClassUiKey.Key);
            return;
        }

        comp.ChosenClass = msg.Choice;

        switch (msg.Choice)
        {
            case VampireClassType.Umbrae:
                AddComp<UmbraeComponent>(uid);
                break;
            case VampireClassType.Hemomancer:
                AddComp<HemomancerComponent>(uid);
                break;
            case VampireClassType.Dantalion:
                AddComp<DantalionComponent>(uid);
                break;
            case VampireClassType.Gargantua:
                // AddComp<GargantuaComponent>(uid);
                break;
        }

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
        if (comp.ChosenClass != VampireClassType.None)
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
