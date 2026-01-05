using Content.Shared.Humanoid;
using Content.Shared.Interaction;
using Robust.Shared.Enums;

namespace Content.Shared._Starlight.Xenobiology.Potions;

public sealed class SlimeGenderChangePotionSystem : EntitySystem
{
    [Dependency] private readonly EntityManager _entityManager = default!;
    [Dependency] private readonly SharedHumanoidAppearanceSystem _sharedHumanoidAppearanceSystem = default!;
    
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<HumanoidAppearanceComponent, InteractUsingEvent>(OnInteractUsing);
    }
    
    private void OnInteractUsing(Entity<HumanoidAppearanceComponent> ent, ref InteractUsingEvent args)
    {
        // SetGender
        if (!_entityManager.TryGetComponent<SlimeGenderChangePotionComponent>(args.Used,
                out _)) return;
        // Because there are 4 gender options, this potion will simply cycle through each one
        var gender = ent.Comp.Gender;
        if (gender == Gender.Neuter)
            _sharedHumanoidAppearanceSystem.SetGender(ent.AsNullable(), Gender.Epicene);
        else if (gender == Gender.Epicene)
            _sharedHumanoidAppearanceSystem.SetGender(ent.AsNullable(), Gender.Female);
        else if (gender == Gender.Female)
            _sharedHumanoidAppearanceSystem.SetGender(ent.AsNullable(), Gender.Male);
        else if (gender == Gender.Male)
            _sharedHumanoidAppearanceSystem.SetGender(ent.AsNullable(), Gender.Neuter);
        PredictedQueueDel(args.Used);
        args.Handled = true;
    }
}