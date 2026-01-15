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
            RaiseLocalEvent(new ConsoleTextMsgEvent(args.User, $"Thanks for inserting a monkey cube! The console now has {entity.Comp.MonkeyCubes} cube(s)."));
            args.Handled = true;
            return;
        }

        if (_entityManager.HasComponent<SlimeMutationPotionComponent>(args.Used))
        {
            entity.Comp.MutationPotions += 1;
            PredictedQueueDel(args.Used);
            RaiseLocalEvent(new ConsoleTextMsgEvent(args.User, $"Thanks for inserting a mutation potion! The console now has {entity.Comp.MutationPotions} mutation potions(s)."));
            args.Handled = true;
            return;
        }
        
        if (_entityManager.HasComponent<SlimeStabilizerPotionComponent>(args.Used))
        {
            entity.Comp.StabilizerPotions += 1;
            PredictedQueueDel(args.Used);
            RaiseLocalEvent(new ConsoleTextMsgEvent(args.User, $"Thanks for inserting a stabilizer potion! The console now has {entity.Comp.StabilizerPotions} stabilizer potions(s)."));
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
        var slimeFound = false;
        foreach (var possibleSlime in _entityLookupSystem.GetEntitiesInRange(remoteEntity.Value, 0.5F))
        {
            if (_entityManager.TryGetComponent<SlimeComponent>(possibleSlime, out _))
            {
                slimeFound = true;
                if (_container.Insert(possibleSlime, xenobiologyConsoleComponent.SlimeContainer))
                    RaiseLocalEvent(new ConsoleTextMsgEvent(args.Performer, $"Picked up a {MetaData(possibleSlime).EntityName}."));
                else
                    RaiseLocalEvent(new ConsoleTextMsgEvent(args.Performer, $"Could not pick up {MetaData(possibleSlime).EntityName}. Try dropping some slimes."));
                break;
            }
        }
        if (!slimeFound)
            RaiseLocalEvent(new ConsoleTextMsgEvent(args.Performer, "No slimes found. Try moving closer to one."));
        args.Handled = true;
    }

    private void OnConsolePlaceSlime(ConsolePlaceSlimeEvent args)
    {
        if (!VerifyComponents(args, out var remoteEyeActorComponent, out var xenobiologyConsoleComponent, out var remoteEntity)) return;
        if (xenobiologyConsoleComponent.SlimeContainer.ContainedEntities.Count <= 0)
        {
            RaiseLocalEvent(new ConsoleTextMsgEvent(args.Performer, "No slimes stored. Try picking up one."));
            args.Handled = true;
            return;
        }
        var slime = xenobiologyConsoleComponent.SlimeContainer.ContainedEntities[0];
        _container.Remove(slime, xenobiologyConsoleComponent.SlimeContainer, destination: Transform(remoteEntity.Value).Coordinates);
        RaiseLocalEvent(new ConsoleTextMsgEvent(args.Performer, $"Placed down a {MetaData(slime).EntityName}."));
        args.Handled = true;
    }

    private void OnConsolePlaceMonkey(ConsolePlaceMonkeyEvent args)
    {
        if (!VerifyComponents(args, out var remoteEyeActorComponent, out var xenobiologyConsoleComponent, out var remoteEntity)) return;
        if (xenobiologyConsoleComponent.MonkeyCubes < 1)
        {
            RaiseLocalEvent(new ConsoleTextMsgEvent(args.Performer, $"Not enough monkey cubes stored ({xenobiologyConsoleComponent.MonkeyCubes}). Try inserting one, or recycling some already eaten monkeys."));
            args.Handled = true;
            return;
        }
        _entityManager.PredictedSpawnAtPosition(xenobiologyConsoleComponent.MonkeyProtoId, Transform(remoteEntity.Value).Coordinates);
        xenobiologyConsoleComponent.MonkeyCubes -= 1;
        RaiseLocalEvent(new ConsoleTextMsgEvent(args.Performer, $"Placed down a monkey. You now have {xenobiologyConsoleComponent.MonkeyCubes} cubes."));
        args.Handled = true;
    }

    private void OnConsoleRecycleMonkeyCorpse(ConsoleRecycleMonkeyCorpseEvent args)
    {
        if (!VerifyComponents(args, out var remoteEyeActorComponent, out var xenobiologyConsoleComponent, out var remoteEntity)) return;
        var monkeysFound = 0;
        foreach (var possibleMonkey in _entityLookupSystem.GetEntitiesInRange(remoteEntity.Value, 0.5F))
        {
            if (_tagSystem.HasTag(possibleMonkey, xenobiologyConsoleComponent.MonkeyTag))
            {
                if (!_entityManager.TryGetComponent<DamageableComponent>(possibleMonkey, out var damageableComponent))
                    continue;
                if (damageableComponent.TotalDamage < 100) continue; // Slimes don't eat monkeys above 100 damage
                PredictedQueueDel(possibleMonkey);
                xenobiologyConsoleComponent.MonkeyCubes += 0.5D;
                monkeysFound += 1;
            }
        }

        RaiseLocalEvent(monkeysFound > 0
            ? new ConsoleTextMsgEvent(args.Performer,
                $"Recycled {monkeysFound} monkey(s). You now have {xenobiologyConsoleComponent.MonkeyCubes}.")
            : new ConsoleTextMsgEvent(args.Performer,
                "No monkeys were found to recycle. Try getting closer or making sure they are damaged enough."));

        args.Handled = true;
    }

    private void OnConsoleApplyMutationPotion(ConsoleApplyMutationPotionEvent args)
    {
        if (!VerifyComponents(args, out var remoteEyeActorComponent, out var xenobiologyConsoleComponent, out var remoteEntity)) return;
        if (xenobiologyConsoleComponent.MutationPotions < 1)
        {
            RaiseLocalEvent(new ConsoleTextMsgEvent(args.Performer, $"Not enough mutation potions stored ({xenobiologyConsoleComponent.MutationPotions}). Try inserting one."));
            args.Handled = true;
            return;
        }
        foreach (var possibleSlime in _entityLookupSystem.GetEntitiesInRange(remoteEntity.Value, 0.5F))
        {
            if (_entityManager.TryGetComponent<SlimeComponent>(possibleSlime, out var slimeComponent))
            {
                slimeComponent.MutationChance += SlimeMutationPotionComponent.MutationChangeAmount;
                xenobiologyConsoleComponent.MutationPotions -= 1;
                RaiseLocalEvent(new ConsoleTextMsgEvent(args.Performer, $"Applied a mutation potion to a {MetaData(possibleSlime).EntityName}. It now has a mutation chance of {slimeComponent.MutationChance}"));
                break;
            }
        }
        args.Handled = true;
    }
    
    private void OnConsoleApplyStabilizerPotion(ConsoleApplyStabilizerPotionEvent args)
    {
        if (!VerifyComponents(args, out var remoteEyeActorComponent, out var xenobiologyConsoleComponent, out var remoteEntity)) return;
        if (xenobiologyConsoleComponent.StabilizerPotions < 1)
        {
            RaiseLocalEvent(new ConsoleTextMsgEvent(args.Performer, $"Not enough stabilizer potions stored ({xenobiologyConsoleComponent.StabilizerPotions}). Try inserting one."));
            args.Handled = true;
            return;
        }
        foreach (var possibleSlime in _entityLookupSystem.GetEntitiesInRange(remoteEntity.Value, 0.5F))
        {
            if (_entityManager.TryGetComponent<SlimeComponent>(possibleSlime, out var slimeComponent))
            {
                slimeComponent.MutationChance += SlimeStabilizerPotionComponent.MutationChangeAmount;
                xenobiologyConsoleComponent.StabilizerPotions -= 1;
                RaiseLocalEvent(new ConsoleTextMsgEvent(args.Performer, $"Applied a stabilizer potion to a {MetaData(possibleSlime).EntityName}. It now has a mutation chance of {slimeComponent.MutationChance}"));
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

public sealed partial class ConsoleTextMsgEvent(EntityUid user, string message)
{
    public EntityUid User = user;
    public string Message = message;
}