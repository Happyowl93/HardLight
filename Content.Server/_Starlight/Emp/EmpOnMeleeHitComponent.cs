namespace Content.Server._Starlight.Emp;

/// <summary>
/// Upon hitting an object will EMP area around it.
/// </summary>
[RegisterComponent]
[Access(typeof(Content.Server.Emp.EmpSystem))]
public sealed partial class EmpOnMeleeHitComponent : Component
{
    [DataField("range"), ViewVariables(VVAccess.ReadWrite)]
    public float Range = 1.0f;

    /// <summary>
    /// How much energy will be consumed per battery in range
    /// </summary>
    [DataField("energyConsumption"), ViewVariables(VVAccess.ReadWrite)]
    public float EnergyConsumption;

    /// <summary>
    /// How long it disables targets in seconds
    /// </summary>
    [DataField("disableDuration"), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan DisableDuration = TimeSpan.FromSeconds(60);

    /// <summary>
    /// How much battery power it takes to trigger the EMP
    /// </summary>
    [DataField("energyPerUse"), ViewVariables(VVAccess.ReadWrite)]
    public float EnergyPerUse = 0f;


}
