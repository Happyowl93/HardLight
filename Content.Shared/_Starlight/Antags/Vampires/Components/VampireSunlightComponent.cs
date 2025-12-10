namespace Content.Shared._Starlight.Antags.Vampires.Components;

[RegisterComponent]
public sealed partial class VampireSunlightComponent : Component
{
    /// <summary>
    ///     How much heat damage is applied when the burn effect triggers
    /// </summary>
    [DataField]
    public float BurnDamage = 3f;

    /// <summary>
    ///     Interval between exposure ticks while in space
    /// </summary>
    [DataField]
    public float DamageInterval = 2f;

    /// <summary>
    ///     Blood cost per exposure tick while the vampire still has reserves
    /// </summary>
    [DataField]
    public int BloodDrainPerInterval = 10;

    /// <summary>
    ///     Chance to apply the burn/ignite effect while the vampire still has blood
    /// </summary>
    [DataField]
    public float BloodEffectChance = 0.1f;

    /// <summary>
    ///     Chance to apply the burn/ignite effect while the vampire has no blood
    /// </summary>
    [DataField]
    public float BloodlessEffectChance = 0.85f;

    /// <summary>
    ///     Fire stacks added when the vampire ignites
    /// </summary>
    [DataField]
    public float FireStacksOnIgnite = 2f;

    /// <summary>
    ///     Genetic damage applied each tick when the vampire has no blood
    /// </summary>
    [DataField]
    public float GeneticDamagePerInterval = 10f;

    /// <summary>
    ///     Threshold of accumulated genetic damage after which the vampire turns to ash
    /// </summary>
    [DataField]
    public float GeneticDustThreshold = 100f;

    /// <summary>
    ///     How long a vampire can linger in space before they start taking damage
    /// </summary>
    [DataField]
    public float GracePeriod = 1.5f;

    /// <summary>
    ///     Minimum seconds between popup warnings to the player
    /// </summary>
    [DataField]
    public float WarningPopupCooldown = 5f;

    /// <summary>
    ///     Localization string displayed when the vampire starts burning
    /// </summary>
    [DataField]
    public LocId WarningPopup = "vampire-space-burn-warning";

    [ViewVariables]
    public float TimeInSpace;

    [ViewVariables]
    public float DamageAccumulator;

    [ViewVariables]
    public TimeSpan NextWarningPopup;
}
