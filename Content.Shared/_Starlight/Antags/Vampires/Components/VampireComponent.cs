using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._Starlight.Antags.Vampires.Components;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState, AutoGenerateComponentPause]

public sealed partial class VampireComponent : Component
{
    /// <summary>
    /// Default abilities, they will be added at start.
    /// </summary>
    [DataField]
    public List<EntProtoId> BaseVampireActions = new()
    {
        "ActionVampireToggleFangs",
        "ActionVampireGlare",
        "ActionVampireRejuvenateI"
    };

    /// <summary>
    /// Core action ids that systems need to manage explicitly.
    /// </summary>
    [DataField]
    public EntProtoId ClassSelectActionId = "ActionClassSelectId";

    [DataField]
    public List<EntProtoId> RejuvenateActions = new()
    {
        "ActionVampireRejuvenateI",
        "ActionVampireRejuvenateII"
    };

    /// <summary>
    /// Lifetime total blood drunk. Used for unlocking abilities.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int TotalBlood = 1500;

    /// <summary>
    /// Total blood drunk by this vampire, used for blood cost calculations.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int DrunkBlood = 1500;

    /// <summary>
    /// Determines whether the fangs are extended or not.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly), DataField, AutoNetworkedField]
    public bool FangsExtended = false;

    public float SipAmount = 10f;

    /// <summary>
    /// Current blood fullness used instead of normal food needs.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly), DataField, AutoNetworkedField]
    public float BloodFullness = 90f;

    /// <summary>
    /// Max amount of blood which can be drained from one person.
    /// </summary>
    [DataField]
    public float MaxBloodFullness = 200f;

    /// <summary>
    /// Decay rate per second for blood fullness.
    /// </summary>
    public float FullnessDecayPerSecond = 0.15f;

    /// <summary>
    /// Action ids for the Hemomancer class
    /// </summary>
    [DataField]
    public List<EntProtoId> HemomancerActions = new()
    {
        "ActionVampireHemomancerClaws",
        "ActionVampireSanguinePool",
        "ActionVampireHemomancerTendrils",
        "ActionVampireBloodBarrier",
        "ActionVampireBloodEruption",
        "ActionVampireBloodBringersRite"
    };

    /// <summary>
    /// Action ids for the Umbrae class
    /// </summary>
    [DataField]
    public List<EntProtoId> UmbraeActions = new()
    {
        "ActionVampireCloakOfDarkness",
        "ActionVampireShadowSnare",
        "ActionVampireShadowAnchor",
        "ActionVampireShadowBoxing",
        "ActionVampireDarkPassage",
        "ActionVampireExtinguish",
        "ActionVampireEternalDarkness"
    };

    /// <summary>
    /// Action ids for the Dantalion class
    /// </summary>
    [DataField]
    public List<EntProtoId> DantalionActions = new()
    {
        "ActionVampireEnthrall",
        "ActionVampirePacify",
        "ActionVampireSubspaceSwap",
        "ActionVampireDecoy",
        "ActionVampireRallyThralls",
        "ActionVampireBloodBond",
        "ActionVampireMassHysteria"
    };

    /// <summary>
    /// Action ids for the Gargantua class
    /// </summary>
    [DataField]
    public List<EntProtoId> GargantuaActions = new()
    {
        "ActionVampireBloodSwell",
        "ActionVampireBloodRush",
        "ActionVampireSeismicStomp",
        "ActionVampireOverwhelmingForce",
        "ActionVampireDemonicGrasp",
        "ActionVampireCharge"
    };

    /// <summary>
    /// Action entities of the vampire, used as ActionId -> EntityUid.
    /// </summary>
    public Dictionary<EntProtoId, EntityUid> ActionEntities = new();

    /// <summary>
    /// Determines whether the vampire is drinking at the moment
    /// </summary>
    public bool IsDrinking = false;

    /// <summary>
    /// tracking how much blood was drunk from each target.
    /// </summary>
    public Dictionary<EntityUid, int> BloodDrunkFromTargets = new();

    public int MaxBloodPerTarget = 200;
    public EntityUid? SpawnedClaws = null;
    [DataField]
    public int ClassSelectThreshold = 150;
    [DataField]
    public int RejuvenateIIThreshold = 200;
    [DataField]
    public int ActionRefreshThreshold = 5;

    [ViewVariables(VVAccess.ReadOnly)]
    public int LastRefreshedBloodLevel = -1;

    [ViewVariables(VVAccess.ReadOnly), DataField, AutoNetworkedField]
    public bool FullPower = true; //Reminder  Не забудь поменят

    [ViewVariables(VVAccess.ReadOnly), DataField, AutoNetworkedField]
    public int UniqueHumanoidVictims = 0;

    [ViewVariables(VVAccess.ReadOnly), DataField, AutoNetworkedField]
    public VampireClassType ChosenClass = VampireClassType.None;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField]
    [AutoPausedField]
    public TimeSpan NextUpdate;

    [DataField]
    public TimeSpan UpdateDelay = TimeSpan.FromSeconds(1);
}

[RegisterComponent]
public sealed partial class ShadowSnareBlindMarkerComponent : Component { }

[RegisterComponent]
public sealed partial class ShadowSnareEnsnareComponent : Component
{
    [DataField]
    public EntityUid Victim;
}
