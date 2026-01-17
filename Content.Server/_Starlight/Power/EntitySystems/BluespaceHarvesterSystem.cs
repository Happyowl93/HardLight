using System.Linq;
using System.Numerics;
using Content.Server.Anomaly;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Station.Systems;
using Content.Shared._Starlight.Power.BluespaceHarvester;
using Content.Shared.Random.Helpers;
using Content.Shared.Station.Components;
using Content.Shared.UserInterface;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server._Starlight.Power.EntitySystems;

/// <summary>
/// Manages Bluespace Harvester machines that convert electrical power into OWN research points.
/// Points can be spent to spawn items from configurable loot pools.
/// </summary>
public sealed class BluespaceHarvesterSystem : EntitySystem
{
    private const float PowerEpsilon = 0.01f; // just not to hardcode it in

    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly PowerNetSystem _powerNet = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly AnomalySystem _anomaly = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BluespaceHarvesterComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<BluespaceHarvesterComponent, BeforeActivatableUIOpenEvent>(OnBeforeUiOpen);
        SubscribeLocalEvent<BluespaceHarvesterComponent, BluespaceHarvesterSetLevelMessage>(OnSetLevel);
        SubscribeLocalEvent<BluespaceHarvesterComponent, BluespaceHarvesterPurchaseMessage>(OnPurchase);
    }

    private void OnMapInit(EntityUid uid, BluespaceHarvesterComponent component, MapInitEvent args)
    {
        if (!TryComp<PowerConsumerComponent>(uid, out var powerConsumer))
            return;

        component.DesiredLevel = ClampDesiredLevel(component, component.DesiredLevel);
        UpdateDrawRate(component, powerConsumer);
        UpdateUi(uid, component, powerConsumer);
    }

    private void OnBeforeUiOpen(EntityUid uid, BluespaceHarvesterComponent component, BeforeActivatableUIOpenEvent args)
    {
        if (!TryComp<PowerConsumerComponent>(uid, out var powerConsumer))
            return;

        UpdateUi(uid, component, powerConsumer, true);
    }

    private void OnSetLevel(EntityUid uid, BluespaceHarvesterComponent component, BluespaceHarvesterSetLevelMessage args)
    {
        if (!TryComp<PowerConsumerComponent>(uid, out var powerConsumer))
            return;

        var desired = ClampDesiredLevel(component, args.Level);
        if (desired == component.DesiredLevel)
            return;

        component.DesiredLevel = desired;
        UpdateDrawRate(component, powerConsumer);
        UpdateUi(uid, component, powerConsumer, true);
    }

    private void OnPurchase(EntityUid uid, BluespaceHarvesterComponent component, BluespaceHarvesterPurchaseMessage args)
    {
        if (!TryComp<PowerConsumerComponent>(uid, out var powerConsumer))
            return;

        if (!_prototype.TryIndex<BluespaceHarvesterPoolPrototype>(args.PoolId, out var pool))
            return;

        if (!pool.Enabled) // safety check
            return;

        if (component.Points < pool.Cost)
            return;

        var lootTable = _prototype.Index(pool.LootTable);
        var spawned = lootTable.Pick(_random);

        component.Points -= pool.Cost;

        // spawn purchased item towards front of harvester
        // TODO: Consider making spawn offset configurable or finding empty tile
        var xform = Transform(uid);
        var spawnCoords = xform.Coordinates.Offset(new Vector2(0f, -0.75f));
        Spawn(spawned, spawnCoords);

        UpdateUi(uid, component, powerConsumer, true);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<BluespaceHarvesterComponent, PowerConsumerComponent>();
        while (query.MoveNext(out var uid, out var component, out var powerConsumer))
        {
            UpdateDrawRate(component, powerConsumer);

            var currentLevel = CalculateCurrentLevel(component, powerConsumer.ReceivedPower);
            if (currentLevel != component.CurrentLevel)
                component.CurrentLevel = currentLevel;

            // Only generate points when machine is actively working
            if (component.CurrentLevel > 0 && powerConsumer.ReceivedPower > 0)
            {
                // Points generation formula => base rate per level + bonus Based on actual power consumption
                var pointsPerSecond = (component.PointsPerLevel * component.CurrentLevel)
                    + (component.PointsPerMegawatt * (powerConsumer.ReceivedPower / 1_000_000f));

                // Accumulator pattern, collect fractional points over time, award whole points when >= 1
                // Prevents losing fractional points each tick
                component.PointAccumulator += pointsPerSecond * frameTime;
                if (component.PointAccumulator >= 1f)
                {
                    var added = (int) component.PointAccumulator;
                    component.PointAccumulator -= added;
                    component.Points += added;
                    component.TotalPoints += added;
                }
            }

            // Dangerous mode
            if (component.CurrentLevel >= component.DangerousLevelThreshold)
            {
                TrySpawnAnomalies(uid, component, frameTime);
            }

            UpdateUi(uid, component, powerConsumer);
        }
    }

    /// <summary>
    /// At dangerous levels, theres a chance per second to spawn flesh anomalies across the station.
    /// This spreads the danger to prevent players from isolating the harvester.
    /// </summary>
    private void TrySpawnAnomalies(EntityUid uid, BluespaceHarvesterComponent component, float frameTime)
    {
        // Calculate chances
        var levelsAboveThreshold = component.CurrentLevel - component.DangerousLevelThreshold;
        var chancePerSecond = component.BaseAnomalyChancePerSecond
            + (levelsAboveThreshold * component.AnomalyChancePerLevelAboveThreshold);

        component.AnomalyAccumulator += frameTime;

        // Check once per second to avoid too frequent rolls
        if (component.AnomalyAccumulator < 1f)
            return;

        component.AnomalyAccumulator -= 1f;

        if (!_random.Prob(chancePerSecond))
            return;

        // Get the station that owns this harvester to spawn anomalies across it
        var stationUid = _station.GetOwningStation(uid);
        if (stationUid == null || !TryComp<StationDataComponent>(stationUid, out var stationData))
        {
            // Spawn near harvester if not on a station
            var xform = Transform(uid);
            var offset = new Vector2(_random.NextFloat(-3f, 3f), _random.NextFloat(-3f, 3f));
            var spawnCoords = xform.Coordinates.Offset(offset);
            Spawn(component.AnomalyPrototype, spawnCoords);
            return;
        }

        var grid = _station.GetLargestGrid((stationUid.Value, stationData));
        if (grid == null)
            return;

        _anomaly.SpawnOnRandomGridLocation(grid.Value, component.AnomalyPrototype);
    }

    private static int ClampDesiredLevel(BluespaceHarvesterComponent component, int level)
    {
        var max = component.LevelPowerDraw.Count;
        if (max <= 0)
            return 0;

        return Math.Clamp(level, 0, max);
    }

    /// <summary>
    /// Determines the actual operating level based on available power.
    /// The machine operates at the highest level it can sustain with current power.
    /// </summary>
    private static int CalculateCurrentLevel(BluespaceHarvesterComponent component, float receivedPower)
    {
        if (component.DesiredLevel <= 0 || component.LevelPowerDraw.Count == 0)
            return 0;

        // Cant exceed desired level even if we have power for more
        var maxLevel = Math.Min(component.DesiredLevel, component.LevelPowerDraw.Count);
        var current = 0;

        // Find highest level we can sustain with current power
        for (var i = 0; i < maxLevel; i++)
        {
            if (receivedPower + PowerEpsilon >= component.LevelPowerDraw[i])
                current = i + 1;
        }

        return current;
    }

    private static float GetPowerForNextLevel(BluespaceHarvesterComponent component, int currentLevel)
    {
        if (component.LevelPowerDraw.Count == 0)
            return 0f;

        var index = Math.Clamp(currentLevel, 0, component.LevelPowerDraw.Count - 1);
        return component.LevelPowerDraw[index];
    }

    private static void UpdateDrawRate(BluespaceHarvesterComponent component, PowerConsumerComponent powerConsumer)
    {
        if (component.DesiredLevel <= 0)
        {
            powerConsumer.DrawRate = 0f;
            return;
        }

        var index = Math.Clamp(component.DesiredLevel - 1, 0, component.LevelPowerDraw.Count - 1);
        powerConsumer.DrawRate = component.LevelPowerDraw[index];
    }

    private void UpdateUi(EntityUid uid, BluespaceHarvesterComponent component, PowerConsumerComponent powerConsumer, bool force = false)
    {
        var currentPower = powerConsumer.ReceivedPower;
        var powerForNext = GetPowerForNextLevel(component, component.CurrentLevel);

        // Get network max supply
        var networkSupply = GetNetworkSupply(powerConsumer);
        var dangerousMode = component.CurrentLevel >= component.DangerousLevelThreshold;

        // Build pool list from prototypes each update
        var pools = _prototype.EnumeratePrototypes<BluespaceHarvesterPoolPrototype>()
            .Where(pool => pool.Enabled)
            .OrderBy(pool => pool.Order)
            .ThenBy(pool => pool.ID)
            .Select(pool => new BluespaceHarvesterPoolEntry(pool.ID, pool.Name, pool.Cost, pool.Enabled))
            .ToArray();

        // Only send UI update if something actually changed
        var changed = force
                      || component.LastUiCurrentLevel != component.CurrentLevel
                      || component.LastUiDesiredLevel != component.DesiredLevel
                      || component.LastUiPoints != component.Points
                      || component.LastUiTotalPoints != component.TotalPoints
                      || component.LastUiDangerousMode != dangerousMode
                      || !MathHelper.CloseTo(component.LastUiCurrentPower, currentPower)
                      || !MathHelper.CloseTo(component.LastUiNextPower, powerForNext)
                      || !MathHelper.CloseTo(component.LastUiNetworkSupply, networkSupply);

        if (!changed)
            return;

        component.LastUiCurrentLevel = component.CurrentLevel;
        component.LastUiDesiredLevel = component.DesiredLevel;
        component.LastUiPoints = component.Points;
        component.LastUiTotalPoints = component.TotalPoints;
        component.LastUiCurrentPower = currentPower;
        component.LastUiNextPower = powerForNext;
        component.LastUiNetworkSupply = networkSupply;
        component.LastUiDangerousMode = dangerousMode;

        var state = new BluespaceHarvesterUiState(
            component.CurrentLevel,
            component.DesiredLevel,
            component.LevelPowerDraw.Count,
            currentPower,
            powerForNext,
            networkSupply,
            component.Points,
            component.TotalPoints,
            dangerousMode,
            pools);

        _ui.SetUiState(uid, BluespaceHarvesterUiKey.Key, state);
    }

    /// <summary>
    /// Gets THEORETICAL max supply from the power network this consumer is connected to
    /// </summary>
    private float GetNetworkSupply(PowerConsumerComponent powerConsumer)
    {
        var net = powerConsumer.Net;
        if (net == null)
            return 0f;

        var stats = _powerNet.GetNetworkStatistics(net.NetworkNode);
        return stats.SupplyTheoretical;
    }
}
