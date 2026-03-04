using System.Linq;
using Content.Server.Administration;
using Content.Server.Administration.Logs;
using Content.Server.Mind;
using Content.Shared.Administration;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Silicons.StationAi;
using Robust.Server.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Toolshed;

namespace Content.Server._Starlight.Administration.Systems.Commands;

[ToolshedCommand]
[AdminCommand(AdminFlags.Fun)]
public sealed class AITakeoverCommand : ToolshedCommand
{
    [Dependency] private readonly IAdminLogManager _alog = default!;
    private ContainerSystem? _container;
    private MindSystem? _mind;
    
    private static readonly EntProtoId DefaultAi = "StationAiBrainConstructed";
    private static readonly string NotAICore = "Target must be an AI core.";
    
    [CommandImplementation]
    public EntityUid AITakeover(IInvocationContext ctx, [PipedArgument] EntityUid uid, EntityUid target)
    {
        if (!HasComp<StationAiCoreComponent>(target))
        {
            ctx.WriteLine(NotAICore);
            return uid;
        }
        _container ??= EntitySystemManager.GetEntitySystem<ContainerSystem>();
        _mind ??= EntitySystemManager.GetEntitySystem<MindSystem>();
        foreach (var entity in _container.GetAllContainers(target).SelectMany(container => container.ContainedEntities))
        {
            if (!HasComp<BorgBrainComponent>(entity)) continue;
            _mind.ControlMob(uid, entity);
            return entity;
        }
        var brain = EntityManager.SpawnInContainerOrDrop(DefaultAi, target, StationAiCoreComponent.Container);
        _mind.ControlMob(uid, brain);
        return brain;
    }
}