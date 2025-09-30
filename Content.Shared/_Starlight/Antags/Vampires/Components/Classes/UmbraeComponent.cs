using Robust.Shared.GameStates;

namespace Content.Shared._Starlight.Antags.Vampires.Components.Classes;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class UmbraeComponent : Component
{
    [AutoNetworkedField]
    public bool CloakOfDarknessActive = false;
    [AutoNetworkedField]
    public bool EternalDarknessActive = false;
    public EntityUid? EternalDarknessAuraEntity = null;
    [AutoNetworkedField]
    public bool ShadowBoxingActive = false;

    [AutoNetworkedField]
    public EntityUid? ShadowBoxingTarget = null;
    public TimeSpan? ShadowBoxingEndTime = null;
    public bool ShadowBoxingLoopRunning = false;
    public int CloakOfDarknessLoopId = 0;
    public int EternalDarknessLoopId = 0;

    [AutoNetworkedField]
    public EntityUid? SpawnedShadowAnchorBeacon = null;
}