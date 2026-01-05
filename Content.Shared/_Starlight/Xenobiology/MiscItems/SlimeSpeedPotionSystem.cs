using Content.Shared.Clothing;
using Content.Shared.Interaction;
using Robust.Shared.GameStates;

namespace Content.Shared._Starlight.Xenobiology.MiscItems;

public sealed class SlimeSpeedPotionSystem : EntitySystem
{
    [Dependency] private readonly EntityManager _entityManager = default!;
    [Dependency] private readonly ClothingSpeedModifierSystem _clothingSpeedModifierSystem = default!;
    
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ClothingSpeedModifierComponent, InteractUsingEvent>(OnInteractUsing);
    }
    
    private void OnInteractUsing(Entity<ClothingSpeedModifierComponent> ent, ref InteractUsingEvent args)
    {
        if (!_entityManager.TryGetComponent<SlimeSpeedPotionComponent>(args.Used,
                out _)) return;
        _clothingSpeedModifierSystem.SetWalkSpeedModifier(ent.Comp, (ent.Comp.WalkModifier + 1.0F) / 2.0F);
        _clothingSpeedModifierSystem.SetSprintSpeedModifier(ent.Comp, (ent.Comp.SprintModifier + 1.0F) / 2.0F);
        PredictedQueueDel(args.Used);
        args.Handled = true;
    }
}