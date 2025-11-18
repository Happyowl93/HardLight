using Content.Shared._Starlight.NullSpace;
using Content.Shared._Starlight.Shadekin;
using Content.Shared.Teleportation.Components;

namespace Content.Server._Starlight.Shadekin;

public sealed partial class ShadekinSystem : EntitySystem
{
    private readonly int _portalCost = 150;
    private readonly int _phaseCost = 50;
    public void InitializeAbilities()
    {
        SubscribeLocalEvent<BrighteyeComponent, BrighteyePortalActionEvent>(OnPortalAction);
        SubscribeLocalEvent<BrighteyeComponent, BrighteyePhaseActionEvent>(OnPhaseAction);
    }

    private void OnPortalAction(EntityUid uid, BrighteyeComponent component, BrighteyePortalActionEvent args)
    {
        if (HasComp<NullSpaceComponent>(uid)) // No making portals while in nullspace!
        {
            args.Handled = true;
            return;
        }

        foreach (var station in _station.GetStations()) // Lets make sure the Portal **IS ON STATION!**
        {
            if (_station.GetLargestGrid(station) is not { } grid)
                return;

            if (Transform(uid).GridUid != grid)
                return;
        }

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

    private void OnPhaseAction(EntityUid uid, BrighteyeComponent component, BrighteyePhaseActionEvent args)
    {
        int cost = _phaseCost;
        if (HasComp<NullSpaceComponent>(uid))
        {
            cost = 0;
        }
        else if (!HasComp<NullSpaceComponent>(uid) && TryComp<ShadekinComponent>(uid, out var shadekin))
        {
            if (shadekin.CurrentState == ShadekinState.Extreme)
                return;
            else if (shadekin.CurrentState == ShadekinState.High)
                cost = component.MaxEnergy;
            else if (shadekin.CurrentState == ShadekinState.Annoying)
                cost *= 3;
            else if (shadekin.CurrentState == ShadekinState.Low)
                cost *= 2;
        }

        if (OnAttemptEnergyUse(uid, component, cost))
            _nullspace.Phase(uid);
        
        args.Handled = true;
    }
}