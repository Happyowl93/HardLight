using Content.Shared.Chemistry;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared._Starlight.Plumbing.Components;

/// <summary>
/// A plumbing-connected smart dispenser that stores reagents pulled from the network,
/// supports inserted container dispensing, and label matching dispensing.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class PlumbingSmartDispenserComponent : Component
{
    /// <summary>
    /// The solution container name for the dispenser's reagent storage.
    /// </summary>
    [DataField]
    public string SolutionName = "fridge";

    /// <summary>
    /// Maximum amount of any single reagent the dispenser can store.
    /// </summary>
    [DataField]
    public FixedPoint2 MaxPerReagent = FixedPoint2.New(200);

    /// <summary>
    /// Selected UI dispense amount for inserted-container dispensing.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public ReagentDispenserDispenseAmount DispenseAmount = ReagentDispenserDispenseAmount.U10;
}
