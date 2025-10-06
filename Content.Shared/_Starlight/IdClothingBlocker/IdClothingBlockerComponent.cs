using Content.Shared.Access;
using Content.Shared.Roles;
using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Set;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Starlight.IdClothingBlocker;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class IdClothingBlockerComponent : Component
{
    [DataField("isBlocked")] [AutoNetworkedField]
    public bool IsBlocked = false;

    [DataField("allowedJobs")]
    public List<ProtoId<AccessLevelPrototype>>? AllowedJobs = new();

    [DataField("beepSound")]
    public SoundSpecifier BeepSound = new SoundPathSpecifier("/Audio/Effects/beep1.ogg");
} 