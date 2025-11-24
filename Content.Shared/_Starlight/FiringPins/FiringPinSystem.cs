using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Tools.Components;
using Robust.Shared.Containers;
using Content.Shared.Tools.Systems;
using Robust.Shared.Audio.Systems;
using System.Linq;
using Content.Shared.Hands.EntitySystems;

namespace Content.Shared._Starlight.FiringPins;

public sealed partial class FiringPinSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedToolSystem _tool = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FiringPinHolderComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<FiringPinHolderComponent, InteractUsingEvent>(OnInteractUsing);

        SubscribeLocalEvent<FiringPinHolderComponent, FiringPinRemovalFinishEvent>(OnPinRemovalFinish);
    }

    private void OnStartup(Entity<FiringPinHolderComponent> ent, ref ComponentStartup args)
    {
        ent.Comp.PinContainer = _container.EnsureContainer<Container>(ent.Owner, FiringPinHolderComponent.PinContainerName);
    }

    private void OnInteractUsing(Entity<FiringPinHolderComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled) return;

        // check insert attempt
        if (TryComp<FiringPinComponent>(args.Used, out var pinComp))
        {
            args.Handled = true;

            if (ent.Comp.PinContainer.ContainedEntities.Count > 0)
            {
                _popup.PopupClient(Loc.GetString("firing-pin-already-there"), ent.Owner, args.User);
                return;
            }

            if (!ent.Comp.SupportedPins.Contains(pinComp.PinType))
            {
                _popup.PopupClient(Loc.GetString("firing-pin-no-fit"), ent.Owner, args.User);
                return;
            }

            if (_container.Insert(args.Used, ent.Comp.PinContainer))
            {
                // todo: give this a delay?
                _popup.PopupClient(Loc.GetString("firing-pin-inserted"), ent.Owner, args.User);
                _audio.PlayPredicted(ent.Comp.PinInsertionSound, ent.Owner, args.User);
                return;
            }
        } else if (TryComp<ToolComponent>(args.Used, out var toolComp) &&
                   _tool.HasQuality(args.Used, ent.Comp.PinExtractionMethod))
        {
            args.Handled = true;

            if(ent.Comp.PinContainer.ContainedEntities.Count == 0)
            {
                _popup.PopupClient(Loc.GetString("firing-pin-holder-empty"), ent.Owner, args.User);
                return;
            }

            _tool.UseTool(args.Used, args.User, ent.Owner, 1.5f, ent.Comp.PinExtractionMethod, new FiringPinRemovalFinishEvent(), toolComponent: toolComp);
        }
    }

    private void OnPinRemovalFinish(Entity<FiringPinHolderComponent> ent, ref FiringPinRemovalFinishEvent args)
    {
        if (args.Cancelled) return;

        var firingPins = ent.Comp.PinContainer.ContainedEntities.ToArray();
        _container.EmptyContainer(ent.Comp.PinContainer, reparent: false);
        // sandbox gets mad at me when I .First(), so just going to loop through all
        foreach (var pin in firingPins)
        {
            _hands.PickupOrDrop(args.User, pin, dropNear: true);
        }
        _popup.PopupClient(Loc.GetString("firing-pin-removed"), ent.Owner, args.User);
        _audio.PlayPredicted(ent.Comp.PinExtractionSound, ent.Owner, args.User);
    }
}