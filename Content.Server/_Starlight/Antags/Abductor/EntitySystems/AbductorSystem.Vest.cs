using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Clothing.Components;
using Content.Shared.Starlight.Antags.Abductor;
using Content.Shared.Stealth.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.Interaction;
using Content.Shared.Toggleable;

namespace Content.Server.Starlight.Antags.Abductor;

public sealed partial class AbductorSystem : SharedAbductorSystem
{
    [Dependency] private readonly ClothingSystem _clothing = default!;
    public void InitializeVest()
    {
        SubscribeLocalEvent<AbductorVestComponent, AfterInteractEvent>(OnVestInteract);
        SubscribeLocalEvent<AbductorVestComponent, ItemSwitchedEvent>(OnItemSwitch);
        SubscribeLocalEvent<AbductorVestComponent, ToggleActionEvent>(OnToggle);
    }

    private void OnToggle(Entity<AbductorVestComponent> ent, ref ToggleActionEvent args)
    {
        if (ent.Comp.CurrentState == AbductorArmorModeType.Combat)
            _popup.PopupEntity(Loc.GetString("need-switch-mode"), ent.Owner, args.Performer, PopupType.MediumCaution);
    }
    private void OnItemSwitch(EntityUid uid, AbductorVestComponent component, ref ItemSwitchedEvent args)
    {
        if (Enum.TryParse<AbductorArmorModeType>(args.State, ignoreCase: true, out var state))
            component.CurrentState = state;

        var user = Transform(uid).ParentUid;

        if (state == AbductorArmorModeType.Combat)
        {
            if (TryComp<ClothingComponent>(uid, out var clothingComponent))
                _clothing.SetEquippedPrefix(uid, "combat", clothingComponent);

            RemComp<StealthComponent>(user);
            RemComp<StealthOnMoveComponent>(user);
        }
    }

    private void OnVestInteract(Entity<AbductorVestComponent> ent, ref AfterInteractEvent args)
    {
        if (!_actionBlockerSystem.CanInstrumentInteract(args.User, args.Used, args.Target))
            return;

        if (!args.Target.HasValue)
            return;

        if (!TryComp<AbductorConsoleComponent>(args.Target, out var console))
            return;

        var netEntity = GetNetEntity(ent);
        console.Armor = netEntity;

        _popup.PopupEntity(Loc.GetString("abductors-ui-vest-linked"), args.User);
        UpdateGui(netEntity, (args.Target.Value, console));
    }
}
