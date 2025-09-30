using Content.Shared._Starlight.Antags.Vampires;
using Content.Shared._Starlight.Antags.Vampires.Components;
using Content.Shared._Starlight.Antags.Vampires.Components.Classes;
using Content.Shared.Alert;
using Content.Server.Actions;
using Content.Server.Body.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Content.Shared.Nutrition.Components;
using Content.Shared.DoAfter;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Content.Shared.Damage.Systems;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Hands.EntitySystems;
using Robust.Server.GameObjects;
using Robust.Shared.Physics.Systems;
using Content.Server.Objectives.Components;
using Content.Server.Objectives.Systems;
using Content.Shared.Objectives.Components;
using Content.Shared.Wieldable;
using Content.Shared.Movement.Systems;
using Content.Shared.Maps;
using Robust.Shared.Random;
using Content.Shared.Stealth;
using Content.Shared.StatusEffectNew;
using Content.Shared.Ensnaring;
using Content.Shared.Charges.Systems;

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
    [Dependency] private readonly VisibilitySystem _visibilitySystem = default!;
    [Dependency] private readonly SharedStealthSystem _stealth = default!;
    [Dependency] private readonly TurfSystem _turf = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;
    [Dependency] private readonly SharedEnsnareableSystem _ensnare = default!;
    [Dependency] private readonly SharedChargesSystem _charges = default!;
    [Dependency] private readonly ILogManager _log = default!;

    private ISawmill? _sawmill;

    // Rinary - move to resources

    private const string VampireBloodAlertId = "VampireBlood";
    private const string VampireFedAlertId = "VampireFed";
    private const string ActionToggleFangsId = "ActionVampireToggleFangs";
    private const string ActionGlareId = "ActionVampireGlare";
    private const string ActionRejuvenateIId = "ActionVampireRejuvenateI";
    private const string ActionRejuvenateIIId = "ActionVampireRejuvenateII";
    private const string ActionClassSelectId = "ActionVampireClassSelect";
    private const string ActionHemomancerClawsId = "ActionVampireHemomancerClaws";
    private const string ActionHemomancerTendrilsId = "ActionVampireHemomancerTendrils";
    private const string ActionBloodBarrierId = "ActionVampireBloodBarrier";
    private const string ActionSanguinePoolId = "ActionVampireSanguinePool";
    private const string ActionBloodEruptionId = "ActionVampireBloodEruption";
    private const string ActionBloodBringersRiteId = "ActionVampireBloodBringersRite";
    private const string ActionCloakOfDarknessId = "ActionVampireCloakOfDarkness";
    private const string ActionShadowSnareId = "ActionVampireShadowSnare";
    private const string ActionDarkPassageId = "ActionVampireDarkPassage";
    private const string ActionExtinguishId = "ActionVampireExtinguish";
    private const string ActionEternalDarknessId = "ActionVampireEternalDarkness";
    private const string ActionShadowAnchorId = "ActionVampireShadowAnchor";
    private const string ActionShadowBoxingId = "ActionVampireShadowBoxing";
    private static readonly ProtoId<DamageGroupPrototype> _bruteGroupId = "Brute";
    private static readonly ProtoId<DamageGroupPrototype> _burnGroupId = "Burn";
    private static readonly ProtoId<DamageTypePrototype> _poisonTypeId = "Poison";
    private static readonly ProtoId<DamageTypePrototype> _oxyLossTypeId = "Asphyxiation";

    public override void Initialize()
    {
        base.Initialize();
        _sawmill = _log.GetSawmill("Vampire");

        SubscribeLocalEvent<VampireComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<VampireComponent, ComponentShutdown>(OnShutdown);
        InitializeAbilities();
        InitializeHemomancer();
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
                return;

            comp.NextUpdate = _timing.CurTime + comp.UpdateDelay;
            var bloodChanged = ProcessBloodDecay(uid, comp);

            if (bloodChanged || ShouldRefreshActions(comp))
                RefreshAllActions(uid, comp);

            if (TryComp<UmbraeComponent>(uid, out var umbrae))
                UpdateCloakToggleState(comp, umbrae);

            if (bloodChanged)
            {
                TryGrantClassAbilities(uid, comp);
                HandleClassSelection(uid, comp);
                EnsureRejuvenateUpgrade(uid, comp);
            }
        }
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

    private bool ShouldRefreshActions(VampireComponent comp)
    {
        return Math.Abs(comp.TotalBlood - comp.LastRefreshedBloodLevel) >= comp.ActionRefreshThreshold;
    }

    private void RefreshAllActions(EntityUid uid, VampireComponent comp)
    {
        comp.LastRefreshedBloodLevel = comp.TotalBlood;

        var actionEntities = new[]
        {
            comp.Actions.ToggleFangsActionEntity,
            comp.Actions.GlareActionEntity,
            comp.Actions.RejuvenateIActionEntity,
            comp.Actions.RejuvenateIIActionEntity,
            comp.Actions.HemomancerClawsActionEntity,
            comp.Actions.HemomancerTendrilsActionEntity,
            comp.Actions.BloodBarrierActionEntity,
            comp.Actions.SanguinePoolActionEntity,
            comp.Actions.BloodEruptionActionEntity,
            comp.Actions.VampireCloakOfDarknessActionEntity,
            comp.Actions.ShadowSnareActionEntity,
            comp.Actions.DarkPassageActionEntity,
            comp.Actions.ExtinguishActionEntity,
            comp.Actions.EternalDarknessActionEntity,
            comp.Actions.ClassSelectActionEntity
        };

        foreach (var actionEntity in actionEntities)
            TryRefreshVampireAction(uid, actionEntity);
    }

    private void UpdateCloakToggleState(VampireComponent vampire, UmbraeComponent umbrae)
    {
        if (vampire.Actions.VampireCloakOfDarknessActionEntity != null)
        {
            if (_actions.GetAction(vampire.Actions.VampireCloakOfDarknessActionEntity) is { } cloakAction)
                _actions.SetToggled(cloakAction.AsNullable(), umbrae.CloakOfDarknessActive);
        }
    }

    private void HandleClassSelection(EntityUid uid, VampireComponent comp)
    {
        if (comp.ChosenClass != VampireClassType.None)
            return;

        if (comp.TotalBlood >= comp.ClassSelectThreshold && comp.Actions.ClassSelectActionEntity == null)
        {
            _actions.AddAction(uid, ref comp.Actions.ClassSelectActionEntity, ActionClassSelectId, uid);
            Dirty(uid, comp);
        }
        TryRefreshVampireAction(uid, comp.Actions.ClassSelectActionEntity);
    }

    private void OnStartup(EntityUid uid, VampireComponent comp, ComponentStartup args)
    {
        foreach (var actionId in comp.BaseVampireActions)
        {
            switch (actionId)
            {
                case ActionToggleFangsId:
                    _actions.AddAction(uid, ref comp.Actions.ToggleFangsActionEntity, ActionToggleFangsId, uid);
                    break;
                case ActionGlareId:
                    _actions.AddAction(uid, ref comp.Actions.GlareActionEntity, ActionGlareId, uid);
                    break;
                case ActionRejuvenateIId:
                    _actions.AddAction(uid, ref comp.Actions.RejuvenateIActionEntity, ActionRejuvenateIId, uid);
                    break;
                case ActionRejuvenateIIId:
                    _actions.AddAction(uid, ref comp.Actions.RejuvenateIIActionEntity, ActionRejuvenateIIId, uid);
                    break;
                default:
                    _actions.AddAction(uid, actionId);
                    break;
            }
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
                _shadowSnares.Remove(trap);
            }
            _playerShadowSnares.Remove(uid);
        }
    }

    partial void SubscribeAbilities();
    partial void UpdateVampireAlert(EntityUid uid);
    partial void UpdateVampireFedAlert(EntityUid uid, VampireComponent? comp);

    private void TryRefreshVampireAction(EntityUid owner, EntityUid? actionEntity)
    {
        if (actionEntity == null)
            return;

        if (_actions.GetAction(actionEntity) is not { } action)
            return;

        if (!TryComp<VampireComponent>(owner, out var vamp))
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
            
        void Grant(ref EntityUid? field, string actionId)
        {
            if (field != null)
                return;

            var threshold = GetActionBloodThreshold(actionId);

            if (comp.TotalBlood >= threshold)
            {
                _actions.AddAction(uid, ref field, actionId, uid);
                Dirty(uid, comp);
            }
        }

        switch (comp.ChosenClass)
        {
            case VampireClassType.Hemomancer:
                Grant(ref comp.Actions.HemomancerClawsActionEntity, ActionHemomancerClawsId);
                Grant(ref comp.Actions.HemomancerTendrilsActionEntity, ActionHemomancerTendrilsId);
                Grant(ref comp.Actions.BloodBarrierActionEntity, ActionBloodBarrierId);
                Grant(ref comp.Actions.SanguinePoolActionEntity, ActionSanguinePoolId);
                Grant(ref comp.Actions.BloodEruptionActionEntity, ActionBloodEruptionId);
                Grant(ref comp.Actions.BloodBringersRiteActionEntity, ActionBloodBringersRiteId);
                break;
            case VampireClassType.Umbrae:
                Grant(ref comp.Actions.VampireCloakOfDarknessActionEntity, ActionCloakOfDarknessId);
                Grant(ref comp.Actions.ShadowSnareActionEntity, ActionShadowSnareId);
                Grant(ref comp.Actions.DarkPassageActionEntity, ActionDarkPassageId);
                Grant(ref comp.Actions.ExtinguishActionEntity, ActionExtinguishId);
                Grant(ref comp.Actions.EternalDarknessActionEntity, ActionEternalDarknessId);
                Grant(ref comp.Actions.ShadowAnchorActionEntity, ActionShadowAnchorId);
                Grant(ref comp.Actions.ShadowBoxingActionEntity, ActionShadowBoxingId);
                break;
        }
    }

    private int GetActionBloodThreshold(string actionId)
    {
        if (_proto.TryIndex<EntityPrototype>(actionId, out var proto) &&
            proto.TryGetComponent<VampireActionComponent>(out var vac, _componentFactory))
        {
            return vac.BloodToUnlock;
        }
        return 0;
    }
    private void EnsureRejuvenateUpgrade(EntityUid uid, VampireComponent comp)
    {
        if (comp.Actions.RejuvenateIIActionEntity != null &&
            TryComp<VampireActionComponent>(comp.Actions.RejuvenateIIActionEntity.Value, out var rejuvII))
        {
            if (comp.TotalBlood < rejuvII.BloodToUnlock)
                return;
        }
        else
        {
            if (comp.TotalBlood < comp.RejuvenateIIThreshold)
                return;
        }

        if (comp.Actions.RejuvenateIIActionEntity == null)
            _actions.AddAction(uid, ref comp.Actions.RejuvenateIIActionEntity, ActionRejuvenateIIId, uid);

        TryRefreshVampireAction(uid, comp.Actions.RejuvenateIIActionEntity);
        if (comp.Actions.RejuvenateIActionEntity != null)
        {
            _actions.RemoveAction(uid, comp.Actions.RejuvenateIActionEntity);
            comp.Actions.RejuvenateIActionEntity = null;
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
        }

        if (comp.Actions.ClassSelectActionEntity != null)
        {
            _actions.RemoveAction(uid, comp.Actions.ClassSelectActionEntity);
            comp.Actions.ClassSelectActionEntity = null;
        }

        _ui.CloseUi(uid, VampireClassUiKey.Key);

        Dirty(uid, comp);
    }

    private void OnVampireClassClosed(EntityUid uid, VampireComponent comp, VampireClassClosedBuiMsg _)
    {
        if (comp.ChosenClass != VampireClassType.None)
            return;

        Log.Debug($"Vampire class UI closed without selection for {ToPrettyString(uid)} (blood={comp.TotalBlood})");
    }

    #region Objectives
    private void InitializeObjectives()
        => SubscribeLocalEvent<BloodDrainConditionComponent, ObjectiveGetProgressEvent>(OnBloodDrainGetProgress);

    private void OnBloodDrainGetProgress(EntityUid uid, BloodDrainConditionComponent comp, ref ObjectiveGetProgressEvent args)
    {
        var target = _number.GetTarget(uid);
        if (args.Mind.OwnedEntity != null && TryComp<VampireComponent>(args.Mind.OwnedEntity.Value, out var vampComp))
        {
            if (target > 0)
            {
                args.Progress = MathF.Min(vampComp.TotalBlood / target, 1f);
            }
            else
                args.Progress = 1f;
        }
        else
        {
            args.Progress = 0f;
        }
    }

    #endregion
}
