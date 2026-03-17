using Content.Server._Starlight.GameTicking.Rules.Components;
using Content.Server.AlertLevel;
using Content.Server.GameTicking;
using Content.Server.StationEvents.Components;
using Content.Shared._Starlight.GameTicking.Components;
using Content.Shared._Starlight.Light;
using Content.Shared.GameTicking.Components;
using Content.Shared.Light.Components;
using Content.Shared.Light.EntitySystems;
using Content.Shared.Station.Components;
using Robust.Shared.Player;

namespace Content.Server.StationEvents.Events;

public sealed class NightShiftRule : StationEventSystem<NightShiftRuleComponent>
{
    [Dependency] private readonly SharedPoweredLightSystem _poweredLightSystem = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NightShiftDimmedLightComponent, GetDimmedLightLevelEvent>(OnGetDimmedLightLevel);
        SubscribeLocalEvent<AlertLevelChangedEvent>(OnAlertLevelChanged);
    }
    
    /// <summary>
    /// React to alert level changes. Only used for disabling night shift dimming prematurely.
    /// </summary>
    private void OnAlertLevelChanged(AlertLevelChangedEvent ev)
    {
        var nightShiftQuery = EntityQueryEnumerator<NightShiftRuleComponent, GameRuleComponent>();
        while (nightShiftQuery.MoveNext(out var shift, out var nightShift, out var gameRule))
        {
            if (!_gameTicker.IsGameRuleActive(shift, gameRule)) continue;
            if (nightShift.PermittedAlertLevels.Contains(ev.AlertLevel)) continue;
            
            var affectedLightQuery = EntityQueryEnumerator<PoweredLightComponent, NightShiftDimmedLightComponent>();
            var announced = false;
            while (affectedLightQuery.MoveNext(out var uid, out var poweredLight, out var dimmed))
            {
                // Remove our dimming component from currently dimmed lights.
                RemComp<NightShiftDimmedLightComponent>(uid);
                _poweredLightSystem.UpdateLight(uid, poweredLight);

                // Announce that we are ending early, once.
                if (!announced && TryComp<StationEventComponent>(shift, out var stationEvent))
                {
                    announced = true;
                    Announce(stationEvent, Loc.GetString(nightShift.EmergencyEndAnnouncement), true);
                }
            }
        }
    }
    
    private void OnGetDimmedLightLevel(EntityUid uid, NightShiftDimmedLightComponent component, GetDimmedLightLevelEvent args)
    {
        args.LightEnergy *= component.LightEnergyMultiplier;
        args.PowerUse *= component.LightEnergyMultiplier;
    }

    protected override void Started(EntityUid uid, NightShiftRuleComponent comp, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, comp, gameRule, args);

        EntityUid? chosenStation;
        if (!TryComp<StationEventComponent>(uid, out var stationEvent)) return;
        chosenStation = stationEvent.TargetStation;
        if (chosenStation is null)
            if (!TryGetRandomStation(out chosenStation))
                return;
        
        // If station alert level doesn't allow night light, don't announce we're starting, just say "Disabling ...".
        if (TryComp<AlertLevelComponent>(chosenStation, out var alert)
            && !comp.PermittedAlertLevels.Contains(alert.CurrentLevel))
        {
            Announce(stationEvent, Loc.GetString(comp.EmergencyEndAnnouncement), true);
            return;
        }
        
        // Find eligible powered lights.
        var query = AllEntityQuery<PoweredLightComponent, TransformComponent>();
        while (query.MoveNext(out var ent, out var light, out var xform))
        {
            if (CompOrNull<StationMemberComponent>(xform.GridUid)?.Station != chosenStation)
                continue;
            
            // Add our dimmer component.
            var nightLightComp = EnsureComp<NightShiftDimmedLightComponent>(ent);
            nightLightComp.LightEnergyMultiplier = comp.LightEnergyMultiplier;
            _poweredLightSystem.UpdateLight(ent, light);
        }
    }

    protected override void Ended(EntityUid uid, NightShiftRuleComponent comp, GameRuleComponent gameRule, GameRuleEndedEvent args)
    {
        base.Ended(uid, comp, gameRule, args);
        
        if (!TryComp<StationEventComponent>(uid, out var stationEvent)) return;

        var query = AllEntityQuery<PoweredLightComponent, NightShiftDimmedLightComponent, TransformComponent>();
        var announced = false;
        while (query.MoveNext(out var ent, out var light, out _, out var xform))
        {
            // Ignore lights that aren't part of the station this event affectec.
            if (CompOrNull<StationMemberComponent>(xform.GridUid)?.Station != stationEvent.TargetStation)
                continue;
            
            // Remove and update light.
            if (!RemComp<NightShiftDimmedLightComponent>(ent)) continue;
            _poweredLightSystem.UpdateLight(ent, light);

            // Announce the event ended, once. If the event ended early (due to alert level),
            // we won't find any NightShiftDimmedLightComponents and thus never get here.
            if (!announced)
            {
                announced = true;
                Announce(stationEvent, Loc.GetString(comp.EndAnnouncement), true);
            }
        }
    }
}
