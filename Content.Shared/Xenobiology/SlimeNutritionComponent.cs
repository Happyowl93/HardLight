using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Xenobiology;

[RegisterComponent, NetworkedComponent]
public sealed partial class SlimeNutritionComponent : Component
{
    [DataField("hunger_threshold")]
    public double HungerThreshold;

    [DataField("nutrition")]
    public double Nutrition;

    [DataField("nutrition_change_per_second")]
    public double NutritionChangePerSecond;
}

[Serializable, NetSerializable]
public sealed class SlimeNutritionComponentState : ComponentState
{
    public double HungerThreshold;
    public double Nutrition;
    public double NutritionChangePerSecond;

    public SlimeNutritionComponentState(double hungerThreshold,
        double nutrition,
        double nutritionChangePerSecond)
    {
        HungerThreshold = hungerThreshold;
        Nutrition = nutrition;
        NutritionChangePerSecond = nutritionChangePerSecond;
    }
}