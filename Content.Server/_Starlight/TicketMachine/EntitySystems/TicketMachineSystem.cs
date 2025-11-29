using Content.Shared._Starlight.TicketMachine.Components;
using Content.Shared._Starlight.TicketMachine.EntitySystems;
using Content.Shared.Power.EntitySystems;
using Content.Shared.DeviceLinking.Events;
using Content.Server.Atmos.EntitySystems;

namespace Content.Server._Starlight.TicketMachine.EntitySystems;

public sealed class TicketMachineSystem : SharedTicketMachineSystem
{
    [Dependency] private readonly SharedPowerReceiverSystem _powerReceiverSystem = default!;
    [Dependency] private readonly FlammableSystem _flammableSystem = default!;

    protected override void OnSignalReceived(EntityUid uid, TicketMachineComponent component, ref SignalReceivedEvent args)
    {
        base.OnSignalReceived(uid, component, ref args);
        if (args.Port == component.BurnPort && _powerReceiverSystem.IsPowered(uid))
        {
            foreach (var ticket in component.issuedTickets)
                _flammableSystem.Ignite(ticket, uid);
            component.issuedTickets.Clear();
            Dirty(uid, component);
        }
    }
}