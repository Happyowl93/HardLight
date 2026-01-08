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
        SubscribeLocalEvent<SlimeGenderChangePotionComponent, AfterInteractEvent>(OnAfterInteract);
    }
    
    private void OnAfterInteract(Entity<SlimeGenderChangePotionComponent> ent, ref AfterInteractEvent args)
    {
        if (!args.Target.HasValue || !args.CanReach) return;
        if (!_entityManager.TryGetComponent<HumanoidAppearanceComponent>(args.Target.Value,
                out var humanoidAppearanceComponent)) return;
        // Because there are 4 gender options, this potion will simply cycle through each one
        // It would be better to have a dropdown list of genders, but this works for now
        var gender = humanoidAppearanceComponent.Gender;
        switch (gender)
        {
            case Gender.Neuter:
                _sharedHumanoidAppearanceSystem.SetGender((args.Target.Value, humanoidAppearanceComponent), Gender.Epicene);
                break;
            case Gender.Epicene:
                _sharedHumanoidAppearanceSystem.SetGender((args.Target.Value, humanoidAppearanceComponent), Gender.Female);
                break;
            case Gender.Female:
                _sharedHumanoidAppearanceSystem.SetGender((args.Target.Value, humanoidAppearanceComponent), Gender.Male);
                break;
            case Gender.Male:
                _sharedHumanoidAppearanceSystem.SetGender((args.Target.Value, humanoidAppearanceComponent), Gender.Neuter);
                break;
        }
        PredictedQueueDel(args.Used);
        args.Handled = true;
    }
}