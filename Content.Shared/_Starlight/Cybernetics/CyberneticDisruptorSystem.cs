using Content.Shared.Administration.Logs;
using Content.Shared.Interaction;
using Content.Shared.Starlight.Cybernetics.Components;
using Robust.Shared.Audio.Systems;
using Content.Shared.DoAfter;

namespace Content.Shared.Starlight.Cybernetics;

public sealed class CyberneticDisruptorSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SharedCyberneticDisruptionSystem _disrupt = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CyberneticDisruptorComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<CyberneticDisruptorComponent, CyberneticDisruptorDoafterEvent>(OnDoafter);
    }
    private void OnAfterInteract(EntityUid uid, CyberneticDisruptorComponent comp, AfterInteractEvent args)
    {
        if (!args.CanReach || args.Target is not { } target)
            return;

        var doAfter = new DoAfterArgs(EntityManager, args.User, comp.UseTime, new CyberneticDisruptorDoafterEvent(), uid, target)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            RequireCanInteract = true,
            CancelDuplicate = true
        };
        _doAfter.TryStartDoAfter(doAfter);
    }

    private void OnDoafter(EntityUid uid, CyberneticDisruptorComponent comp, CyberneticDisruptorDoafterEvent args)
    { 
        if(!args.Target.HasValue)
            return;

        if(args.Cancelled)
            return;

        _disrupt.TryAddCyberneticDisruptionDuration(args.Target.Value, comp.Duration, comp.RefreshDuration);
        args.Handled = true;
    }
}
