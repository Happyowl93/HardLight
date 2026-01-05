using Content.Shared.Interaction;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.NameModifier.Components;
using Content.Shared.NameModifier.EntitySystems;
using Robust.Shared.Serialization;

namespace Content.Shared._Starlight.Xenobiology.Potions;

public sealed class SlimeMindContainerComponentPotionSystem : EntitySystem
{
    [Dependency] private readonly EntityManager _entityManager = default!;
    [Dependency] private readonly SharedMindSystem _sharedMindSystem = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly MetaDataSystem _metaDataSystem = default!;
    
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MindContainerComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<SlimeNameChangePotionComponent, SlimeNameChangePotionNewNameChangedMessage>(OnSlimePotionNameChanged);
    }
    
    private void OnInteractUsing(Entity<MindContainerComponent> ent, ref InteractUsingEvent args)
    {
        if (_entityManager.TryGetComponent<SlimeSentiencePotionComponent>(args.Used, out _))
        {
            _sharedMindSystem.MakeSentient(ent.Owner); // I hope this creates the associated ghost role because otherwise I've got nothing.
            PredictedQueueDel(args.Used);
            args.Handled = true;
        }
        else if (_entityManager.TryGetComponent<SlimeNameChangePotionComponent>(args.Used, out var slimeNameChangePotionComponent))
        {
            _metaDataSystem.SetEntityName(args.Target, slimeNameChangePotionComponent.AssignedName);
            PredictedQueueDel(args.Used);
            args.Handled = true;
        }
    }

    private void OnSlimePotionNameChanged(EntityUid uid, SlimeNameChangePotionComponent slimeSentiencePotionComponent, SlimeNameChangePotionNewNameChangedMessage args)
    {
        slimeSentiencePotionComponent.AssignedName = args.NewName;
        Dirty(uid, slimeSentiencePotionComponent);
    }
}

[Serializable, NetSerializable]
public enum SlimeNameChangePotionUiKey
{
    Key,
}

[Serializable, NetSerializable]
public sealed class SlimeNameChangePotionNewNameChangedMessage(string newName) : BoundUserInterfaceMessage
{
    public string NewName { get; } = newName;
}
