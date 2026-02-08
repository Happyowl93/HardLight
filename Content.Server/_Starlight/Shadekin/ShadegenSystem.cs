using Content.Server.Light.EntitySystems;
using Content.Shared._Starlight.Shadekin;
using Content.Shared.Light;
using Content.Shared.Light.Components;
using Robust.Shared.Timing;

namespace Content.Server._Starlight.Shadekin;

public sealed partial class ShadegenSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly PoweredLightSystem _light = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedHandheldLightSystem _handheldLight = default!;
    public override void Initialize()
    {
        base.Initialize();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ShadegenComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            if (_timing.CurTime < component.NextUpdate)
                continue;

            component.NextUpdate = _timing.CurTime + component.UpdateCooldown;

            var lightQuery = _lookup.GetEntitiesInRange<HandheldLightComponent>(Transform(uid).Coordinates, component.Range, LookupFlags.Uncontained);

            foreach (var light in lightQuery)
            {
                if (!light.Comp.Activated || HasComp<DarkLightComponent>(light.Owner))
                    continue;

                _handheldLight.TurnOff(light);
            }

            if (!component.DestroyLights)
                continue;

            var poweredLightQuery = _lookup.GetEntitiesInRange<PoweredLightComponent>(Transform(uid).Coordinates, component.Range, LookupFlags.StaticSundries);

            foreach (var light in poweredLightQuery)
            {
                if (!light.Comp.On || HasComp<DarkLightComponent>(light.Owner))
                    continue;

                _light.TryDestroyBulb(light.Owner, light.Comp);
            }
        }
    }
}
