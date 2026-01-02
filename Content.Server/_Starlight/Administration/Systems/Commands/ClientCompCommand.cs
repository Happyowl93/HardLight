using Content.Server._Starlight.Components;
using Content.Server.Administration;
using Content.Server.GameTicking;
using Content.Server.Ghost;
using Content.Shared._Starlight.Components;
using Content.Shared.Administration;
using Content.Shared.GameTicking;
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
            RefreshEntity(targetEntity);
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
            RefreshEntity(targetEntity);
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
            RefreshEntity(targetEntity);
        }
        catch (Exception e)
        {
            throw; // TODO handle exception
        }
    }

    /// <summary>
    /// Ghost then transfer mind back into entity to refresh things like input component.
    /// </summary>
    private void RefreshEntity(EntityUid uid)
    {
        _mind ??= GetSys<SharedMindSystem>();
        _ghost ??= GetSys<GhostSystem>();
        _ticker ??= GetSys<GameTicker>();
        log ??= LogManager.GetSawmill("ccomp");
        if(!_playerManager.TryGetSessionByEntity(uid, out var player))
            return;
        if (!_ticker.PlayerGameStatuses.TryGetValue(player.UserId, out var playerStatus) ||
            playerStatus is not PlayerGameStatus.JoinedGame)
            return;
        if (!_mind.TryGetMind(player, out var mindId, out var mind))
            (mindId, mind) = _mind.CreateMind(player.UserId);
        _ghost.OnGhostAttempt(mindId, false, true, true, mind);
        _mind.TransferTo(mindId, uid, true, false);
        log.Log(LogLevel.Info, "refreshed entity!");
    }
}