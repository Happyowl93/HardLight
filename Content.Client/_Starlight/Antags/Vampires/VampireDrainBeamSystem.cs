using System.Numerics;
using Content.Shared._Starlight.Antags.Vampires;
using Robust.Client.GameObjects;
using Robust.Shared.Timing;

namespace Content.Client._Starlight.Antags.Vampires;

/// <summary>
/// Client-side system for smooth vampire drain beam visualization
/// </summary>
public sealed class VampireDrainBeamSystem : EntitySystem
{
    private static readonly Angle _beamAngleOffset = Angle.Zero;
    private const bool SpriteIsVertical = true;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    /// <summary>
    /// Tracks client-side beam visual entities
    /// Key = (source, target) pair, Value = visual beam entity
    /// </summary>
    private readonly Dictionary<(EntityUid, EntityUid), EntityUid> _activeBeamVisuals = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<VampireDrainBeamEvent>(OnDrainBeamEvent);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // Update all active beam visuals every frame for smooth following
        var toRemove = new List<(EntityUid, EntityUid)>();

        foreach (var ((source, target), beamEntity) in _activeBeamVisuals)
        {
            // Check if entities still exist
            if (!Exists(source) || !Exists(target) || !Exists(beamEntity))
            {
                toRemove.Add((source, target));
                if (Exists(beamEntity))
                    QueueDel(beamEntity);
                continue;
            }

            UpdateBeamVisual(beamEntity, source, target);
        }

        foreach (var key in toRemove)
        {
            _activeBeamVisuals.Remove(key);
        }
    }

    private void OnDrainBeamEvent(VampireDrainBeamEvent ev)
    {
        var source = GetEntity(ev.Source);
        var target = GetEntity(ev.Target);

        if (!Exists(source) || !Exists(target))
            return;

        var key = (source, target);

        if (ev.Create)
        {
            CreateBeamVisual(source, target);
        }
        else
        {
            if (_activeBeamVisuals.TryGetValue(key, out var beamEntity))
            {
                QueueDel(beamEntity);
                _activeBeamVisuals.Remove(key);
            }
        }
    }

    private void CreateBeamVisual(EntityUid source, EntityUid target)
    {
        var key = (source, target);

        // Remove existing beam if any exist
        if (_activeBeamVisuals.TryGetValue(key, out var existingBeam))
        {
            QueueDel(existingBeam);
        }

        var beam = Spawn("VampireDrainBeamVisual", Transform(source).Coordinates);

        _activeBeamVisuals[key] = beam;

        UpdateBeamVisual(beam, source, target);
    }

    private void UpdateBeamVisual(EntityUid beam, EntityUid source, EntityUid target)
    {
        if (!TryComp<SpriteComponent>(beam, out var sprite))
            return;

        var sourcePos = _transform.GetWorldPosition(source);
        var targetPos = _transform.GetWorldPosition(target);

        var direction = targetPos - sourcePos;
        var distance = direction.Length();

        if (distance < 0.1f)
            return;

        var worldAngle = direction.ToWorldAngle() + _beamAngleOffset;

        var midpoint = sourcePos + (direction * 0.5f);
        _transform.SetWorldPosition(beam, midpoint);

        // Rotate entity transform; keep sprite rotation at zero to avoid double rotation artifacts
        _transform.SetWorldRotation(beam, worldAngle);
        _sprite.SetRotation((beam, sprite), Angle.Zero);

        // Scale beam to match distance. Isvertical ? scale Y : scale X
        var length = MathF.Max(0.05f, distance);
        var thickness = 0.9f;
        var scale = SpriteIsVertical ? new Vector2(thickness, length) : new Vector2(length, thickness);
        _sprite.SetScale((beam, sprite), scale);
        _sprite.SetOffset((beam, sprite), Vector2.Zero);
    }

    public override void Shutdown()
    {
        // Clean up all beam visuals
        foreach (var beamEntity in _activeBeamVisuals.Values)
        {
            if (Exists(beamEntity))
                QueueDel(beamEntity);
        }
        _activeBeamVisuals.Clear();

        base.Shutdown();
    }
}
