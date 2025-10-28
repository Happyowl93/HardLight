using Robust.Shared.Prototypes;

namespace Content.Server.BPL.Pinata;

[RegisterComponent]
public sealed partial class PinataComponent : Component
{
    [DataField]
    public EntProtoId SpawnOnHit = "FoodSnackMREBrownieOpen";

    [DataField]
    public int MinSpawn = 1;

    [DataField]
    public int MaxSpawn = 4;
}
