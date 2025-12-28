using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Xenobiology;

[RegisterComponent, NetworkedComponent]
public sealed partial class SlimeNutritionComponent : Component
{
    [DataField("hunger_threshold")]
    public FixedPoint2 HungerThreshold;

    [DataField("nutrition")]
    public FixedPoint2 Nutrition;

    [DataField("nutrition_change_per_second")]
    public FixedPoint2 NutritionChangePerSecond;
}

[Serializable, NetSerializable]
public sealed class SlimeNutritionComponentState : ComponentState
{
    public FixedPoint2 HungerThreshold;
    public FixedPoint2 Nutrition;
    public FixedPoint2 NutritionChangePerSecond;

    public SlimeNutritionComponentState(FixedPoint2 hungerThreshold,
        FixedPoint2 nutrition,
        FixedPoint2 nutritionChangePerSecond)
    {
        HungerThreshold = hungerThreshold;
        Nutrition = nutrition;
        NutritionChangePerSecond = nutritionChangePerSecond;
    }
}