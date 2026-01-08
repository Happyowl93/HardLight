using System.Numerics;
using Content.Shared.Coordinates;
using Content.Shared.Damage.Components;
using Content.Shared.Interaction;
using Content.Shared.Jittering;
using Content.Shared.Power;
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
    
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<SlimeProcessorComponent>();
        while (query.MoveNext(out var uid, out var slimeProcessorComponent))
        {
            if (!slimeProcessorComponent.IsPowered)
            {
                slimeProcessorComponent.IsProcessing = false;
                slimeProcessorComponent.SlimeAcquireMoment = null;
                continue;
            }
            
            if (slimeProcessorComponent.IsProcessing)
            {
                if (!slimeProcessorComponent.ProcessingFinishedMoment.HasValue || slimeProcessorComponent.ProcessingFinishedMoment.Value <= _gameTiming.CurTime)
                {
                    System.Random random = new System.Random();
                    foreach (var proto in slimeProcessorComponent.Extracts)
                    {
                        Vector2 randomOffset = new Vector2(random.NextFloat(-0.2F, 0.2F), random.NextFloat(-0.2F, 0.2F));
                        EntityCoordinates ec = new EntityCoordinates(uid, uid.ToCoordinates().Position + randomOffset);
                        _entityManager.SpawnAtPosition(proto, ec);
                    }

                    slimeProcessorComponent.Extracts.Clear();
                    RemCompDeferred<JitteringComponent>(uid);
                    slimeProcessorComponent.IsProcessing = false;
                    slimeProcessorComponent.SlimeAcquireMoment = _gameTiming.CurTime + slimeProcessorComponent.SlimeAcquireCooldown;
                }
            }
            else
            {
                if (!slimeProcessorComponent.SlimeAcquireMoment.HasValue || slimeProcessorComponent.SlimeAcquireMoment.Value <= _gameTiming.CurTime)
                {
                    foreach (var entity in _entityLookupSystem.GetEntitiesInRange(uid, 1F))
                    {
                        if (!_entityManager.TryGetComponent(entity, out SlimeComponent? slimeComponent)) continue;
                        if (!_entityManager.TryGetComponent(entity, out DamageableComponent? damageableComponent)) continue;
                        if (damageableComponent.TotalDamage >= 200)
                        {
                            for (int i = 0; i < slimeProcessorComponent.YieldMultiplier + slimeComponent.SlimeSteroidAmount; i++)
                            {
                                slimeProcessorComponent.Extracts.Add(slimeComponent.Extract);
                            }
                            PredictedQueueDel(entity);
                            slimeProcessorComponent.SlimeAcquireMoment = _gameTiming.CurTime + slimeProcessorComponent.SlimeAcquireCooldown;
                            break;
                        }
                    }
                }
            }
        }
    }

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SlimeProcessorComponent, ActivateInWorldEvent>(OnAfterActivate);
        SubscribeLocalEvent<SlimeProcessorComponent, PowerChangedEvent>(OnPowerChanged);
    }
    
    private void OnPowerChanged(EntityUid uid, SlimeProcessorComponent component, ref PowerChangedEvent args)
    {
        component.IsPowered = args.Powered;
    }

    private void OnAfterActivate(Entity<SlimeProcessorComponent> ent, ref ActivateInWorldEvent args)
    {
        if (ent.Comp.Extracts.Count <= 0)
            return;
        
        ent.Comp.ProcessingFinishedMoment = _gameTiming.CurTime + ent.Comp.ProcessingTime;
        ent.Comp.IsProcessing = true;
        _jitteringSystem.AddJitter(ent.Owner, -10, 100);
    }
}