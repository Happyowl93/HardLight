using Content.Shared.Teleportation.Systems;
using Content.Shared._Starlight.Shadekin;
using Content.Shared.Anomaly.Components;
using Content.Shared.Light.Components;
using Content.Server.Light.EntitySystems;

namespace Content.Server._Starlight.Shadekin;

public abstract class DarkPortalSystem : EntitySystem
{
    [Dependency] private readonly LinkedEntitySystem _link = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly PoweredLightSystem _light = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DarkPortalComponent, ComponentStartup>(OnInit);
        SubscribeLocalEvent<DarkPortalComponent, AnomalyPulseEvent>(OnPulse);
        SubscribeLocalEvent<DarkPortalComponent, AnomalySupercriticalEvent>(OnSupercritical);
    }

    private void OnInit(EntityUid uid, DarkPortalComponent component, ComponentStartup args)
    {
        var query = EntityQueryEnumerator<DarkHubComponent>();
        while (query.MoveNext(out var target, out var portal))
            _link.TryLink(uid, target);
    }

    private void OnPulse(EntityUid uid, DarkPortalComponent component, ref AnomalyPulseEvent args)
    {
        var lights = GetEntityQuery<PoweredLightComponent>();

        foreach (var ent in _lookup.GetEntitiesInRange(uid, component.PulseRange))
            if (lights.HasComponent(ent))
                _light.TryDestroyBulb(ent);
    }

    private void OnSupercritical(EntityUid uid, DarkPortalComponent component, ref AnomalySupercriticalEvent args)
    {
        var lights = GetEntityQuery<PoweredLightComponent>();

        foreach (var ent in _lookup.GetEntitiesInRange(uid, component.SupercriticalRange))
            if (lights.HasComponent(ent))
                _light.TryDestroyBulb(ent);

        var newportal = SpawnAtPosition("PortalShadekin", Transform(uid).Coordinates);
        if (TryComp<DarkPortalComponent>(newportal, out var portal))
            portal.Brighteye = component.Brighteye;
    }
}