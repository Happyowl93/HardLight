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
    ///     The direction of the charge
    /// </summary>
    [DataField]
    public Angle ChargeDirection;

    /// <summary>
    ///     Current charge direction as vector
    /// </summary>
    [DataField]
    public Vector2 ChargeDirectionVector;

    /// <summary>
    ///     Current charge speed
    /// </summary>
    [DataField]
    public float ChargeSpeed;

    /// <summary>
    ///     Damage dealt to creatures on charge impact
    /// </summary>
    [DataField]
    public float ChargeCreatureDamage;

    /// <summary>
    ///     How far creatures are thrown on charge impact
    /// </summary>
    [DataField]
    public float ChargeCreatureThrowDistance;

    /// <summary>
    ///     Damage dealt to structures on charge impact
    /// </summary>
    [DataField]
    public float ChargeStructuralDamage;

    /// <summary>
    ///     Speed modifier for Blood Rush
    /// </summary>
    [DataField]
    public float BloodRushSpeedMultiplier = 1.5f;

    /// <summary>
    ///     Duration of Blood Rush in seconds
    /// </summary>
    [DataField]
    public float BloodRushDuration = 10f;

    /// <summary>
    ///     Duration of Blood Swell in seconds
    /// </summary>
    [DataField]
    public float BloodSwellDuration = 30f;

    /// <summary>
    ///     Brute damage reduction during Blood Swell
    /// </summary>
    [DataField]
    public float BloodSwellBruteReduction = 0.6f;

    /// <summary>
    ///     Stamina/Burn damage reduction during Blood Swell
    /// </summary>
    [DataField]
    public float BloodSwellOtherReduction = 0.5f;

    /// <summary>
    ///     Bonus melee damage during Blood Swell (after 400 total blood)
    /// </summary>
    [DataField]
    public float BloodSwellMeleeBonusDamage = 14f;

    /// <summary>
    ///     Total blood threshold for enhanced Blood Swell
    /// </summary>
    [DataField]
    public float BloodSwellEnhancedThreshold = 400f;
}
