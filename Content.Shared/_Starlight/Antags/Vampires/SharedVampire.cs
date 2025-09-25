using Robust.Shared.Serialization;

namespace Content.Shared._Starlight.Antags.Vampires;

public enum VampireVisualLayers : byte
{
    Digit1,
    Digit2,
    Digit3,
    Digit4,
}

[Serializable, NetSerializable]
public sealed partial class VampireClassClosedBuiMsg : BoundUserInterfaceMessage
{
}

[Serializable, NetSerializable]
public enum VampireClassUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public enum VampireClassType : byte
{
    None = 0,
    Hemomancer = 1,
    Umbrae = 2,
    Gargantua = 3,
    Dantalion = 4
}

[Serializable, NetSerializable]
public sealed class VampireClassChosenBuiMsg : BoundUserInterfaceMessage
{
    public VampireClassType Choice { get; init; }
}