using Content.Shared._Starlight.Plumbing;
using Content.Shared.Chemistry;
using Content.Shared.Containers.ItemSlots;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using static Content.Shared.Chemistry.SharedReagentDispenser;

namespace Content.Client._Starlight.Plumbing.UI;

[UsedImplicitly]
public sealed class PlumbingSmartDispenserBoundUserInterface : BoundUserInterface
{
    private PlumbingSmartDispenserWindow? _window;

    public PlumbingSmartDispenserBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindow<PlumbingSmartDispenserWindow>();

        _window.AmountGrid.OnButtonPressed += value => SendMessage(new PlumbingSmartDispenserSetDispenseAmountMessage(value));
        _window.ClearButton.OnPressed += _ => SendMessage(new ReagentDispenserClearContainerSolutionMessage());
        _window.EjectButton.OnPressed += _ => SendMessage(new ItemSlotButtonPressedEvent(OutputSlotName, tryInsert: false));
        _window.OnDispenseReagentPressed += reagentId => SendMessage(new PlumbingSmartDispenserDispenseReagentMessage(reagentId));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (_window == null || state is not PlumbingSmartDispenserBuiState cast)
            return;

        _window.UpdateState(cast);
    }
}
