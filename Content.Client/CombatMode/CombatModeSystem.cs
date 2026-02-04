using Content.Client.Hands.Systems;
using Content.Client.NPC.HTN;
using Content.Shared.CCVar;
using Content.Shared.CombatMode;
using Content.Shared.Starlight.CCVar;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;

namespace Content.Client.CombatMode;

public sealed class CombatModeSystem : SharedCombatModeSystem
{
    [Dependency] private readonly IOverlayManager _overlayManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IInputManager _inputManager = default!;
    [Dependency] private readonly IEyeManager _eye = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    /// <summary>
    /// Raised whenever combat mode changes.
    /// </summary>
    public event Action<bool>? LocalPlayerCombatModeUpdated;

    private bool _lastState = false;
    private string _rangedSight = "GunSight";
    private string _meleeSight = "MeleeSight";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CombatModeComponent, AfterAutoHandleStateEvent>(OnHandleState);

        Subs.CVar(_cfg, CCVars.CombatModeIndicatorsPointShow, OnShowCombatIndicatorsChanged, true);
        Subs.CVar(_cfg, StarlightCCVars.RangedSight, OnRangedSightChanged, true);
        Subs.CVar(_cfg, StarlightCCVars.RangedSight, OnMeleeSightChanged, true);
    }

    private void OnHandleState(EntityUid uid, CombatModeComponent component, ref AfterAutoHandleStateEvent args)
    {
        UpdateHud(uid);
    }

    public override void Shutdown()
    {
        _overlayManager.RemoveOverlay<CombatModeIndicatorsOverlay>();

        base.Shutdown();
    }

    public bool IsInCombatMode()
    {
        var entity = _playerManager.LocalEntity;

        if (entity == null)
            return false;

        return IsInCombatMode(entity.Value);
    }

    public override void SetInCombatMode(EntityUid entity, bool value, CombatModeComponent? component = null)
    {
        base.SetInCombatMode(entity, value, component);
        UpdateHud(entity);
    }

    protected override bool IsNpc(EntityUid uid)
    {
        return HasComp<HTNComponent>(uid);
    }

    private void UpdateHud(EntityUid entity)
    {
        if (entity != _playerManager.LocalEntity || !Timing.IsFirstTimePredicted)
        {
            return;
        }

        var inCombatMode = IsInCombatMode();
        LocalPlayerCombatModeUpdated?.Invoke(inCombatMode);
    }

    private void OnRangedSightChanged(string sight)
    {
        _rangedSight = sight;
        var state = _lastState;
        OnShowCombatIndicatorsChanged(false);
        OnShowCombatIndicatorsChanged(state);
    }

    private void OnMeleeSightChanged(string sight)
    {

    }

    private void OnShowCombatIndicatorsChanged(bool isShow)
    {
        if (isShow != _lastState)
            _lastState = isShow;
        if (_lastState)
        {
            _overlayManager.AddOverlay(new CombatModeIndicatorsOverlay(
                _inputManager,
                EntityManager,
                _prototypeManager,
                _eye,
                this,
                EntityManager.System<HandsSystem>(),
                _rangedSight,
                _meleeSight));
        }
        else if (_overlayManager.HasOverlay<CombatModeIndicatorsOverlay>())
        {
            _overlayManager.RemoveOverlay<CombatModeIndicatorsOverlay>();
        }
    }
}
