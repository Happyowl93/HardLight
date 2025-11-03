using Content.Shared.Teleportation.Systems;
using Content.Shared._Starlight.Shadekin;
using Content.Shared.Anomaly.Components;
using Content.Shared.Light.Components;
using Content.Server.Light.EntitySystems;
using Content.Shared.Examine;
using Content.Shared.Verbs;
using Robust.Shared.Prototypes;
using Content.Shared.Anomaly;

namespace Content.Server._Starlight.Shadekin;

public sealed class DarkPortalSystem : EntitySystem
{
    [Dependency] private readonly LinkedEntitySystem _link = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly PoweredLightSystem _light = default!;
    [Dependency] private readonly ShadekinSystem _shadekin = default!;
    [Dependency] private readonly SharedAnomalySystem _sharedAnomalySystem = default!;

    private readonly EntProtoId _shadekinShadow = "ShadekinShadow";
    private readonly EntProtoId _shadekinPortal = "PortalShadekin";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DarkPortalComponent, ComponentStartup>(OnInit);
        SubscribeLocalEvent<DarkPortalComponent, AnomalyPulseEvent>(OnPulse);
        SubscribeLocalEvent<DarkPortalComponent, AnomalySupercriticalEvent>(OnSupercritical);
        SubscribeLocalEvent<DarkPortalComponent, AnomalyShutdownEvent>(OnShutdown);

        SubscribeLocalEvent<DarkPortalComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<DarkPortalComponent, GetVerbsEvent<InteractionVerb>>(OnGetInteractionVerbs);
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

        // TODO: Set Brighteye Portal to new portal.

        var newportal = SpawnAtPosition(_shadekinPortal, Transform(uid).Coordinates);
        if (TryComp<DarkPortalComponent>(newportal, out var portal))
            portal.Brighteye = component.Brighteye;
    }

    private void OnShutdown(EntityUid uid, DarkPortalComponent component, ref AnomalyShutdownEvent args)
    {
        // TODO: Alert the Brighteye that his portal has been killed.
    }

    private void OnExamined(EntityUid uid, DarkPortalComponent component, ref ExaminedEvent args)
    {
        if (component.Brighteye != args.Examiner)
            return;

        args.PushMarkup(Loc.GetString("shadekin-portal-owner"));
    }

    private void OnGetInteractionVerbs(EntityUid uid, DarkPortalComponent component, ref GetVerbsEvent<InteractionVerb> args)
    {
        if (!args.CanAccess || component.Brighteye != args.User || !TryComp<AnomalyComponent>(uid, out var anomaly))
            return;

        args.Verbs.Add(new()
        {
            Act = () =>
            {
                SpawnAtPosition(_shadekinShadow, Transform(uid).Coordinates);
                QueueDel(uid);
            },
            Text = Loc.GetString("shadekin-portal-destroy"),
        });

        // TODO: This should use Shadekin Energy to stabilize.
        args.Verbs.Add(new()
        {
            Act = () =>
            {
                _sharedAnomalySystem.ChangeAnomalyStability(uid, -0.5f, anomaly);
                _sharedAnomalySystem.ChangeAnomalySeverity(uid, -0.5f, anomaly);
                _sharedAnomalySystem.ChangeAnomalyHealth(uid, 0.5f, anomaly);
            },
            Text = Loc.GetString("shadekin-portal-stabilize"),
        });
    }
}