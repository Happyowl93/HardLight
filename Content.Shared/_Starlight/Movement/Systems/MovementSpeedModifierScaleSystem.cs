using Content.Shared._Starlight.Movement.Components;
using Content.Shared.Movement.Systems;

namespace Content.Shared._Starlight.Movement.Systems;

public sealed class MovementSpeedModifierScaleSystem : EntitySystem
{

    public override void Initialize()
    {
        SubscribeLocalEvent<MovementSpeedModifierScaleComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovement);
    }

    private void OnRefreshMovement(EntityUid uid, MovementSpeedModifierScaleComponent comp, ref RefreshMovementSpeedModifiersEvent args)
    {
        args.WalkSpeedModifier = (args.WalkSpeedModifier - 1) * comp.MovementSpeedScale + 1;
        args.SprintSpeedModifier = (args.SprintSpeedModifier - 1) * comp.MovementSpeedScale + 1;
    }
} 