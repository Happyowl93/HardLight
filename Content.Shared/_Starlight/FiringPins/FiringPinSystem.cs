using Content.Shared.Interaction;
using Content.Shared.Radio.Components;
using Robust.Shared.Containers;

namespace Content.Shared._Starlight.FiringPins;

public sealed partial class FiringPinSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FiringPinHolderComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<FiringPinHolderComponent, InteractUsingEvent>(OnInteractUsing);
    }

    private void OnStartup(Entity<FiringPinHolderComponent> ent, ref ComponentStartup args)
    {
        ent.Comp.PinContainer = _container.EnsureContainer<Container>(ent.Owner, FiringPinHolderComponent.PinContainerName);
    }

    private void OnInteractUsing(Entity<FiringPinHolderComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled) return;

        // check insert attempt
        // check screwdriver attempt
    }
}