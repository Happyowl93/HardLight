using Content.Shared.EntityEffects;
using Content.Shared.EntityEffects.Effects.EntitySpawning;
using Content.Shared.EntityTable;
using Robust.Shared.Network;

namespace Content.Shared._Starlight.EntityEffects.Effects.EntitySpawning;

/// <summary>
/// Spawns a number of entities from a given prototype at the coordinates of this entity.
/// Amount is modified by scale.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed class SpawnEntityFromTableEntityEffectSystem : EntityEffectSystem<TransformComponent, SpawnEntityFromTable>
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly EntityTableSystem _entityTable = default!;
    
    protected override void Effect(Entity<TransformComponent> entity, ref EntityEffectEvent<SpawnEntityFromTable> args)
    {
        var quantity = args.Effect.Number * (int)Math.Floor(args.Scale);
        var random = new System.Random();
        
        if (_net.IsServer)
        {
            for (var i = 0; i < quantity; i++)
            {
                var spawns = _entityTable.GetSpawns(args.Effect.EntityTable, random);
                foreach (var proto in spawns)
                    SpawnNextToOrDrop(proto, entity, entity.Comp);
            }
        }
    }
}