using Robust.Shared.GameStates;
// STARLIGHT: This file was added for future reflective set bonus configuration.
// STARLIGHT: This component is currently unused, but reserved for future reflective set bonus configuration.

namespace Content.Shared.Clothing.Components;

/// <summary>
/// Starlight: Detects when both reflective vest and reflective helmet are equipped and boosts reflection to 100%.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ReflectiveSetBonusComponent : Component
{
    /// <summary>
    /// The entity ID of the reflective vest that completes this set bonus.
    /// </summary>
    [DataField("requiredVestId")]
    public string RequiredVestId = "ClothingOuterArmorReflective";

    /// <summary>
    /// The entity ID of the reflective helmet that completes this set bonus.
    /// </summary>
    [DataField("requiredHelmetId")]
    public string RequiredHelmetId = "ClothingHeadHelmetReflective";

    /// <summary>
    /// The reflection probability when the full set is equipped.
    /// </summary>
    [DataField("fullSetReflectProb")]
    public float FullSetReflectProb = 1.0f;

    /// <summary>
    /// The default reflection probability for a single piece.
    /// </summary>
    [DataField("singlePieceReflectProb")]
    public float SinglePieceReflectProb = 0.5f;
}
