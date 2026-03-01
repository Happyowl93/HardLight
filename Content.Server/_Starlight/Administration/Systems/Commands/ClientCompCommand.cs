using Content.Server._Starlight.Components;
using Content.Server.Administration;
using Content.Server.GameTicking;
using Content.Server.Ghost;
using Content.Shared._Starlight.Components;
using Content.Shared.Administration;
using Content.Shared.Mind;
using Robust.Server.Player;
using Robust.Shared.Toolshed;

namespace Content.Server._Starlight.Administration.Systems.Commands;

[ToolshedCommand, AdminCommand(AdminFlags.Fun)]
public sealed class ClientCompCommand : ToolshedCommand
{
    [Dependency] private readonly ILogManager LogManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    private GameTicker? _ticker;
    private SharedMindSystem? _mind;
    private GhostSystem? _ghost;
    private ClientComponentControlSystem? _cc;

    private ISawmill? log;

    [CommandImplementation("ensure")]
    public async void Ensure([PipedArgument] EntityUid targetEntity, string compName)
    {
        try
        {
            log ??= LogManager.GetSawmill("ccomp");
            _cc ??= GetSys<ClientComponentControlSystem>();
            if (!EntityManager.TryGetNetEntity(targetEntity, out var netEntity)) return;
            var ev = new CreateClientComponentEvent { NetEntityUid = netEntity.Value, ComponentName = compName, };
            var results = await _cc.SendToAllClients(ev);
            foreach (var result in results)
            {
                log.Log(LogLevel.Debug, $"result from {result.Key}: {result.Value!.ControlType}, {result.Value!.ControlSuccess}, {result.Value?.Message}");
            }
        }
        catch (Exception e)
        {
            throw; // TODO handle exception
        }
    }

    [CommandImplementation("write")]
    public async void Write([PipedArgument] EntityUid targetEntity, string compName, string path, string data)
    {
        try
        {
            log ??= LogManager.GetSawmill("ccomp");
            _cc ??= GetSys<ClientComponentControlSystem>();
            if (!EntityManager.TryGetNetEntity(targetEntity, out var netEntity)) return;
            var ev = new WriteClientComponentEvent
            {
                NetEntityUid = netEntity.Value, ComponentName = compName, ValuePath = path, NewValue = data,
            };
            var results = await _cc.SendToAllClients(ev);
            foreach (var result in results)
            {
                log.Log(LogLevel.Debug, $"result from {result.Key}: {result.Value!.ControlType}, {result.Value!.ControlSuccess}, {result.Value?.Message}");
            }
        }
        catch (Exception e)
        {
            throw; // TODO handle exception
        }
    }
    
    [CommandImplementation("rm")]
    public async void Remove([PipedArgument] EntityUid targetEntity, string compName)
    {
        try
        {
            log ??= LogManager.GetSawmill("ccomp");
            _cc = GetSys<ClientComponentControlSystem>();
            if (!EntityManager.TryGetNetEntity(targetEntity, out var netEntity)) return;
            var ev = new RemoveClientComponentEvent { NetEntityUid = netEntity.Value, ComponentName = compName, };
            var results = await _cc.SendToAllClients(ev);
            
            foreach (var result in results)
            {
                log.Log(LogLevel.Debug, $"result from {result.Key}: {result.Value!.ControlType}, {result.Value!.ControlSuccess}, {result.Value?.Message}");
            }
        }
        catch (Exception e)
        {
            throw; // TODO handle exception
        }
    }
}