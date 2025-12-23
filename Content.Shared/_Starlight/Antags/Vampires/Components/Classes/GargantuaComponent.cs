using System.Numerics;
using Robust.Shared.GameStates;

namespace Content.Shared._Starlight.Antags.Vampires.Components.Classes;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class GargantuaComponent : Component
{
    /// <summary>
    ///     Whether Blood Swell is currently active
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool BloodSwellActive;

    /// <summary>
    ///     When Blood Swell ends
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan? BloodSwellEndTime;

    /// <summary>
    ///     Whether Blood Rush is currently active
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool BloodRushActive;

    /// <summary>
    ///     When Blood Rush ends
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan? BloodRushEndTime;

    /// <summary>
    ///     Whether Overwhelming Force toggle is active
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool OverwhelmingForceActive;

    /// <summary>
    ///     Whether vampire is currently charging
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool IsCharging;

    /// <summary>
    ///     Current charge direction as vector
    /// </summary>
    public Vector2 ChargeDirectionVector;
}
