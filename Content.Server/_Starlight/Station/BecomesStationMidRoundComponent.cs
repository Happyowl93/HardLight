using Content.Server.GameTicking;
using Content.Server.Station.Systems;
using Robust.Shared.Prototypes;

namespace Content.Server._Starlight.Station;

/// <summary>
/// Similar to BecomesStation, but causes it to initialize on grid/map spawn instead of solely on gamemap load.
/// </summary>
[RegisterComponent]
[Access(typeof(GameTicker), typeof(StationSystem))]
public sealed partial class BecomesStationMidRoundComponent : Component
{
    [DataField] [ViewVariables(VVAccess.ReadOnly)]
    public string? InitializedId = null;
    [DataField(required: true)] public string? Id = null;
    [DataField] public EntProtoId StationProto = new("StandardNanotrasenStation");
}