using Content.Server.Popups;
using Content.Shared.Storage.Components;
using Content.Shared.ActionBlocker;
using Content.Shared.DoAfter;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.Movement.Events;
using Content.Shared.Resist;
using Content.Shared.Storage;
using Content.Shared.Tag; // Starlight Edit
using Robust.Server.GameObjects; // Starlight Edit
using Robust.Shared.Containers;

namespace Content.Server.Resist;

public sealed class EscapeInventorySystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly ActionBlockerSystem _actionBlockerSystem = default!;
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
    [Dependency] private readonly TagSystem _tagSystem = default!; // Starlight Edit
    [Dependency] private readonly TransformSystem _transformSystem = default!; // Starlight Edit

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CanEscapeInventoryComponent, MoveInputEvent>(OnRelayMovement);
        SubscribeLocalEvent<CanEscapeInventoryComponent, EscapeInventoryEvent>(OnEscape);
        SubscribeLocalEvent<CanEscapeInventoryComponent, DroppedEvent>(OnDropped);
    }

    private void OnRelayMovement(EntityUid uid, CanEscapeInventoryComponent component, ref MoveInputEvent args)
    {
        if (!args.HasDirectionalMovement)
            return;

        if (!_containerSystem.TryGetContainingContainer((uid, null, null), out var container) || !_actionBlockerSystem.CanInteract(uid, container.Owner))
            return;

        // Make sure there's nothing stopped the removal (like being glued)
        if (!_containerSystem.CanRemove(uid, container))
        {
            _popupSystem.PopupEntity(Loc.GetString("escape-inventory-component-failed-resisting"), uid, uid);
            return;
        }

        // Contested
        if (_handsSystem.IsHolding(container.Owner, uid, out _))
        {
            AttemptEscape(uid, container.Owner, component);
            return;
        }

        // Uncontested
        if (HasComp<StorageComponent>(container.Owner) || HasComp<InventoryComponent>(container.Owner) || HasComp<SecretStashComponent>(container.Owner))
        // Starlight edit start - Add another escapable container
        {
            AttemptEscape(uid, container.Owner, component);
            return;
        }
        
        // Uncontested - Escape from borg modules and such
        if (_tagSystem.HasTag(container.Owner, "PersonnelStorage"))
        {
            AttemptEscape(uid, container.Owner, component);
        }
        // Starlight edit end
    }

    private void AttemptEscape(EntityUid user, EntityUid container, CanEscapeInventoryComponent component, float multiplier = 1f)
    {
        if (component.IsEscaping)
            return;

        var doAfterEventArgs = new DoAfterArgs(EntityManager, user, component.BaseResistTime * multiplier, new EscapeInventoryEvent(), user, target: container)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = false
        };

        if (!_doAfterSystem.TryStartDoAfter(doAfterEventArgs, out component.DoAfter))
            return;

        _popupSystem.PopupEntity(Loc.GetString("escape-inventory-component-start-resisting"), user, user);
        _popupSystem.PopupEntity(Loc.GetString("escape-inventory-component-start-resisting-target"), container, container);
    }

    private void OnEscape(EntityUid uid, CanEscapeInventoryComponent component, EscapeInventoryEvent args)
    {
        component.DoAfter = null;

        if (args.Handled || args.Cancelled)
            return;

        // Starlight edit start - Special handling for borg modules
        if (_containerSystem.TryGetContainingContainer((uid, null, null), out var container) &&
            _tagSystem.HasTag(container.Owner, "PersonnelStorage"))
        {
            EscapeFromPersonnelStorage(uid);
        }
        else
        {
            _containerSystem.AttachParentToContainerOrGrid((uid, Transform(uid)));
        }
        // Starlight edit end
        args.Handled = true;
    }
    // Starlight edit start - Special handling for borg modules
    /// <summary>
    /// Special handling for borg modules, it is required because in some cases borgs and their modules are two separate containers,
    /// we recursively remove them from those containers until we hit a container that is not an escape source.
    /// This is kinda needed to handle a case where a borg would be inside of a locker and someone tried to escape for it, to make it so they stay inside of the locker.
    /// </summary>
    private void EscapeFromPersonnelStorage(EntityUid uid)
    {
        while (true)
        {
            if (!_containerSystem.TryGetContainingContainer((uid, null, null), out var container))
            {
                var transform = Transform(uid);
                _transformSystem.AttachToGridOrMap(uid, transform);
                return;
            }
            
            bool isEscapeTarget = _handsSystem.IsHolding(container.Owner, uid, out _) ||
                                  HasComp<StorageComponent>(container.Owner) ||
                                  HasComp<InventoryComponent>(container.Owner) ||
                                  HasComp<SecretStashComponent>(container.Owner) ||
                                  _tagSystem.HasTag(container.Owner, "PersonnelStorage");
            
            if (isEscapeTarget)
            {
                _containerSystem.Remove(uid, container, force: true);
                continue;
            }
            
            _containerSystem.Insert(uid, container);
            return;
        }
    }
    // Starlight edit end

    private void OnDropped(EntityUid uid, CanEscapeInventoryComponent component, DroppedEvent args)
    {
        if (component.DoAfter != null)
            _doAfterSystem.Cancel(component.DoAfter);
    }
}
