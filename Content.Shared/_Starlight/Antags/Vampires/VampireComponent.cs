using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Starlight.Antags.Vampires;

/// <summary>
/// Visual layer mapping for the Vampire blood counter alert view entity.
/// Shared so YAML can reference enum.VampireVisualLayers.* and client can use it to set layers.
/// Not net-serialized.
/// </summary>
public enum VampireVisualLayers : byte
{
    Digit1,
    Digit2,
    Digit3,
}

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class VampireComponent : Component
{
    /// <summary>
    /// Whether fangs are currently extended (drinking enabled).
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool FangsExtended = false;

    /// <summary>
    /// Total blood drunk by this vampire (units).
    /// </summary>
    [DataField, AutoNetworkedField]
    public int DrunkBlood = 0;

    /// <summary>
    /// Amount removed per sip when clicking a target with bloodstream.
    /// </summary>
    [DataField]
    public float SipAmount = 5f;

    /// <summary>
    /// Current blood fullness used instead of normal food need.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float BloodFullness = 0f;

    /// <summary>
    /// Max fullness cap.
    /// </summary>
    [DataField]
    public float MaxBloodFullness = 100f;

    /// <summary>
    /// Decay rate per second for blood fullness.
    /// </summary>
    [DataField]
    public float FullnessDecayPerSecond = 0.25f;

    /// <summary>
    /// Action prototype id for toggling fangs.
    /// </summary>
    [DataField]
    public EntProtoId ToggleFangsAction = "ActionVampireToggleFangs";

    /// <summary>
    /// Runtime action entity for toggling fangs.
    /// </summary>
    [DataField]
    public EntityUid? ToggleFangsActionEntity;
}
