using Robust.Shared.Prototypes;

namespace Content.Shared._Starlight.FiringPins;

/// <summary>
/// Marker prototype for firing pins, so we can describe what pins should fit in a holder
/// </summary>
[Prototype]
public sealed partial class FiringPinClassPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;
}