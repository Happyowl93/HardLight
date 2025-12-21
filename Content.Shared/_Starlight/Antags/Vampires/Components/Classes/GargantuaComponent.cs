using System.Numerics;
using Robust.Shared.Audio;
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
    public Angle ChargeDirection;

    /// <summary>
    ///     Current charge direction as vector
    /// </summary>
    public Vector2 ChargeDirectionVector;

    /// <summary>
    ///     Current charge speed
    /// </summary>
    public float ChargeSpeed;

    /// <summary>
    ///     Damage dealt to creatures on charge impact
    /// </summary>
    public float ChargeCreatureDamage;

    /// <summary>
    ///     How far creatures are thrown on charge impact
    /// </summary>
    public float ChargeCreatureThrowDistance;

    /// <summary>
    ///     Damage dealt to structures on charge impact
    /// </summary>
    public float ChargeStructuralDamage;

    /// <summary>
    ///     Sound played when the charge ends due to an impact.
    ///     Set from <see cref="VampireChargeActionEvent"/> when the ability is activated.
    /// </summary>
    public SoundSpecifier? ChargeImpactSound;

    /// <summary>
    ///     Runtime speed multiplier for Blood Rush.
    ///     Set from <see cref="VampireBloodRushActionEvent"/> when the ability is activated.
    /// </summary>
    public float BloodRushSpeedMultiplier;

    /// <summary>
    ///     Runtime bonus melee damage during Blood Swell (enhanced).
    ///     Set from <see cref="VampireBloodSwellActionEvent"/> when the ability is activated.
    /// </summary>
    public float BloodSwellMeleeBonusDamage;

    /// <summary>
    ///     Runtime total-blood threshold for enhanced Blood Swell.
    ///     Set from <see cref="VampireBloodSwellActionEvent"/> when the ability is activated.
    /// </summary>
    public float BloodSwellEnhancedThreshold;
}
