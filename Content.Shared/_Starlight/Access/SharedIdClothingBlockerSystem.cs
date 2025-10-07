using Content.Shared.Clothing.Components;
using Content.Shared.DoAfter;
using Content.Shared.Inventory.Events;

namespace Content.Shared._Starlight.Access;

public abstract class SharedIdClothingBlockerSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<Access.IdClothingBlockerComponent, GotEquippedEvent>(OnGotEquipped);
        SubscribeLocalEvent<Access.IdClothingBlockerComponent, GotUnequippedEvent>(OnGotUnequipped);
        SubscribeLocalEvent<Access.IdClothingBlockerComponent, BeingUnequippedAttemptEvent>(OnUnequipAttempt);
        SubscribeLocalEvent<Access.IdClothingBlockerComponent, DoAfterAttemptEvent<ClothingUnequipDoAfterEvent>>(OnUnequipDoAfterAttempt);
    }

    protected virtual void OnUnequipAttempt(EntityUid uid, Access.IdClothingBlockerComponent component,
        BeingUnequippedAttemptEvent args)
    {
        var wearerHasAccess = HasJobAccess(args.Unequipee, component);
        if (wearerHasAccess)
            return;

        if (args.UnEquipTarget == args.Unequipee)
        {
            args.Cancel();
        }
    }

    protected virtual void OnUnequipDoAfterAttempt(EntityUid uid, Access.IdClothingBlockerComponent component,
        DoAfterAttemptEvent<ClothingUnequipDoAfterEvent> args)
    {
        if (args.DoAfter.Args.Target == null)
            return;

        var wearerHasAccess = HasJobAccess(args.DoAfter.Args.Target.Value, component);

        if (wearerHasAccess)
            return;

        args.Cancel();
        PopupClient(Loc.GetString("access-clothing-blocker-notify-unauthorized-access"), uid);
    }
    
    protected virtual bool HasJobAccess(EntityUid wearer, Access.IdClothingBlockerComponent component)
    {
        return !component.IsBlocked;
    }

    private void OnGotEquipped(EntityUid uid, Access.IdClothingBlockerComponent component, GotEquippedEvent args)
    {
        var wearerHasAccess = HasJobAccess(args.Equipee, component);

        if (wearerHasAccess)
            return;

        OnUnauthorizedAccess(uid, component, args.Equipee);
    }

    protected virtual void OnUnauthorizedAccess(EntityUid clothingUid, Access.IdClothingBlockerComponent component,
        EntityUid wearer)
    {
    }

    private void OnGotUnequipped(EntityUid uid, Access.IdClothingBlockerComponent component, GotUnequippedEvent args)
    {
        if (EntityManager.EntityExists(args.Equipee) &&
            EntityManager.HasComponent<IdClothingFrozenComponent>(args.Equipee))
        {
            EntityManager.RemoveComponent<IdClothingFrozenComponent>(args.Equipee);
        }
    }

    protected virtual void PopupClient(string message, EntityUid uid, EntityUid? target = null)
    {
    }
}