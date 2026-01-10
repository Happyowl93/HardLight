using Content.Shared._Starlight.Antags.Vampires.Components;
using Content.Shared.Movement.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared._Starlight.Antags.Vampires.Systems;

/// <summary>
/// Applies movement slowdown when a vampire is starving
/// </summary>
public sealed class SharedVampireStarvationSystem : EntitySystem
{
    [Dependency] private readonly MovementSpeedModifierSystem _movement = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VampireComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovespeed);
        SubscribeLocalEvent<VampireComponent, ComponentHandleState>(OnHandleState);
        SubscribeLocalEvent<VampireComponent, ComponentStartup>(OnStartup);
    }

    private void OnStartup(EntityUid uid, VampireComponent component, ComponentStartup args) =>
        _movement.RefreshMovementSpeedModifiers(uid);

    private void OnHandleState(EntityUid uid, VampireComponent component, ref ComponentHandleState args) =>
        _movement.RefreshMovementSpeedModifiers(uid);

    private void OnRefreshMovespeed(EntityUid uid, VampireComponent component, RefreshMovementSpeedModifiersEvent args)
    {
        if (component.BloodFullness > 0f)
            return;

        args.ModifySpeed(component.StarvationWalkSpeedModifier, component.StarvationSprintSpeedModifier);
    }
}
