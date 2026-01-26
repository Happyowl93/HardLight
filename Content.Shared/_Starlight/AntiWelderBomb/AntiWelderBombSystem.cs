using Robust.Shared.Prototypes;
using Content.Shared.Tag;
using Content.Shared.Interaction.Events;
using Content.Shared.Movement.Pulling.Events;

namespace Content.Shared._Starlight.AntiWelderBomb;

public sealed class AntiWelderBombSystem : EntitySystem
{
    [Dependency] private readonly TagSystem _tags = default!;

    private static readonly ProtoId<TagPrototype> _fuelTankTag = "FuelTank";

    public override void Initialize()
    {
        SubscribeLocalEvent<AntiWelderBombComponent, AttackAttemptEvent>(OnAttackAttempt);
        SubscribeLocalEvent<AntiWelderBombComponent, PullAttemptEvent>(OnPullAttempt);
    }

    private void OnAttackAttempt(EntityUid uid, AntiWelderBombComponent comp, AttackAttemptEvent args)
    {
        if (args.Target is { } target && IsFuelTank(target))
            args.Cancel();
    }

    private void OnPullAttempt(EntityUid uid, AntiWelderBombComponent comp, PullAttemptEvent args)
    {
        if (args.PulledUid is { } target && IsFuelTank(target))
            args.Cancelled = true;
    }

    private bool IsFuelTank(EntityUid uid) => _tags.HasTag(uid, _fuelTankTag);
}