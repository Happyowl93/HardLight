using Robust.Shared.GameStates;

namespace Content.Shared._Starlight.Antags.Vampires.Components;

/// <summary>
///     Marker component applied to entities that have been enthralled by a Dantalion vampire.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class VampireThrallComponent : Component
{
    /// <summary>
    ///     The vampire currently controlling this thrall
    /// </summary>
    [AutoNetworkedField]
    public EntityUid? Master;
}
