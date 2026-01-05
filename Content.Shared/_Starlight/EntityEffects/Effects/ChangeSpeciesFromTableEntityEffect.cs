using Content.Shared.EntityEffects;
using Content.Shared.EntityTable.EntitySelectors;

namespace Content.Shared._Starlight.EntityEffects.Effects;

/// <summary>
/// A type of <see cref="EntityEffectBase{T}"/> for effects that changes a target entity's species from a table.
/// </summary>
/// <inheritdoc cref="EntityEffect"/>
public sealed partial class ChangeSpeciesFromTable : EntityEffectBase<ChangeSpeciesFromTable>
{
    /// <summary>
    /// Table from which we're pulling the species name
    /// </summary>
    [DataField(required: true)]
    public EntityTableSelector SpeciesTable = default!;
}