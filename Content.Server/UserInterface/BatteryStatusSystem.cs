using Content.Server.PowerCell;
using Content.Shared._Starlight.UI;
using Content.Shared.Alert;
using Content.Shared.PowerCell.Components;

namespace Content.Server._Starlight.UI;

public sealed partial class BatteryStatusSystem : EntitySystem
{
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly PowerCellSystem _powerCell = default!;
    public void UpdateBatteryAlert(EntityUid uid, PowerCellSlotComponent? slotComponent = null)
    {
        if (TryComp<BatteryStatusComponent>(uid, out var comp))
        {
            if (!_powerCell.TryGetBatteryFromSlot(uid, out var battery, slotComponent))
            {
                _alerts.ClearAlert(uid, comp.BatteryAlert);
                _alerts.ShowAlert(uid, comp.NoBatteryAlert);
                return;
            }

            var chargePercent = (short)MathF.Round(battery.CurrentCharge / battery.MaxCharge * 10f);

            // we make sure 0 only shows if they have absolutely no battery.
            // also account for floating point imprecision
            if (chargePercent == 0 && _powerCell.HasDrawCharge(uid, cell: slotComponent))
            {
                chargePercent = 1;
            }

            _alerts.ClearAlert(uid, comp.NoBatteryAlert);
            _alerts.ShowAlert(uid, comp.BatteryAlert, chargePercent);
        }
    }
}