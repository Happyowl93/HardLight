using Content.Shared.EntityEffects;
using Content.Shared.EntityTable;
using Content.Shared.Humanoid;
using Robust.Shared.Random;

namespace Content.Shared._Starlight.EntityEffects.Effects;

public sealed class ChangeSpeciesFromTableEntityEffectSystem : EntityEffectSystem<HumanoidAppearanceComponent, ChangeSpeciesFromTable>
{
    [Dependency] private readonly SharedHumanoidAppearanceSystem _sharedHumanoidAppearanceSystem = default!;
    [Dependency] private readonly EntityTableSystem _entityTableSystem = default!;
    [Dependency] private readonly IRobustRandom _robustRandom = default!;

    protected override void Effect(Entity<HumanoidAppearanceComponent> entity,
        ref EntityEffectEvent<ChangeSpeciesFromTable> args)
    {
        var random = _robustRandom.GetRandom();
        var sps = _entityTableSystem.GetSpawns(args.Effect.SpeciesTable, random);
        foreach (var species in sps) 
        {
            _sharedHumanoidAppearanceSystem.SetSpecies(entity.Owner, species, true, entity.AsNullable());
        }
    }
}