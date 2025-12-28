namespace Content.Shared.Xenobiology;

public sealed class SlimeNutritionSystem : EntitySystem
{
    /// <inheritdoc />
    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var query = EntityQueryEnumerator<SlimeNutritionComponent>();
        while (query.MoveNext(out var uid, out var slime))
        {
            slime.Nutrition = double.Max(slime.Nutrition + (frameTime * slime.NutritionChangePerSecond), 0);
        }
    }
}