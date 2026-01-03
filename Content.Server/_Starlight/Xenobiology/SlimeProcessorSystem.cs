using System.Numerics;
using Content.Shared.Coordinates;
using Content.Shared.Damage.Components;
using Content.Shared.Interaction;
using Content.Shared.Jittering;
using Content.Shared.Power;
using Robust.Shared.Map;
using Robust.Shared.Random;

namespace Content.Server._Starlight.Xenobiology;

public sealed class SlimeProcessorSystem : EntitySystem
{
    [Dependency] private readonly EntityManager _entityManager = default!;
    [Dependency] private readonly SharedJitteringSystem _jitteringSystem = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookupSystem = default!;
    
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<Shared._Starlight.Xenobiology.SlimeProcessorComponent>();
        while (query.MoveNext(out var uid, out var slimeProcessorComponent))
        {
            if (!slimeProcessorComponent.IsPowered)
            {
                slimeProcessorComponent.IsProcessing = false;
                slimeProcessorComponent.SlimeAcquireTimer = slimeProcessorComponent.SlimeAcquireCooldown;
                continue;
            }
            
            if (slimeProcessorComponent.IsProcessing)
            {
                slimeProcessorComponent.ProcessingTimer -= frameTime;

                if (slimeProcessorComponent.ProcessingTimer <= 0) // If we are processing slimes
                {
                    Random random = new Random();
                    foreach (var proto in slimeProcessorComponent.Extracts)
                    {
                        Vector2 randomOffset = new Vector2(random.NextFloat(-0.2F, 0.2F), random.NextFloat(-0.2F, 0.2F));
                        EntityCoordinates ec = new EntityCoordinates(uid, uid.ToCoordinates().Position + randomOffset);
                        _entityManager.SpawnAtPosition(proto, ec);
                    }

                    slimeProcessorComponent.Extracts.Clear();
                    RemCompDeferred<JitteringComponent>(uid);
                    slimeProcessorComponent.IsProcessing = false;
                    slimeProcessorComponent.SlimeAcquireTimer = slimeProcessorComponent.SlimeAcquireCooldown;
                }
            }
            else
            {
                slimeProcessorComponent.SlimeAcquireTimer -= frameTime;
                if (slimeProcessorComponent.SlimeAcquireTimer <= 0)
                {
                    foreach (var entity in _entityLookupSystem.GetEntitiesInRange(uid, 1F))
                    {
                        if (!_entityManager.TryGetComponent(entity, out Shared._Starlight.Xenobiology.SlimeComponent? slimeComponent)) continue;
                        if (!_entityManager.TryGetComponent(entity, out DamageableComponent? damageableComponent)) continue;
                        if (damageableComponent.TotalDamage >= 200)
                        {
                            for (int i = 0; i < slimeProcessorComponent.YieldMultiplier; i++)
                            {
                                slimeProcessorComponent.Extracts.Add(slimeComponent.Extract);
                            }
                            QueueDel(entity);
                            slimeProcessorComponent.SlimeAcquireTimer = slimeProcessorComponent.SlimeAcquireCooldown;
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
        SubscribeLocalEvent<Shared._Starlight.Xenobiology.SlimeProcessorComponent, ActivateInWorldEvent>(OnAfterActivate);
        SubscribeLocalEvent<Shared._Starlight.Xenobiology.SlimeProcessorComponent, PowerChangedEvent>(OnPowerChanged);
    }
    
    private void OnPowerChanged(EntityUid uid, Shared._Starlight.Xenobiology.SlimeProcessorComponent component, ref PowerChangedEvent args)
    {
        component.IsPowered = args.Powered;
    }

    private void OnAfterActivate(Entity<Shared._Starlight.Xenobiology.SlimeProcessorComponent> ent, ref ActivateInWorldEvent args)
    {
        if (ent.Comp.Extracts.Count <= 0)
            return;
        
        ent.Comp.ProcessingTimer = ent.Comp.ProcessingTime;
        ent.Comp.IsProcessing = true;
        _jitteringSystem.AddJitter(ent.Owner, -10, 100);
    }
}