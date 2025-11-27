using Content.Shared.Inventory;
using Content.Shared.Body.Components;
using Content.Shared.Movement.Components;

namespace Content.Shared.Movement;

public sealed class MovementHinderedByClothingSystem : EntitySystem
{
    [Dependency] private readonly InventorySystem _inventory = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BodyComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshSpeed);
    }

    private void OnRefreshSpeed(EntityUid uid, BodyComponent body, ref RefreshMovementSpeedModifiersEvent args)
    {
        // shoes check
        if (!_inventory.TryGetSlotEntity(uid, "shoes", out var _))
            return;
     
        float hinderModifier = 0f;

        foreach (var legEntity in body.LegEntities)
        {
            if (!TryComp<MovementBodyPartHinderedByClothingComponent>(legEntity, out var legModifier))
                continue;

            hinderModifier += legModifier.HinderModifier;
        }

        if (hinderModifier > 0f)
        {
            args.ModifySpeed(1, 1 - hinderModifier);
        }
    }
}