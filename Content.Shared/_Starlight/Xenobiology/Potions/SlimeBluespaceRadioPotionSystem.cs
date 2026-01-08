using Content.Shared._Starlight.Language.Components;
using Content.Shared.Interaction;
using Content.Shared.Radio.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared._Starlight.Xenobiology.Potions;

public sealed class SlimeBluespaceRadioPotionSystem : EntitySystem
{
    [Dependency] private readonly EntityManager _entityManager = default!;
    
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SlimeBluespaceRadioPotionComponent, AfterInteractEvent>(OnAfterInteract);
    }

    private void OnAfterInteract(Entity<SlimeBluespaceRadioPotionComponent> ent, ref AfterInteractEvent args)
    {
        if (!args.Target.HasValue || !args.CanReach) return;
        if (!_entityManager.TryGetComponent<LanguageSpeakerComponent>(args.Target.Value, out _)) return;
        var activeRadioComponent = _entityManager.AddComponent<ActiveRadioComponent>(args.Target.Value);
        activeRadioComponent.Channels = ent.Comp.Channels;
        var intrinsicRadioTransmitterComponent = _entityManager.AddComponent<IntrinsicRadioTransmitterComponent>(args.Target.Value);
        intrinsicRadioTransmitterComponent.Channels = ent.Comp.Channels;
        _entityManager.AddComponent<IntrinsicRadioReceiverComponent>(args.Target.Value);
        
        PredictedQueueDel(args.Used);
        args.Handled = true;
    }
}