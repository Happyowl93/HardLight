using Content.Shared._Starlight.Xenobiology.MiscItems;
using Content.Shared.Clothing.Components;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Interaction;
using Robust.Shared.Prototypes;

namespace Content.Server._Starlight.Xenobiology.MiscItems;

public sealed class SlimeFireproofPotionSystem : EntitySystem
{
    [Dependency] private readonly EntityManager _entityManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    private readonly ProtoId<DamageModifierSetPrototype> fireproofModifierSet = "SlimeFireproofPotionEffect";
    
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DamageableComponent, InteractUsingEvent>(OnInteractUsing);
    }

    private void OnInteractUsing(Entity<DamageableComponent> ent, ref InteractUsingEvent args)
    {
        if (!_entityManager.TryGetComponent<SlimeFireproofPotionComponent>(args.Used,
                out var slimeFireproofPotionComponent)) return;
        if (!_prototypeManager.Resolve(fireproofModifierSet, out var modifier))
            return;
        var damageProtectionBuffComponent = _entityManager.EnsureComponent<DamageProtectionBuffComponent>(ent);
        damageProtectionBuffComponent.Modifiers.Add("SlimeFireproofPotionEffect", modifier);
        slimeFireproofPotionComponent.RemainingUses -= 1;
        if (slimeFireproofPotionComponent.RemainingUses <= 0)
            QueueDel(args.Used);
        args.Handled = true;
    }
}