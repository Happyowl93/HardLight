using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._Starlight.FiringPins;

[Serializable, NetSerializable]
public sealed partial class FiringPinRemovalFinishEvent : SimpleDoAfterEvent { };