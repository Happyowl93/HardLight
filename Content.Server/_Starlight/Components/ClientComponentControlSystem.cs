using System.Linq;
using System.Threading.Tasks;
using Content.Server.GameTicking;
using Content.Shared._Starlight.Components;
using Content.Shared.GameTicking;
using Robust.Server.Player;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Server._Starlight.Components;

public sealed class ClientComponentControlSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _players = default!;

    private readonly Dictionary<NetUserId, TaskCompletionSource<ClientComponentControlResultEvent>> _pending = [];
    // for applying things to entities for a client that joined after a client component was added or modified by CCC.
    private readonly Dictionary<NetEntity, HashSet<string>> _addedComps = [];
    private readonly Dictionary<NetEntity, Dictionary<string, Dictionary<string, string>>> _compWrites = [];

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<ClientComponentControlResultEvent>(OnResult);
        SubscribeLocalEvent<PlayerConnectEvent>(OnPlayerJoined);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
    }

    private void OnResult(ClientComponentControlResultEvent ev)
    {
        Log.Log(LogLevel.Info, $"got result! {ev.ControlType}, {ev.ControlSuccess}, {ev.Message}");
        if (!_pending.Remove(ev.Target, out var tcs)) return;
        tcs.TrySetResult(ev);
    }

    public async Task<ClientComponentControlResultEvent?> SendToClient(ICommonSession session,
        ClientComponentControlEvent ev, float timeout = 5)
    {
        var user = session.UserId;

        if (_pending.TryGetValue(user, out var existing)) existing.TrySetCanceled();

        var tcs = new TaskCompletionSource<ClientComponentControlResultEvent>();
        _pending[user] = tcs;

        Log.Log(LogLevel.Info, $"Raising event for {user}");
        RaiseNetworkEvent(ev, session);

        var delay = Task.Delay(TimeSpan.FromSeconds(timeout));
        Log.Log(LogLevel.Info, "Awaiting!");
        var completed = await Task.WhenAny(delay, tcs.Task);

        Log.Log(LogLevel.Info, $"Got result: {completed}");

        if (completed != delay)
        {
            _pending.Remove(user);
            return await tcs.Task;
        }
        _pending.Remove(user);
        return null;
    }

    public async Task<Dictionary<NetUserId, ClientComponentControlResultEvent?>> SendToAllClients(ClientComponentControlEvent ev, float timeout = 5)
    {
        var sessions = _players.Sessions.ToList();
        if (sessions.Count == 0) return new Dictionary<NetUserId, ClientComponentControlResultEvent?>();

        var tasks = new Dictionary<NetUserId, Task<ClientComponentControlResultEvent?>>();

        foreach (var session in sessions)
        {
            var user = session.UserId;
            tasks[user] = SendToClient(session, ev, timeout);
        }

        Log.Log(LogLevel.Info, "Awaiting for everyone!");
        await Task.WhenAll(tasks.Values);
        Log.Log(LogLevel.Info, "Got results!");

        var results = new Dictionary<NetUserId, ClientComponentControlResultEvent?>();
        foreach (var (user, task) in tasks)
        {
            var result = task.GetAwaiter().GetResult();
            results[user] = result;
            Log.Log(LogLevel.Info, $"Result: {result}");
            if (result is null) continue;
            Log.Log(LogLevel.Info, $"Not null! is it success? {result.ControlSuccess}");
            if (!result.ControlSuccess) continue;
            Log.Log(LogLevel.Info, "Recording! hopefully!");
            RecordComponent(result, ev);
        }
        return results;
    }

    private void RecordComponent(ClientComponentControlResultEvent result, ClientComponentControlEvent request)
    {
        if (!result.ControlSuccess) return;

        switch (request)
        {
            case CreateClientComponentEvent ev:
                {
                    if (!_addedComps.TryGetValue(ev.NetEntityUid, out var comps))
                    {
                        comps = [];
                        _addedComps[ev.NetEntityUid] = comps;
                    }

                    comps.Add(ev.ComponentName);
                    Log.Log(LogLevel.Info, "Saved component creation!");
                    break;
                }
            case WriteClientComponentEvent ev:
                {
                    if (!_compWrites.TryGetValue(ev.NetEntityUid, out var compDict))
                    {
                        compDict = [];
                        _compWrites[ev.NetEntityUid] = compDict;
                    }

                    if (!compDict.TryGetValue(ev.ComponentName, out var pathDict))
                    {
                        pathDict = [];
                        compDict[ev.ComponentName] = pathDict;
                    }

                    pathDict[ev.ValuePath] = ev.NewValue;
                    Log.Log(LogLevel.Info, "Saved component modification!");
                    break;
                }
            case RemoveClientComponentEvent ev:
                {
                    _addedComps.Remove(ev.NetEntityUid);
                    _compWrites.Remove(ev.NetEntityUid);
                    Log.Log(LogLevel.Info, "Saved component removal!");
                    break;
                }
            default: return;
        }
    }

    private void ResetSavedComps()
    {
        _addedComps.Clear();
        _compWrites.Clear();
        Log.Log(LogLevel.Info, "Reset saved components!");
    }

    private void OnPlayerJoined(PlayerConnectEvent ev)
    {
        Log.Log(LogLevel.Info, $"Player joined! {ev.PlayerSession}");
        Log.Info($"Saved adds: {_addedComps.Count}");
        foreach (var kv in _addedComps)
            Log.Info($" saved add: {kv.Key} => [{string.Join(", ", kv.Value)}]");

        Log.Info($"Saved writes: {_compWrites.Count}");
        foreach (var kv in _compWrites)
        foreach (var ck in kv.Value)
            Log.Info($" saved write: {kv.Key} / {ck.Key} => [{string.Join(", ", ck.Value.Select(x=>$"{x.Key}={x.Value}"))}]");
        var session = ev.PlayerSession;
        foreach (var kvp in _addedComps.ToList())
        {
            var entity = kvp.Key;
            if (!EntityManager.TryGetEntity(entity, out var existingEntity))
            {
                // entity is no longer valid, remove from saved data and skip!
                _addedComps.Remove(kvp.Key);
                _compWrites.Remove(kvp.Key);
                Log.Log(LogLevel.Info, "Invalid. Bye!");
                continue;
            }
            foreach (var comp in kvp.Value)
            {
                var cev = new CreateClientComponentEvent
                {
                    NetEntityUid = entity,
                    ComponentName = comp,
                    Target = session.UserId,
                };
                RaiseNetworkEvent(cev, session);
                Log.Log(LogLevel.Info, "Sent network event for creation!");
            }
        }

        foreach (var kvp in _compWrites)
        {
            var entity = kvp.Key;
            foreach (var compKvp in kvp.Value)
            {
                var comp = compKvp.Key;
                foreach (var pathKvp in compKvp.Value)
                {
                    var path = pathKvp.Key;
                    var value = pathKvp.Value;
                    var cev = new WriteClientComponentEvent
                    {
                        NetEntityUid = entity,
                        ComponentName = comp,
                        ValuePath = path,
                        NewValue = value,
                        Target = session.UserId,
                    };
                    RaiseNetworkEvent(cev, session);
                    Log.Log(LogLevel.Info, "Sent network event for modification!");
                }
            }
        }
    }

    private void OnRoundRestart(RoundRestartCleanupEvent ev) => ResetSavedComps();
}