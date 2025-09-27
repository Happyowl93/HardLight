using Content.Shared.Atmos;
namespace Content.Server.Cargo.Components;

/// <summary>
/// Takes input gas and stores it for sale
/// </summary>
[RegisterComponent]
public sealed partial class CargoGasPalletComponent : Component, IGasMixtureHolder
{
    /// <summary>
    /// The name of the pipe inlet
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("inlet")]
    public string InletName { get; set; } = "pipe";

    /// <summary>
    /// A gas mixture representing the remote resivoir.
    /// </summary>
    [DataField("air")]
    public GasMixture Air { get; set; } = new GasMixture();
}
