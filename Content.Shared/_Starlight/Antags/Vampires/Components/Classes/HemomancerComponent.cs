using Content.Shared.Polymorph;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Starlight.Antags.Vampires.Components.Classes;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class HemomancerComponent : Component
{
    [AutoNetworkedField]
    public bool InSanguinePool = false;
    [AutoNetworkedField]
    public bool BloodBringersRiteActive = false;

    public int BloodBringersRiteLoopId = 0;

    [DataField]
    public EntProtoId BloodTendrilsVisual = "VampireBloodTendrilVisual";

    [DataField]
    public EntProtoId BloodTendrilsPuddle = "PuddleBlood";

    [DataField]
    public EntProtoId BloodBarrier = "VampireBloodBarrier";

    [DataField]
    public EntProtoId SanguinePoolAction = "ActionVampireSanguinePool";

    [DataField]
    public EntProtoId SanguinePoolEnterEffect = "VampireSanguinePoolOut";

    [DataField]
    public EntProtoId SanguinePoolExitEffect = "VampireSanguinePoolIn";

    [DataField]
    public ProtoId<PolymorphPrototype> SanguinePoolPolymorph = "VampireSanguinePoolPolymorph";

    // [DataField]
    // public SoundSpecifier? SanguinePoolEnterSound = new SoundPathSpecifier("/Audio/_Starlight/Effects/vampire/enter_blood.ogg");

    // [DataField]
    // public SoundSpecifier? SanguinePoolExitSound = new SoundPathSpecifier("/Audio/_Starlight/Effects/vampire/exit_blood.ogg");
}