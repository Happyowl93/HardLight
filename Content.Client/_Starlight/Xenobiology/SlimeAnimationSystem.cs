using System.Numerics;
using Content.Shared._Starlight.Xenobiology;
using Robust.Client.Animations;
using Robust.Client.GameObjects;

namespace Content.Client._Starlight.Xenobiology;

public sealed class SlimeAnimationSystem : EntitySystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly AnimationPlayerSystem _animation = default!;

    [Dependency] private readonly ILogManager _logManager = default!;
    private ISawmill _sawmill = default!;
    
    private const string SlimeEatAnimationKey = "slime-eat";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<SlimeBiteAnimationMessage>(OnSlimeBiteAnimation);

        _sawmill = _logManager.GetSawmill("slime");
    }

    private void OnSlimeBiteAnimation(SlimeBiteAnimationMessage args)
    {
        _sawmill.Log(LogLevel.Debug, "1");
        _animation.Play(GetEntity(args.Entity), GetSlimeEatAnimation(args.Angle), SlimeEatAnimationKey);
        _sawmill.Log(LogLevel.Debug, "2");
    }

    private Animation GetSlimeEatAnimation(Angle rot)
    {
        const float Distance = 0.15f;
        const float Length = 0.15f;
        var startOffset = rot.RotateVec(new Vector2(0f, 0f));
        var endOffset = rot.RotateVec(new Vector2(0f, -Distance));
        
        _sawmill.Log(LogLevel.Debug, $"endOffset: ({endOffset.X}, {endOffset.Y})");

        return new Animation()
        {
            Length = TimeSpan.FromSeconds(Length),
            AnimationTracks =
            {
                new AnimationTrackComponentProperty()
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Offset),
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(startOffset, 0F),
                        new AnimationTrackProperty.KeyFrame(endOffset, 0.05F),
                        new AnimationTrackProperty.KeyFrame(startOffset, Length),
                    }
                },
            }
        };
    }
}