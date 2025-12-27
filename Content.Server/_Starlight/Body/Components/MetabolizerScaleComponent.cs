namespace Content.Server._Starlight.Body.Components;

/// <summary>
///     Metabolizer Scale: changes the efficiency at which reagents are processed.
///     Lower values means they last longer and effect more.
///     Higher values means they waste more and effect less.
/// </summary>
[RegisterComponent]
public sealed partial class MetabolizerScaleComponent : Component
{
    /// <summary>
    ///     Sets the scale for the "Medicine" reagent group efficiency and rate
    /// </summary>
    [DataField]
    public float MedicineScale = 1f;

    /// <summary>
    ///     Sets the scale for the "Poison" reagent group efficiency and rate
    /// </summary>
    [DataField]
    public float PoisonScale = 1f;

    /// <summary>
    ///     Sets the scale for the "Narcotics" reagent group efficiency and rate
    /// </summary>
    [DataField]
    public float NarcoticScale = 1f;
}