using Content.Server.NodeContainer.Nodes;
using Content.Server.Power.Nodes;
using Content.Shared.NodeContainer;
using Robust.Shared.Map.Components;

namespace Content.Server._Starlight.Power.Nodes;

/// <summary>
/// A cable device node that can connect to power cables across a larger area than a single tile.
/// Useful for multi-tile machines that need to connect to any cable
/// within their presence, not just the center tile.
/// </summary>
[DataDefinition]
public sealed partial class LargeCableDeviceNode : CableDeviceNode
{
    /// <summary>
    /// The bounding box within which this node will search for cable nodes to connect to.
    /// If null, the node behaves like a standard <see cref="CableDeviceNode"/>.
    /// </summary>
    [DataField]
    public Box2? Bounds;

    /// <summary>
    /// Finds all reachable cable nodes within the defined bounds.
    /// </summary>
    /// <param name="xform">The transform of the entity owning this node.</param>
    /// <param name="nodeQuery">Query for NodeContainerComponent lookup.</param>
    /// <param name="xformQuery">Query for TransformComponent lookup.</param>
    /// <param name="grid">The map grid the entity ison (null if not on a grid).</param>
    /// <param name="entMan">The entity manager for system access.</param>
    public override IEnumerable<Node> GetReachableNodes(
        TransformComponent xform,
        EntityQuery<NodeContainerComponent> nodeQuery,
        EntityQuery<TransformComponent> xformQuery,
        MapGridComponent? grid,
        IEntityManager entMan)
    {
        // Must be anchored and on a grid to connect to cables
        if (!xform.Anchored || grid == null)
            yield break;

        // No custom bounds =Ю fall back to standard single-tile behavior
        if (Bounds == null)
        {
            foreach (var node in base.GetReachableNodes(xform, nodeQuery, xformQuery, grid, entMan))
                yield return node;

            yield break;
        }

        // Calculate world coordinates for the bounding box corners
        var bounds = Bounds.Value;
        var minCoords = xform.Coordinates.Offset(bounds.BottomLeft);
        var maxCoords = xform.Coordinates.Offset(bounds.TopRight);

        if (xform.GridUid == null)
            yield break;

        // Convert world coordinates to tile indices
        var mapSystem = entMan.EntitySysManager.GetEntitySystem<SharedMapSystem>();
        var gridUid = xform.GridUid.Value;
        var minTile = mapSystem.TileIndicesFor(gridUid, grid, minCoords);
        var maxTile = mapSystem.TileIndicesFor(gridUid, grid, maxCoords);

        // Ensure we iterate in the correct order regardless of coordinate signs
        var minX = Math.Min(minTile.X, maxTile.X);
        var maxX = Math.Max(minTile.X, maxTile.X);
        var minY = Math.Min(minTile.Y, maxTile.Y);
        var maxY = Math.Max(minTile.Y, maxTile.Y);

        // Iterate through all tiles in the bounding box and find cable nodes
        for (var x = minX; x <= maxX; x++)
        {
            for (var y = minY; y <= maxY; y++)
            {
                var tile = new Vector2i(x, y);

                foreach (var node in NodeHelpers.GetNodesInTile(nodeQuery, grid, tile))
                {
                    if (node is CableNode)
                        yield return node;
                }
            }
        }
    }
}
