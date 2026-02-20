using Content.Shared.Medical.SuitSensor;
using Robust.Shared.Map; // Starlight
using Robust.Shared.Serialization;

namespace Content.Shared.Medical.CrewMonitoring;

[Serializable, NetSerializable]
public enum CrewMonitoringUIKey
{
    Key
}

[Serializable, NetSerializable]
public sealed class CrewMonitoringState : BoundUserInterfaceState
{
    public TimeSpan LastUpdate;
    public List<SuitSensorStatus> Sensors;

    public CrewMonitoringState(TimeSpan lastUpdate, List<SuitSensorStatus> sensors)
    {
        LastUpdate = lastUpdate;
        Sensors = sensors;
    }
}
// Starlight-start
[Serializable, NetSerializable]
public sealed partial class CrewMonitoringWarpRequestMessage : BoundUserInterfaceMessage
{
    public NetCoordinates Coordinates;

    public CrewMonitoringWarpRequestMessage(NetCoordinates coordinates)
    {
        Coordinates = coordinates;
    }
}
// Starlight-end
