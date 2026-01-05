using Content.Shared.Damage.Components;
using Content.Shared.Interaction;
using Robust.Shared.Prototypes;

namespace Content.Shared._Starlight.Xenobiology.Potions;

public sealed class SlimeFireproofPotionSystem : EntitySystem
{
    [Dependency] private readonly EntityManager _entityManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DamageableComponent, InteractUsingEvent>(OnInteractUsing);
    }

    private void OnInteractUsing(Entity<DamageableComponent> ent, ref InteractUsingEvent args)
    {
        if (!_entityManager.TryGetComponent<SlimeFireproofPotionComponent>(args.Used,
                out var slimeFireproofPotionComponent)) return;
        if (!_prototypeManager.Resolve(slimeFireproofPotionComponent.FireproofDamageSet, out var modifier))
            return;
        var damageProtectionBuffComponent = _entityManager.EnsureComponent<DamageProtectionBuffComponent>(ent);
        damageProtectionBuffComponent.Modifiers.Add("SlimeFireproofPotionEffect", modifier);
        slimeFireproofPotionComponent.RemainingUses -= 1;
        if (slimeFireproofPotionComponent.RemainingUses <= 0)
            PredictedQueueDel(args.Used);
        args.Handled = true;
    }
}