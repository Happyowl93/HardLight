using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Starlight.Antags.Vampires;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]

public sealed partial class VampireComponent : Component
{
    [DataField]
    public List<ProtoId<EntityPrototype>> BaseVampireActions = new()
    {
        "ActionVampireToggleFangs",
        "ActionVampireGlare",
        "ActionVampireRejuvenateI"
    };

    /// <summary>
    /// Lifetime total blood drunk. Used for unlocking abilities
    /// </summary>
    [ DataField, AutoNetworkedField]
    public int TotalBlood = 0;

    /// <summary>
    /// Total blood drunk by this vampire, used for blood cost calculations
    /// </summary>
    [DataField, AutoNetworkedField]
    public int DrunkBlood = 0;

    [ViewVariables(VVAccess.ReadOnly), DataField, AutoNetworkedField]
    public bool FangsExtended = false;

    public float SipAmount = 10f;

    /// <summary>
    /// Current blood fullness used instead of normal food needs
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly), DataField, AutoNetworkedField]
    public float BloodFullness = 90f;
    [DataField]
    public float MaxBloodFullness = 200f; // keep configurable

    /// <summary>
    /// Decay rate per second for blood fullness
    /// </summary>
    public float FullnessDecayPerSecond = 0.25f;

    public VampireActionEntities Actions = new();
    public bool IsDrinking = false;

    /// <summary>
    /// tracking how much blood was drunk from each target
    /// </summary>
    public Dictionary<EntityUid, int> BloodDrunkFromTargets = new();

    public int MaxBloodPerTarget = 200;
    public EntityUid? SpawnedClaws = null;
    [AutoNetworkedField]
    public bool InSanguinePool = false;
    public int? OriginalCollisionMask = null;
    public int? OriginalCollisionLayer = null;
    [DataField]
    public int ClassSelectThreshold = 150;
    [DataField]
    public int RejuvenateIIThreshold = 200;
    [DataField]
    public int ActionRefreshThreshold = 5;

    [ViewVariables(VVAccess.ReadOnly)]
    public int LastRefreshedBloodLevel = -1;

    [AutoNetworkedField]
    public bool BloodBringersRiteActive = false;
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
    public Dictionary<string, int>? PoolOriginalMasks = null;
    public Dictionary<string, int>? PoolOriginalLayers = null;
    public bool PoolOwnedGodmode = false;

    public int BloodBringersRiteLoopId = 0;
    public int CloakOfDarknessLoopId = 0;
    public int EternalDarknessLoopId = 0;

    [ViewVariables(VVAccess.ReadOnly), DataField, AutoNetworkedField]
    public bool FullPower = false;

    [ViewVariables(VVAccess.ReadOnly), DataField, AutoNetworkedField]
    public int UniqueHumanoidVictims = 0;

    [ViewVariables(VVAccess.ReadOnly), DataField, AutoNetworkedField]
    public VampireClassType ChosenClass = VampireClassType.None;

    [AutoNetworkedField]
    public EntityUid? SpawnedShadowAnchorBeacon = null;
}

/// <summary>
/// Holds all runtime action entity Uids for a vampire
/// </summary>
[Serializable]
public sealed class VampireActionEntities
{
    [DataField]
    public EntityUid? ToggleFangsActionEntity;
    [DataField]
    public EntityUid? GlareActionEntity;

    [DataField]
    public EntityUid? RejuvenateIActionEntity;

    [DataField]
    public EntityUid? RejuvenateIIActionEntity;

    [DataField]
    public EntityUid? ClassSelectActionEntity;

    [DataField]
    public EntityUid? HemomancerClawsActionEntity;

    [DataField]
    public EntityUid? HemomancerTendrilsActionEntity;

    [DataField]
    public EntityUid? BloodBarrierActionEntity;

    [DataField]
    public EntityUid? SanguinePoolActionEntity;

    [DataField]
    public EntityUid? BloodEruptionActionEntity;

    [DataField]
    public EntityUid? BloodBringersRiteActionEntity;
    [DataField]
    public EntityUid? VampireCloakOfDarknessActionEntity;
    [DataField]
    public EntityUid? ShadowSnareActionEntity;
    [DataField]
    public EntityUid? DarkPassageActionEntity;
    [DataField]
    public EntityUid? ExtinguishActionEntity;
    [DataField]
    public EntityUid? EternalDarknessActionEntity;
    [DataField]
    public EntityUid? ShadowAnchorActionEntity;
    [DataField]
    public EntityUid? ShadowBoxingActionEntity;

    [DataField]
    public bool ShadowBoxingActive = false;
}

[RegisterComponent]
public sealed partial class ShadowSnareBlindMarkerComponent : Component { }

[RegisterComponent]
public sealed partial class ShadowSnareEnsnareComponent : Component
{
    [DataField]
    public EntityUid Victim;
}
