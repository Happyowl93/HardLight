using Content.Shared.Alert;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._Starlight.Shoelaces.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ShoelaceTiedComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntProtoId StatusEffect = "StatusEffectTiedShoelaces";

    [DataField, AutoNetworkedField]
    public float UntieSelfTime = 4.0f;

    [DataField, AutoNetworkedField]
    public float UntieAssistTime = 2.0f;

    [DataField, AutoNetworkedField]
    public float TripKnockdownTime = 1.5f;

    [DataField, AutoNetworkedField]
    public float TripAttemptCooldown = 0.75f;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField]
    public TimeSpan NextTripAttempt;

    [DataField]
    public ProtoId<AlertPrototype> Alert = "ShoelacesTied";
}

public sealed partial class RemoveTiedShoelacesAlertEvent : BaseAlertEvent;
