using Content.Server.PowerCell;
using Content.Shared._Starlight.UI;
using Content.Shared.Alert;
using Content.Shared.PowerCell;
using Content.Shared.PowerCell.Components;

namespace Content.Server._Starlight.Alert;

public sealed partial class BatteryAlertSystem : EntitySystem
{
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly PowerCellSystem _powerCell = default!;
    
    public override void Initialize()
    {
        SubscribeLocalEvent<BatteryAlertComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<BatteryAlertComponent, PowerCellChangedEvent>(OnPowerCellChanged);
    }
    
    private void OnMapInit(EntityUid uid, BatteryAlertComponent component, MapInitEvent args) => TryUpdateBatteryAlert(uid, component);
    
    private void OnPowerCellChanged(EntityUid uid, BatteryAlertComponent component, PowerCellChangedEvent args) => TryUpdateBatteryAlert(uid, component);
    
    public bool TryUpdateBatteryAlert(EntityUid uid, BatteryAlertComponent? = null comp, PowerCellSlotComponent? slotComponent = null)
    {
        if (!Resolve(uid, ref comp))
            return false;
        
        if (!_powerCell.TryGetBatteryFromSlot(uid, out var battery, slotComponent))
        {
            _alerts.ClearAlert(uid, comp.BatteryAlert);
            _alerts.ShowAlert(uid, comp.NoBatteryAlert);
            return true;
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
        return true;
    }
}