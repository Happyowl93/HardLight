using Content.Shared._Starlight.TicketMachine.Components;
using Content.Shared.Interaction;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Popups;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Power;
using Robust.Shared.Audio.Systems;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.Examine;
using Robust.Shared.Timing;
using Robust.Shared.Network;

namespace Content.Shared._Starlight.TicketMachine.EntitySystems;

public abstract class SharedTicketMachineSystem : EntitySystem
{
    [Dependency] private readonly AccessReaderSystem _accessReaderSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedPowerReceiverSystem _powerReceiverSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly INetManager _net = default!;

    public override void Initialize()
    {
        base.Initialize();

        // Tickets issue

        SubscribeLocalEvent<TicketMachineComponent, AfterInteractUsingEvent>(OnInteract);
        SubscribeLocalEvent<TicketMachineComponent, InteractHandEvent>(OnHandInteract);

        // Visuals
        SubscribeLocalEvent<TicketMachineComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<TicketMachineComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<TicketComponent, ExaminedEvent>(OnTicketExamined);

        //Device linking
        SubscribeLocalEvent<TicketMachineComponent, SignalReceivedEvent>(OnSignalReceived);

    }

    #region Ticket Issuing

    /// <summary>
    /// Handles interaction with the ticket machine using an id card.
    /// </summary>
    private void OnInteract(EntityUid uid, TicketMachineComponent component, AfterInteractUsingEvent args)
    {
        if (args.Handled || !args.CanReach)
            return;

        if (TryComp<AccessReaderComponent>(uid, out var accessReader) && HasComp<IdCardComponent>(args.Used))
        {
            if (!_accessReaderSystem.IsAllowed(args.Used, uid, accessReader))
            {
                args.Handled = true;
                _audioSystem.PlayPredicted(component.accessDeniedSound, uid, args.User);
                return;
            }
            component.dispenseEnabled = !component.dispenseEnabled;
            _popupSystem.PopupPredicted("Dispense toggled.", args.User, null, PopupType.Medium);
            args.Handled = true;
        }
    }

    /// <summary>
    /// Handles interaction with the ticket machine with empty hand.
    /// </summary>
    private void OnHandInteract(EntityUid uid, TicketMachineComponent component, InteractHandEvent args)
    {
        if (!_gameTiming.IsFirstTimePredicted || args.Handled 
            || component.previousIssueTime + component.issueCooldown > _gameTiming.CurTime 
            || component.lastIssuedNumber >= component.maxTickets || !_powerReceiverSystem.IsPowered(uid))
            return;

        component.previousIssueTime = _gameTiming.CurTime;

        if (!component.dispenseEnabled)
        {
            _popupSystem.PopupPredicted("Ticket dispensing is disabled.", args.User, null, PopupType.Medium);
            args.Handled = true;
            return;
        }
        
        var ticket = EntityManager.PredictedSpawnAtPosition(component.TicketProtoId, Transform(uid).Coordinates);
        args.Handled = true;
        
        if (TryComp<TicketComponent>(ticket, out var ticketComponent))
        {
            component.lastIssuedNumber++;
            ticketComponent.Number = component.lastIssuedNumber;
            component.issuedTickets.Add(ticket);
            _audioSystem.PlayPredicted(component.dispenseSound, uid, args.User);
            _handsSystem.TryPickup(args.User, ticket);
            UpdateVisuals(uid, component);
            UpdateTicketVisuals(ticket, ticketComponent);
        }
        else
            QueueDel(ticket);
    }

    #endregion

    #region Visuals

    /// <summary>
    /// Handles power state changes, for updating visuals.
    /// </summary>
    private void OnPowerChanged(EntityUid uid, TicketMachineComponent component, ref PowerChangedEvent args) => UpdateVisuals(uid, component);

    private void UpdateVisuals(EntityUid uid, TicketMachineComponent component)
    {
        var paperState = CalculatePaperState(component.maxTickets, component.lastIssuedNumber, component.paperStateAmount);

        _appearanceSystem.SetData(uid, TicketMachineVisuals.isPowered, _powerReceiverSystem.IsPowered(uid));
        _appearanceSystem.SetData(uid, TicketMachineVisuals.isFilled, component.hasPaper);
        _appearanceSystem.SetData(uid, TicketMachineVisuals.Paper, paperState);
        _appearanceSystem.SetData(uid, TicketMachineVisuals.DisplayNumber, component.displayNumber);
    }

    private void UpdateTicketVisuals(EntityUid uid, TicketComponent component) => _appearanceSystem.SetData(uid, TicketVisuals.Number, component.Number == null ? 0 : component.Number.Value);

    private int CalculatePaperState(int maxTickets, int lastIssued, int paperStates)
    {
        float percent = (float)(maxTickets - lastIssued) / maxTickets;
        int state = (int)Math.Floor(Math.Log(1f / percent, 2)) + 1;
        return Math.Clamp(state, 1, paperStates);
    }

    private void OnExamined(EntityUid uid, TicketMachineComponent component, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        float percent = (float)(component.maxTickets - component.lastIssuedNumber) / component.maxTickets;
        int percentInt = (int)(percent * 100);
        args.PushMarkup($"<b>Displayed ticket:</b> {component.displayNumber}\n" +
                        $"<b>Paper amount:</b> {(component.hasPaper ? $"{percentInt}%" : "Empty")}\n" +
                        $"<b>Dispensing:</b> {(component.dispenseEnabled ? "Enabled" : "Disabled")}");
    }

    private void OnTicketExamined(EntityUid uid, TicketComponent component, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        args.PushMarkup($"<b>Ticket Number:</b> {component.Number}");
    }

    #endregion

    #region Device Linking
    protected virtual void OnSignalReceived(EntityUid uid, TicketMachineComponent component, ref SignalReceivedEvent args)
    {
        if (_gameTiming.IsFirstTimePredicted && args.Port == component.NextNumberPort && _powerReceiverSystem.IsPowered(uid) 
            && component.displayNumber < component.lastIssuedNumber) // You can't go higher than the number of issued tickets
        {
            component.displayNumber++;
            UpdateVisuals(uid, component);
        }
    }
    #endregion
}