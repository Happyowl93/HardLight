using Content.Shared._Starlight.Antags.Vampires;
using Content.Shared.Actions;
using Content.Shared.Alert;
using Content.Shared.Toggleable;
using Content.Shared.Interaction;
using Content.Shared.Body.Components;
using System;
using Content.Server.Actions;
using Content.Server.Body.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Content.Shared.Nutrition.Components;

namespace Content.Server._Starlight.Antags.Vampires;

public sealed class VampireSystem : EntitySystem
{
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly BloodstreamSystem _blood = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private const string VampireBloodAlertId = "VampireBlood";
    private const string VampireFedAlertId = "VampireFed";
    private TimeSpan _nextDecay;
    private readonly TimeSpan _decayInterval = TimeSpan.FromSeconds(1);

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VampireComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<VampireComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<VampireComponent, ToggleActionEvent>(OnToggleFangs);
        SubscribeLocalEvent<VampireComponent, AfterInteractEvent>(OnAfterInteract);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_timing.IsFirstTimePredicted)
            return;

        if (_timing.CurTime < _nextDecay)
            return;

        _nextDecay = _timing.CurTime + _decayInterval;

    var query = EntityQueryEnumerator<VampireComponent>();
    while (query.MoveNext(out var uid, out var comp))
        {
            var before = comp.BloodFullness;
            if (before > 0)
            {
                comp.BloodFullness = MathF.Max(0, before - comp.FullnessDecayPerSecond);
                if (!MathF.Abs(comp.BloodFullness - before).Equals(0f))
                {
                    Dirty(uid, comp);
            UpdateVampireFedAlert(uid, comp);
                }
            }
        }
    }

    private void OnStartup(EntityUid uid, VampireComponent comp, ComponentStartup args)
    {
        // Grant the toggle fangs action.
        _actions.AddAction(uid, ref comp.ToggleFangsActionEntity, comp.ToggleFangsAction, uid);

        // Replace standard hunger with vampire blood fullness: remove HungerComponent and clear its alert category.
        if (HasComp<HungerComponent>(uid))
        {
            RemComp<HungerComponent>(uid);
            _alerts.ClearAlertCategory(uid, "Hunger");
        }

        // Ensure alerts are visible on spawn.
        UpdateVampireAlert(uid);
        UpdateVampireFedAlert(uid, comp);
    }

    private void OnShutdown(EntityUid uid, VampireComponent comp, ComponentShutdown args)
    {
        // Action cleanup is handled by ActionsSystem when owner is deleted; nothing special here.
    }

    private void OnToggleFangs(EntityUid uid, VampireComponent comp, ref ToggleActionEvent args)
    {
        if (args.Handled)
            return;

        if (comp.ToggleFangsActionEntity == null || args.Action != comp.ToggleFangsActionEntity)
            return;

        comp.FangsExtended = !comp.FangsExtended;
        Dirty(uid, comp);
        args.Handled = true;
    }

    private void OnAfterInteract(EntityUid uid, VampireComponent comp, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || !comp.FangsExtended)
            return;

        if (args.Target == null)
            return;

        var target = args.Target.Value;
        if (!HasComp<BloodstreamComponent>(target))
            return;

        // Drain blood from target
        if (_blood.TryModifyBloodLevel(target, -comp.SipAmount))
        {
            comp.DrunkBlood += (int) comp.SipAmount;
            comp.BloodFullness = MathF.Min(comp.MaxBloodFullness, comp.BloodFullness + comp.SipAmount);
            Dirty(uid, comp);
            UpdateVampireAlert(uid);
            UpdateVampireFedAlert(uid, comp);
            args.Handled = true;
        }
    }

    private void UpdateVampireAlert(EntityUid uid)
        => _alerts.ShowAlert(uid, VampireBloodAlertId);

    private void UpdateVampireFedAlert(EntityUid uid, VampireComponent? comp = null)
    {
        if (!Resolve(uid, ref comp, false))
            return;

        var frac = comp.MaxBloodFullness <= 0f ? 0f : comp.BloodFullness / comp.MaxBloodFullness;
        // 0 hungry, 1 fed, 2 well-fed, 3 full
        short severity = 0;
        if (frac >= 0.9f) severity = 3;
        else if (frac >= 0.6f) severity = 2;
        else if (frac >= 0.25f) severity = 1;
        _alerts.ShowAlert(uid, VampireFedAlertId, severity);
    }
}
