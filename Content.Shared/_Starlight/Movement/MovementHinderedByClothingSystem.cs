using Content.Shared.Inventory;
using Content.Shared.Movement.Systems;
using Content.Shared.Trigger.Systems;

namespace Content.Shared.Movement;

public sealed class MovementHinderedByClothingSystem : EntitySystem
{
    [Dependency] private readonly InventorySystem _invetory = default;
    [Dependency] private readonly BodySystem _body = default;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeed = default;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MovementBodyPartHinderedByClothingComponent, GotEquippedEvent>(OnGotEquipped);
        SubscribeLocalEvent<MovementBodyPartHinderedByClothingComponent, GotUnequippedEvent>(OnGotUnequipped);
    }

    private void OnGotEquipped(EntityUid uid, Entity<MovementBodyPartHinderedByClothingComponent> ent, ref GotEquippedEvent args)
    {
        // skip if equipped item wasn't shoes
        if (!_invetory.TryGetSlotEntity(uid, "shoes", out var shoes))
            return;
    }

    private void OnGotUnequipped(EntityUid uid, Entity<MovementBodyPartHinderedByClothingComponent> ent, ref GotUnequippedEvent args)
    {
        // skip if equipped item wasn't shoes
        if (!_invetory.TryGetSlotEntity(uid, "shoes", out var shoes))
            return;
    }
}