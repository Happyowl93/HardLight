using System.Linq;
using Content.Shared.Atmos;
using Content.Shared.Damage.Systems;
using Content.Shared.Interaction.Events;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Temperature.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Timing;

namespace Content.Shared.Temperature.Systems;

/// <summary>
/// This handles predicting temperature based speedup.
/// </summary>
public abstract class SharedTemperatureSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifier = default!;
    [Dependency] private readonly DamageableSystem _damage = default!; // Starlight
    [Dependency] private readonly SharedPopupSystem _popup = default!; // Starlight

    /// <summary>
    /// Band-aid for unpredicted atmos. Delays the application for a short period so that laggy clients can get the replicated temperature.
    /// </summary>
    private static readonly TimeSpan SlowdownApplicationDelay = TimeSpan.FromSeconds(1f);

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TemperatureSpeedComponent, OnTemperatureChangeEvent>(OnTemperatureChanged);
        SubscribeLocalEvent<TemperatureSpeedComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovementSpeedModifiers);
        SubscribeLocalEvent<TemperatureComponent, InteractionSuccessEvent>(OnInteractionSuccess); // Starlight
    }

    // Starlight begin

    private void OnInteractionSuccess(Entity<TemperatureComponent> ent, ref InteractionSuccessEvent args)
    {
        if (!TryComp<TemperatureComponent>(args.User, out var userTempComp))
            return;
        
        DealHugTemperatureDamage(ent, (args.User, userTempComp));
        DealHugTemperatureDamage((args.User, userTempComp), ent);
    }

    private void DealHugTemperatureDamage(Entity<TemperatureComponent> target, Entity<TemperatureComponent> giver)
    {
        var temp = giver.Comp.CurrentTemperature;

        if (target.Comp.ColdDamageThreshold >= temp)
        {
            _damage.TryChangeDamage(target.Owner, target.Comp.ColdHugDamage, ignoreResistances: true, interruptsDoAfters: false);
            _popup.PopupClient(Loc.GetString("hugging-too-cold"), target.Owner, PopupType.LargeCaution);
        }

        if (target.Comp.HeatDamageThreshold <= temp)
        {
            _damage.TryChangeDamage(target.Owner, target.Comp.HeatHugDamage, ignoreResistances: true, interruptsDoAfters: false);
            _popup.PopupClient(Loc.GetString("hugging-too-hot"), target.Owner, PopupType.LargeCaution);
        }
    }

    // Starlight end
    
    private void OnTemperatureChanged(Entity<TemperatureSpeedComponent> ent, ref OnTemperatureChangeEvent args)
    {
        foreach (var (threshold, modifier) in ent.Comp.Thresholds)
        {
            if (args.CurrentTemperature < threshold && args.LastTemperature > threshold ||
                args.CurrentTemperature > threshold && args.LastTemperature < threshold)
            {
                ent.Comp.NextSlowdownUpdate = _timing.CurTime + SlowdownApplicationDelay;
                ent.Comp.CurrentSpeedModifier = modifier;
                Dirty(ent);
                break;
            }
        }

        var maxThreshold = ent.Comp.Thresholds.Max(p => p.Key);
        if (args.CurrentTemperature > maxThreshold && args.LastTemperature < maxThreshold)
        {
            ent.Comp.NextSlowdownUpdate = _timing.CurTime + SlowdownApplicationDelay;
            ent.Comp.CurrentSpeedModifier = null;
            Dirty(ent);
        }
    }

    private void OnRefreshMovementSpeedModifiers(Entity<TemperatureSpeedComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        // Don't update speed and mispredict while we're compensating for lag.
        if (ent.Comp.NextSlowdownUpdate != null || ent.Comp.CurrentSpeedModifier == null)
            return;

        args.ModifySpeed(ent.Comp.CurrentSpeedModifier.Value, ent.Comp.CurrentSpeedModifier.Value);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<TemperatureSpeedComponent, MovementSpeedModifierComponent>();
        while (query.MoveNext(out var uid, out var temp, out var movement))
        {
            if (temp.NextSlowdownUpdate == null)
                continue;

            if (_timing.CurTime < temp.NextSlowdownUpdate)
                continue;

            temp.NextSlowdownUpdate = null;
            _movementSpeedModifier.RefreshMovementSpeedModifiers(uid, movement);
            Dirty(uid, temp);
        }
    }

    public virtual void ChangeHeat(EntityUid uid, float heatAmount, bool ignoreHeatResistance = false, TemperatureComponent? temperature = null)
    {

    }

    public float GetHeatCapacity(EntityUid uid, TemperatureComponent? comp = null, PhysicsComponent? physics = null)
    {
        if (!Resolve(uid, ref comp) || !Resolve(uid, ref physics, false) || physics.FixturesMass <= 0)
        {
            return Atmospherics.MinimumHeatCapacity;
        }

        return comp.SpecificHeat * physics.FixturesMass;
    }
}
