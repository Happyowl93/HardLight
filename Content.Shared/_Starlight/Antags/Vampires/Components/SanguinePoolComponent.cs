using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Starlight.Antags.Vampires.Components;

/// <summary>
///     Marker placed on the polymorph form created by Sanguine Pool.
///     Handles collision filtering on both client and server and exposes
///     tunables used while the form is active (trail spawning, etc.).
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SanguinePoolComponent : Component
{
    /// <summary>
    ///     How often to spawn a trailing puddle while gliding around.
    /// </summary>
    [DataField]
    public float TrailInterval = 0.3f;

    /// <summary>
    ///     Prototype spawned each time <see cref="TrailInterval"/> elapses.
    /// </summary>
    [DataField]
    public EntProtoId? TrailPrototype = "PuddleBlood";

    /// <summary>
    ///     Internal accumulator used by the server system.
    /// </summary>
    [ViewVariables]
    public float Accumulator;
}
