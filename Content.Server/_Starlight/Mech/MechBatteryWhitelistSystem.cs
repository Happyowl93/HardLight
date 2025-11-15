using Content.Server.Power.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Interaction;
using Content.Shared.Mech.Components;
using Content.Shared.Popups;
using Content.Shared.Wires;
using Robust.Shared.Localization;

namespace Content.Server._Starlight.Mech;

/// <summary>
/// Starlight: Validates battery insertions for mechs with ItemSlots whitelists
/// Intercepts battery insertion before MechSystem to check against whitelist tags
/// </summary>
public sealed class MechBatteryWhitelistSystem : EntitySystem
{
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();
        // Subscribe to general InteractUsingEvent before MechSystem processes it
        SubscribeLocalEvent<InteractUsingEvent>(OnInteractUsing, before: new[] { typeof(Server.Mech.Systems.MechSystem) });
    }

    private void OnInteractUsing(InteractUsingEvent args)
    {
        // Only process if target has a MechComponent
        if (!TryComp<MechComponent>(args.Target, out var component))
            return;

        // Only handle battery insertions when the wires panel is open
        if (TryComp<WiresPanelComponent>(args.Target, out var panel) && !panel.Open)
            return;

        // Check if this is a battery insertion attempt
        if (component.BatterySlot.ContainedEntity != null || !HasComp<BatteryComponent>(args.Used))
            return;

        // Check ItemSlots whitelist if it exists on this mech
        if (TryComp<ItemSlotsComponent>(args.Target, out var itemSlots) &&
            _itemSlots.TryGetSlot(args.Target, "mech-battery-slot", out var slot, itemSlots))
        {
            // Validate the battery against the whitelist
            if (!_itemSlots.CanInsert(args.Target, args.Used, args.User, slot))
            {
                // Show the custom failure message if configured
                if (slot.WhitelistFailPopup != null)
                    _popup.PopupEntity(Loc.GetString(slot.WhitelistFailPopup), args.Target, args.User);
                
                // Cancel the interaction to prevent insertion
                args.Handled = true;
                return;
            }
        }
    }
}
