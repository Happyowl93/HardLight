using Content.Shared.EntityEffects;

namespace Content.Shared.Starlight.EntityEffects.Effects;

/// <summary>
/// See serverside system.
/// </summary>
/// <inheritdoc cref="EntityEffect"/>
public sealed partial class ChangeGasTemperature : EntityEffectBase<ChangeGasTemperature>
{
    /// <summary>
    ///     The amount we're adjusting the temperature by
    /// </summary>
    [DataField]
    public float Temperature;
}