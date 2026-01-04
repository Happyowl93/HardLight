using Content.Shared._Starlight.Language.Components;
using Content.Shared.Interaction;
using Content.Shared.Radio.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared._Starlight.Xenobiology.MiscItems;

public sealed class SlimeBluespaceRadioPotionSystem : EntitySystem
{
    [Dependency] private readonly EntityManager _entityManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<LanguageSpeakerComponent, InteractUsingEvent>(OnInteractUsing);
    }

    private void OnInteractUsing(Entity<LanguageSpeakerComponent> ent, ref InteractUsingEvent args)
    {
        if (!_entityManager.TryGetComponent<SlimeBluespaceRadioPotionComponent>(args.Used,
                out var slimeBluespaceRadioPotionComponent)) return;
        var activeRadioComponent = _entityManager.AddComponent<ActiveRadioComponent>(args.Target);
        activeRadioComponent.Channels = slimeBluespaceRadioPotionComponent.Channels;
        var intrinsicRadioTransmitterComponent = _entityManager.AddComponent<IntrinsicRadioTransmitterComponent>(args.Target);
        intrinsicRadioTransmitterComponent.Channels = slimeBluespaceRadioPotionComponent.Channels;
        _entityManager.AddComponent<IntrinsicRadioReceiverComponent>(args.Target);
        
        PredictedQueueDel(args.Used);
        args.Handled = true;
    }
}