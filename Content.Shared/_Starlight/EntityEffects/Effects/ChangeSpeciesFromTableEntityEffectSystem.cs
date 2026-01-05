using Content.Shared.EntityEffects;
using Content.Shared.EntityTable;
using Content.Shared.Humanoid;

namespace Content.Shared._Starlight.EntityEffects.Effects;

public sealed class ChangeSpeciesFromTableEntityEffectSystem : EntityEffectSystem<HumanoidAppearanceComponent, ChangeSpeciesFromTable>
{
    [Dependency] private readonly SharedHumanoidAppearanceSystem _sharedHumanoidAppearanceSystem = default!;
    [Dependency] private readonly EntityTableSystem _entityTableSystem = default!;

    protected override void Effect(Entity<HumanoidAppearanceComponent> entity,
        ref EntityEffectEvent<ChangeSpeciesFromTable> args)
    {
        var random = new System.Random();
        var sps = _entityTableSystem.GetSpawns(args.Effect.SpeciesTable, random);
        foreach (var species in sps)
        {
            _sharedHumanoidAppearanceSystem.SetSpecies(entity.Owner, species, true, entity.AsNullable());
        }
    }
}