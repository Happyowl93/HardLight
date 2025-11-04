using Content.Shared._Starlight.NullSpace;
using Content.Shared._Starlight.Shadekin;
using Content.Shared.Teleportation.Components;

namespace Content.Server._Starlight.Shadekin;

public sealed partial class ShadekinSystem : EntitySystem
{
    private readonly int _portalCost = 150;
    public void InitializeAbilities()
    {
        SubscribeLocalEvent<BrighteyeComponent, BrighteyePortalActionEvent>(OnPortalAction);
    }

    private void OnPortalAction(EntityUid uid, BrighteyeComponent component, BrighteyePortalActionEvent args)
    {
        if (HasComp<NullSpaceComponent>(uid)) // No making portals while in nullspace!
        {
            args.Handled = true;
            return;
        }

        // TODO: Before even trying to put down the portal, we need to make sure were on station.
        if (OnAttemptEnergyUse(uid, component, _portalCost))
        {
            _actionsSystem.RemoveAction(uid, component.PortalAction);

            EnsureComp<PortalTimeoutComponent>(uid); // Lets not teleport as soon we put down the portal, duh.

            var newportal = SpawnAtPosition(_shadekinPortal, Transform(uid).Coordinates);
            if (TryComp<DarkPortalComponent>(newportal, out var portal))
                portal.Brighteye = uid;

            component.Portal = newportal;

            _alerts.ClearAlert(uid, component.PortalAlert);
        }

        args.Handled = true;
    }
}