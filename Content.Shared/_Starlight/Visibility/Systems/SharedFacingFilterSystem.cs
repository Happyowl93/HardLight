using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Player;

namespace Content.Shared._Starlight.Visibility.Systems;

public sealed class SharedFacingFilterSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    /// Builds a player filter from source entity PVS, limited to attached viewers facing the source entity.
    /// <param name="source">Entity being viewed.</param>
    /// <param name="except">Optional entity to exclude by attached player entity.</param>
    /// <param name="rangeMultiplier">PVS range multiplier used when collecting initial recipients.</param>
    /// <param name="maxAngleDeltaRadians">Maximum facing angle delta from viewer forward vector to source direction.</param>
    public Filter FacingPvsExcept(
        EntityUid source,
        EntityUid? except = null,
        float rangeMultiplier = 2f,
        float maxAngleDeltaRadians = MathF.PI / 2f)
    {
        var filter = Filter.Pvs(source, rangeMultiplier, entityManager: EntityManager);

        if (except is { } excluded)
            filter.RemovePlayerByAttachedEntity(excluded);

        var xformQuery = GetEntityQuery<TransformComponent>();
        filter.RemoveWhereAttachedEntity(viewer => !IsFacingTowards(viewer, source, maxAngleDeltaRadians, xformQuery));
        return filter;
    }

    public bool IsFacingTowards(
        EntityUid facing,
        EntityUid target,
        float maxAngleDeltaRadians = MathF.PI / 2f,
        EntityQuery<TransformComponent>? xformQuery = null)
    {
        var query = xformQuery ?? GetEntityQuery<TransformComponent>();

        if (!query.TryGetComponent(facing, out var facingXform) ||
            !query.TryGetComponent(target, out var targetXform) ||
            facingXform.MapID != targetXform.MapID)
        {
            return true;
        }

        var facingPos = _transform.GetWorldPosition(facingXform, query);
        var targetPos = _transform.GetWorldPosition(targetXform, query);
        var toTarget = targetPos - facingPos;

        if (toTarget.LengthSquared() <= 0.0001f)
            return true;

        var facingAngle = _transform.GetWorldRotation(facing, query);
        var targetAngle = Angle.FromWorldVec(toTarget);
        var angleDelta = Math.Abs(Angle.ShortestDistance(facingAngle, targetAngle).Theta);

        return angleDelta <= maxAngleDeltaRadians;
    }
}
