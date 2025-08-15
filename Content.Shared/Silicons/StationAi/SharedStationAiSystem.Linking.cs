using Content.Shared.DeviceLinking.Events;
using Content.Shared.Silicons.Laws.Components;
using Content.Shared.Silicons.StationAi;

namespace Content.Shared.Silicons.StationAi;

public abstract partial class SharedStationAiSystem
{
    // Handles device linking

    private void InitializeLinking()
    {
        SubscribeLocalEvent<StationAiCoreComponent, NewLinkEvent>(OnNewLink);
        SubscribeLocalEvent<StationAiCoreComponent, PortDisconnectedEvent>(OnPortDisconnected);
    }
    
    private void OnNewLink(Entity<StationAiCoreComponent> ent, ref NewLinkEvent args)
    {
        if (!TryComp<SiliconLawUpdaterComponent>(args.Sink, out var lawUpdater))
            return;

        ent.Comp.LawConsole = GetNetEntity(args.Sink);

        lawUpdater.Core = GetNetEntity(ent.Owner);
        Dirty(args.Sink, lawUpdater);
        Dirty(ent);
    }
    
    private void OnPortDisconnected(Entity<StationAiCoreComponent> ent, ref PortDisconnectedEvent args)
    {
        var consoleNetEntity = ent.Comp.LawConsole;
        if (args.Port != ent.Comp.LinkingPort || consoleNetEntity == null)
            return;

        var lawConsoleEntityUid = GetEntity(consoleNetEntity);
        if (TryComp<SiliconLawUpdaterComponent>(lawConsoleEntityUid, out var lawUpdater))
        {
            lawUpdater.Core = null;
            Dirty(lawConsoleEntityUid.Value, lawUpdater);
        }

        ent.Comp.LawConsole = null;
        Dirty(ent);
    }
}