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
using Content.Shared.DoAfter;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Content.Shared.Damage.Systems;
using Content.Shared.Damage.Components;
using Robust.Shared.GameObjects;

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

    private const string VampireBloodAlertId = "VampireBlood";
    private const string VampireFedAlertId = "VampireFed";
    private TimeSpan _nextDecay;
    private readonly TimeSpan _decayInterval = TimeSpan.FromSeconds(1);

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VampireComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<VampireComponent, ComponentShutdown>(OnShutdown);
        SubscribeAbilities();
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

        // Grant the glare action.
        _actions.AddAction(uid, ref comp.GlareActionEntity, comp.GlareAction, uid);

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

    // Method hooks implemented in VampireSystem.Abilities.cs
    partial void SubscribeAbilities();
    partial void UpdateVampireAlert(EntityUid uid);
    partial void UpdateVampireFedAlert(EntityUid uid, VampireComponent? comp);
}
