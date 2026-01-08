using System.Numerics;
using Content.Shared.Coordinates;
using Content.Shared.Damage.Components;
using Content.Shared.Interaction;
using Content.Shared.Jittering;
using Content.Shared.Power;
using Content.Shared.Storage.Components;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared._Starlight.Xenobiology;

public sealed class SlimeProcessorSystem : EntitySystem
{
    [Dependency] private readonly EntityManager _entityManager = default!;
    [Dependency] private readonly SharedJitteringSystem _jitteringSystem = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookupSystem = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<SlimeProcessorComponent>();
        while (query.MoveNext(out var uid, out var slimeProcessorComponent))
        {
            if (slimeProcessorComponent.IsProcessing)
            {
                if (!slimeProcessorComponent.ProcessingFinishedMoment.HasValue)
                {
                    slimeProcessorComponent.ProcessingFinishedMoment = _gameTiming.CurTime + slimeProcessorComponent.ProcessingTime;
                    continue;
                }

                if (slimeProcessorComponent.ProcessingFinishedMoment.Value > _gameTiming.CurTime) continue;
                System.Random random = new System.Random();
                foreach (var entity in slimeProcessorComponent.SlimeContainer.ContainedEntities)
                {
                    if (!_entityManager.TryGetComponent(entity, out SlimeComponent? slimeComponent)) continue;
                    for (int i = 0; i < slimeProcessorComponent.YieldMultiplier + slimeComponent.SlimeSteroidAmount; i++)
                    {
                        Vector2 randomOffset = new Vector2(random.NextFloat(-0.2F, 0.2F), random.NextFloat(-0.2F, 0.2F));
                        EntityCoordinates ec = new EntityCoordinates(uid, uid.ToCoordinates().Position + randomOffset);
                        _entityManager.PredictedSpawnAtPosition(slimeComponent.Extract, ec);
                    }
                    PredictedQueueDel(entity);
                }
                    
                RemCompDeferred<JitteringComponent>(uid);
                slimeProcessorComponent.IsProcessing = false;
                slimeProcessorComponent.SlimeAcquireMoment = _gameTiming.CurTime + slimeProcessorComponent.SlimeAcquireCooldown;
            }
            else
            {
                if (!slimeProcessorComponent.SlimeAcquireMoment.HasValue)
                {
                    slimeProcessorComponent.SlimeAcquireMoment = _gameTiming.CurTime + slimeProcessorComponent.SlimeAcquireCooldown;
                    continue;
                }

                if (slimeProcessorComponent.SlimeAcquireMoment.Value > _gameTiming.CurTime) continue;
                foreach (var entity in _entityLookupSystem.GetEntitiesInRange(uid, 1F))
                {
                    if (!_entityManager.TryGetComponent(entity, out SlimeComponent? slimeComponent)) continue;
                    if (!_entityManager.TryGetComponent(entity, out DamageableComponent? damageableComponent)) continue;
                    if (damageableComponent.TotalDamage >= 200)
                    {
                        _container.Insert(entity, slimeProcessorComponent.SlimeContainer);
                        slimeProcessorComponent.SlimeAcquireMoment = _gameTiming.CurTime + slimeProcessorComponent.SlimeAcquireCooldown;
                        break;
                    }
                }
            }
        }
    }

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SlimeProcessorComponent, ActivateInWorldEvent>(OnAfterActivate);
        SubscribeLocalEvent<SlimeProcessorComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<SlimeProcessorComponent, PowerChangedEvent>(OnPowerChanged);
    }

    private void OnAfterActivate(Entity<SlimeProcessorComponent> ent, ref ActivateInWorldEvent args)
    {
        if (ent.Comp.SlimeContainer.ContainedEntities.Count <= 0) return;
        
        ent.Comp.ProcessingFinishedMoment = _gameTiming.CurTime + ent.Comp.ProcessingTime;
        ent.Comp.IsProcessing = true;
        _jitteringSystem.AddJitter(ent.Owner, -10, 100);
    }

    private void OnComponentInit(Entity<SlimeProcessorComponent> ent, ref ComponentInit args)
    {
        ent.Comp.SlimeContainer = _container.EnsureContainer<ContainerSlot>(ent.Owner, SlimeProcessorComponent.SlimeContainerName);
    }
    
    private void OnPowerChanged(Entity<SlimeProcessorComponent> ent, ref PowerChangedEvent args)
    {
        if (!args.Powered)
        {
            ent.Comp.IsProcessing = false;
            ent.Comp.SlimeAcquireMoment = null;
            ent.Comp.ProcessingFinishedMoment = null;
            if (HasComp<JitteringComponent>(ent.Owner))
                RemCompDeferred<JitteringComponent>(ent.Owner);
        }
    }
}