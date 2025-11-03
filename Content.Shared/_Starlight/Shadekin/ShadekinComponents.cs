using Content.Shared.Alert;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Starlight.Shadekin;

#region Shadekin
[RegisterComponent, NetworkedComponent, AutoGenerateComponentPause, AutoGenerateComponentState]
public sealed partial class ShadekinComponent : Component
{
    [DataField]
    public ProtoId<AlertPrototype> ShadekinAlert = "Shadekin";

    [ViewVariables(VVAccess.ReadOnly), AutoPausedField]
    public TimeSpan NextUpdate = TimeSpan.Zero;

    [DataField]
    public TimeSpan UpdateCooldown = TimeSpan.FromSeconds(1f);

    [AutoNetworkedField, ViewVariables]
    public ShadekinState CurrentState { get; set; } = ShadekinState.Dark;

    [DataField("thresholds", required: true)]
    public SortedDictionary<FixedPoint2, ShadekinState> Thresholds = new();
}

[Serializable, NetSerializable]
public enum ShadekinState : byte
{
    Invalid = 0,
    Dark = 1,
    Low = 2,
    Annoying = 3,
    High = 4,
    Extreme = 5

}
#endregion

#region Brighteye
[RegisterComponent, NetworkedComponent, AutoGenerateComponentPause, AutoGenerateComponentState]
public sealed partial class BrighteyeComponent : Component
{
    [DataField]
    public ProtoId<AlertPrototype> BrighteyeAlert { get; set; } = "ShadekinEnergy";

    /// <summary>
    /// How many Energy the brighteye has.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int Energy = 0;

    /// <summary>
    /// The Max Energy the brighteye can have.
    /// </summary>
    [DataField]
    public int MaxEnergy = 200;

    /// <summary>
    /// Shadekin Portal, if null then the portal does not exist.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid Portal;
}
#endregion
