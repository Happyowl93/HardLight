using Content.Shared.Body.Events;
using Content.Shared.Damage;
using Content.Shared.Gibbing.Events;
using Content.Shared.Throwing;
using Content.Shared.EntityTable;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.BPL.Pinata;
public sealed class PinataSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedPhysicsSystem Physics = default!;
    [Dependency] protected readonly SharedTransformSystem TransformSystem = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly EntityTableSystem _entityTable =  default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PinataComponent, DamageModifyEvent>(OnHit);
        SubscribeLocalEvent<PinataComponent, BeingGibbedEvent>(OnGib);
        SubscribeLocalEvent<PinataComponent, EntityGibbedEvent>(OnGibAlt);
    }

    //This is from most explicit gib effects
    private void OnGibAlt(Entity<PinataComponent> ent, ref EntityGibbedEvent args) => RemoveGibbedParts(ent, args.DroppedEntities);

    //This is from taking too much damage and gibbing.
    private void OnGib(Entity<PinataComponent> ent, ref BeingGibbedEvent args) => RemoveGibbedParts(ent, args.GibbedParts);

    private void RemoveGibbedParts(Entity<PinataComponent> ent, ICollection<EntityUid> guts)
    {
        foreach (var organ in guts)
            QueueDel(organ);
        
        guts.Clear();

        for (int i = 0; i < _random.Next(12, 21); i++)
            SpawnItem(ent);
    }

    private void OnHit(Entity<PinataComponent> ent, ref DamageModifyEvent args)
    {
        var damPerGroup = args.Damage.GetDamagePerGroup(_proto);
        if (!damPerGroup.TryGetValue("Brute", out var brute) || brute <= 5) //Has to be a decent hit
            return;
            
        for (int i = 0; i < _random.Next(ent.Comp.MinSpawn, ent.Comp.MaxSpawn); i++)
            SpawnItem(ent);
    }

    public void SpawnItem(Entity<PinataComponent> entity)
    {
        var spawns = _entityTable.GetSpawns(entity.Comp.Table);
        var coords = Transform(entity).Coordinates;
        foreach (var spawn in spawns)
        {
            var entity = Spawn(spawn, coords);
            _throwing.TryThrow(entity , _random.NextVector2(), baseThrowSpeed: 5f);
        }

    }
}
