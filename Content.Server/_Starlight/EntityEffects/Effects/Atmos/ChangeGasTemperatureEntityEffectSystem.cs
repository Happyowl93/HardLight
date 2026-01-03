using Content.Server.Atmos.EntitySystems;
using Content.Shared.EntityEffects;
using Content.Shared.Starlight.EntityEffects.Effects;

namespace Content.Server._Starlight.EntityEffects.Effects.Atmos;

/// <summary>
/// This effect adjusts the gas temperature this entity is currently on.
/// The amount changed is modified by scale.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class ChangeGasTemperatureEntityEffectSystem : EntityEffectSystem<TransformComponent, ChangeGasTemperature>
{
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;

    protected override void Effect(Entity<TransformComponent> entity, ref EntityEffectEvent<ChangeGasTemperature> args)
    {
        var tileMix = _atmosphere.GetContainingMixture(entity.AsNullable(), false, true);
        if (tileMix == null) return;
        tileMix.Temperature += (args.Effect.Temperature * args.Scale);
    }
}