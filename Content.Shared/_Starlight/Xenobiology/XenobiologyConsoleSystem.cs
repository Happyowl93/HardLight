using System.Diagnostics.CodeAnalysis;
using Content.Shared._Starlight.Computers.RemoteEye;
using Content.Shared._Starlight.Xenobiology.MiscItems;
using Content.Shared._Starlight.Xenobiology.Potions;
using Content.Shared.Actions;
using Content.Shared.Damage.Components;
using Content.Shared.Interaction;
using Content.Shared.Tag;
using Robust.Shared.Containers;

namespace Content.Shared._Starlight.Xenobiology;

public sealed class XenobiologyConsoleSystem : EntitySystem
{
    [Dependency] private readonly EntityManager _entityManager = default!;
    [Dependency] private readonly TagSystem _tagSystem = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookupSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<XenobiologyConsoleComponent, AfterInteractUsingEvent>(OnAfterInteractUsing);
        
        SubscribeLocalEvent<ConsoleGrabSlimeEvent>(OnConsoleGrabSlime);
        SubscribeLocalEvent<ConsolePlaceSlimeEvent>(OnConsolePlaceSlime);
        SubscribeLocalEvent<ConsolePlaceMonkeyEvent>(OnConsolePlaceMonkey);
        SubscribeLocalEvent<ConsoleRecycleMonkeyCorpseEvent>(OnConsoleRecycleMonkeyCorpse);
        SubscribeLocalEvent<ConsoleApplyMutationPotionEvent>(OnConsoleApplyMutationPotion);
        SubscribeLocalEvent<ConsoleApplyStabilizerPotionEvent>(OnConsoleApplyStabilizerPotion);
        SubscribeLocalEvent<ConsoleGetSlimeInfoEvent>(OnConsoleGetSlimeInfo);
    }

    private void OnAfterInteractUsing(Entity<XenobiologyConsoleComponent> entity, ref AfterInteractUsingEvent args)
    {
        if (_tagSystem.HasTag(args.Used, entity.Comp.MonkeyCubeTag))
        {
            entity.Comp.MonkeyCubes += 1;
            PredictedQueueDel(args.Used);
            args.Handled = true;
            return;
        }

        if (_entityManager.HasComponent<SlimeMutationPotionComponent>(args.Used))
        {
            entity.Comp.MutationPotions += 1;
            PredictedQueueDel(args.Used);
            args.Handled = true;
            return;
        }
        
        if (_entityManager.HasComponent<SlimeStabilizerPotionComponent>(args.Used))
        {
            entity.Comp.StabilizerPotions += 1;
            PredictedQueueDel(args.Used);
            args.Handled = true;
            return;
        }
    }
    
    private bool VerifyComponents(InstantActionEvent args, [NotNullWhen(true)] out RemoteEyeActorComponent? remoteEyeActorComponent,
        [NotNullWhen(true)] out XenobiologyConsoleComponent? xenobiologyConsoleComponent, [NotNullWhen(true)] out EntityUid? remoteEntity)
    {
        remoteEyeActorComponent = null;
        xenobiologyConsoleComponent = null;
        remoteEntity = null;
        if (!_entityManager.TryGetComponent<RemoteEyeActorComponent>(args.Performer, out remoteEyeActorComponent))
        {
            return false;
        }
        if (remoteEyeActorComponent.VirtualItem == null)
        {
            return false;
        }
        if (!_entityManager.TryGetComponent<XenobiologyConsoleComponent>(remoteEyeActorComponent.VirtualItem, out xenobiologyConsoleComponent)) return false;
        if (remoteEyeActorComponent.RemoteEntity == null) return false;
        remoteEntity = remoteEyeActorComponent.RemoteEntity;
        return true;
    }

    private void OnConsoleGrabSlime(ConsoleGrabSlimeEvent args)
    {
        if (!VerifyComponents(args, out var remoteEyeActorComponent, out var xenobiologyConsoleComponent, out var remoteEntity)) return;
        xenobiologyConsoleComponent.SlimeContainer = _container.EnsureContainer<ContainerSlot>(remoteEntity.Value, XenobiologyConsoleComponent.SlimeContainerName);
        foreach (var possibleSlime in _entityLookupSystem.GetEntitiesInRange(remoteEntity.Value, 0.5F))
        {
            if (_entityManager.TryGetComponent<SlimeComponent>(possibleSlime, out _))
            {
                _container.Insert(possibleSlime, xenobiologyConsoleComponent.SlimeContainer);
                break;
            }
        }
        args.Handled = true;
    }

    private void OnConsolePlaceSlime(ConsolePlaceSlimeEvent args)
    {
        if (!VerifyComponents(args, out var remoteEyeActorComponent, out var xenobiologyConsoleComponent, out var remoteEntity)) return;
        if (xenobiologyConsoleComponent.SlimeContainer.ContainedEntities.Count <= 0) return;
        var slime = xenobiologyConsoleComponent.SlimeContainer.ContainedEntities[0];
        _container.Remove(slime, xenobiologyConsoleComponent.SlimeContainer, destination: Transform(remoteEntity.Value).Coordinates);
        args.Handled = true;
    }

    private void OnConsolePlaceMonkey(ConsolePlaceMonkeyEvent args)
    {
        if (!VerifyComponents(args, out var remoteEyeActorComponent, out var xenobiologyConsoleComponent, out var remoteEntity)) return;
        if (xenobiologyConsoleComponent.MonkeyCubes < 1) return;
        _entityManager.PredictedSpawnAtPosition(xenobiologyConsoleComponent.MonkeyProtoId, Transform(remoteEntity.Value).Coordinates);
        xenobiologyConsoleComponent.MonkeyCubes -= 1;
        args.Handled = true;
    }

    private void OnConsoleRecycleMonkeyCorpse(ConsoleRecycleMonkeyCorpseEvent args)
    {
        if (!VerifyComponents(args, out var remoteEyeActorComponent, out var xenobiologyConsoleComponent, out var remoteEntity)) return;
        foreach (var possibleMonkey in _entityLookupSystem.GetEntitiesInRange(remoteEntity.Value, 0.5F))
        {
            if (_tagSystem.HasTag(possibleMonkey, xenobiologyConsoleComponent.MonkeyTag))
            {
                if (!_entityManager.TryGetComponent<DamageableComponent>(possibleMonkey, out var damageableComponent))
                    continue;
                if (damageableComponent.TotalDamage < 100) continue; // Slimes don't eat monkeys above 100 damage
                PredictedQueueDel(possibleMonkey);
                xenobiologyConsoleComponent.MonkeyCubes += 0.5D;
                break;
            }
        }
        args.Handled = true;
    }

    private void OnConsoleApplyMutationPotion(ConsoleApplyMutationPotionEvent args)
    {
        if (!VerifyComponents(args, out var remoteEyeActorComponent, out var xenobiologyConsoleComponent, out var remoteEntity)) return;
        foreach (var possibleSlime in _entityLookupSystem.GetEntitiesInRange(remoteEntity.Value, 0.5F))
        {
            if (_entityManager.TryGetComponent<SlimeComponent>(possibleSlime, out var slimeComponent))
            {
                slimeComponent.MutationChance += SlimeMutationPotionComponent.MutationChangeAmount;
                xenobiologyConsoleComponent.MutationPotions -= 1;
                break;
            }
        }
        args.Handled = true;
    }
    
    private void OnConsoleApplyStabilizerPotion(ConsoleApplyStabilizerPotionEvent args)
    {
        if (!VerifyComponents(args, out var remoteEyeActorComponent, out var xenobiologyConsoleComponent, out var remoteEntity)) return;
        foreach (var possibleSlime in _entityLookupSystem.GetEntitiesInRange(remoteEntity.Value, 0.5F))
        {
            if (_entityManager.TryGetComponent<SlimeComponent>(possibleSlime, out var slimeComponent))
            {
                slimeComponent.MutationChance += SlimeStabilizerPotionComponent.MutationChangeAmount;
                xenobiologyConsoleComponent.StabilizerPotions -= 1;
                break;
            }
        }
        args.Handled = true;
    }

    private void OnConsoleGetSlimeInfo(ConsoleGetSlimeInfoEvent args)
    {
        if (!VerifyComponents(args, out var remoteEyeActorComponent, out var xenobiologyConsoleComponent, out var remoteEntity)) return;
        if (!remoteEyeActorComponent.VirtualItem.HasValue) return;
        foreach (var possibleSlime in _entityLookupSystem.GetEntitiesInRange(remoteEntity.Value, 0.5F))
        {
            if (_entityManager.TryGetComponent<SlimeComponent>(possibleSlime, out var slimeComponent))
            {
                var ev = new ConsoleMsgToScannerEvent(args.Performer, possibleSlime);
                RaiseLocalEvent(remoteEyeActorComponent.VirtualItem.Value, ev);
                break;
            }
        }
        args.Handled = true;
    }
}

public sealed partial class ConsoleGetSlimeInfoEvent : InstantActionEvent
{

}

public sealed partial class ConsoleGrabSlimeEvent : InstantActionEvent
{

}

public sealed partial class ConsolePlaceSlimeEvent : InstantActionEvent
{

}

public sealed partial class ConsolePlaceMonkeyEvent : InstantActionEvent
{

}

public sealed partial class ConsoleRecycleMonkeyCorpseEvent : InstantActionEvent
{

}

public sealed partial class ConsoleApplyMutationPotionEvent : InstantActionEvent
{

}

public sealed partial class ConsoleApplyStabilizerPotionEvent : InstantActionEvent
{

}