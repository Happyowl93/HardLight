using Content.Shared._Starlight.Components;
using Robust.Shared.Network;

namespace Content.Client._Starlight.Components;

public sealed class ClientComponentControlSystem : EntitySystem
{
    [Dependency] private readonly IViewVariablesManager _vvm = default!;
    [Dependency] private readonly IComponentFactory _factory = default!;
    [Dependency] private readonly IClientNetManager _net = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<CreateClientComponentEvent>(OnCreateClientComp); // add comp to client entity
        SubscribeNetworkEvent<ReadClientComponentEvent>(OnReadClientComp);
        SubscribeNetworkEvent<WriteClientComponentEvent>(OnWriteClientComp); // edit via vv
        SubscribeNetworkEvent<RemoveClientComponentEvent>(OnRemoveClientComp); // remove comp from client entity
    }

    private void OnCreateClientComp(CreateClientComponentEvent ev)
    {
        var result = new ClientComponentControlResultEvent
        {
            ControlType = ClientComponentControlResultEvent.ClientComponentControlType.Create,
            Target = _net.ServerChannel!.UserId,
        };
        try
        {
            if (EntityManager.TryGetEntity(ev.NetEntityUid, out var entity))
            {
                if (_factory.TryGetRegistration(ev.ComponentName, out var reg))
                {
                    var comp = _factory.GetComponent(reg.Name);
                    // too stupid to figure out how to get something that works in place of T when doing Func<T>(), so here's some jank.
                    if(!HasComp(entity.Value, comp.GetType())) AddComp(entity.Value, comp);
                    result.ControlSuccess = true;
                    result.Message = "Added component to client entity.";
                    RaiseNetworkEvent(result);
                    return;
                }

                result.ControlSuccess = false;
                result.Message = "The specified component does not exist client-side.";
                RaiseNetworkEvent(result);
            }
            result.ControlSuccess = false;
            result.Message = "The specified entity does not exist.";
            RaiseNetworkEvent(result);
        }
        catch (Exception e)
        {
            result.ControlSuccess = false;
            result.Message = $"An exception occurred on the target's client. {e}";
            RaiseNetworkEvent(result);
        }
    }

    private void OnReadClientComp(ReadClientComponentEvent ev)
    {
        var result = new ClientComponentControlResultEvent
        {
            ControlType = ClientComponentControlResultEvent.ClientComponentControlType.Read,
            Target = _net.ServerChannel!.UserId,
        };
        try
        {
            if (EntityManager.TryGetEntity(ev.NetEntityUid, out var entity))
            {
                if (_factory.TryGetRegistration(ev.ComponentName, out var reg))
                {
                    var vvPath = $"/c/{entity}/{reg.Name}/{ev.ValuePath}";
                    var data = _vvm.ReadPathSerialized(vvPath);
                    result.ControlSuccess = true;
                    result.Message = data ?? "null";
                    RaiseNetworkEvent(result);
                    return;
                }

                result.ControlSuccess = false;
                result.Message = "The specified component does not exist client-side.";
                RaiseNetworkEvent(result);
            }
            result.ControlSuccess = false;
            result.Message = "The specified entity does not exist.";
            RaiseNetworkEvent(result);
        }
        catch (Exception e)
        {
            result.ControlSuccess = false;
            result.Message = $"An exception occurred on the target's client. {e}";
            RaiseNetworkEvent(result);
        }
    }

    private void OnWriteClientComp(WriteClientComponentEvent ev)
    {
        var result = new ClientComponentControlResultEvent
        {
            ControlType = ClientComponentControlResultEvent.ClientComponentControlType.Write,
            Target = _net.ServerChannel!.UserId,
        };
        try
        {
            if (EntityManager.TryGetEntity(ev.NetEntityUid, out var entity))
            {
                if (_factory.TryGetRegistration(ev.ComponentName, out var reg))
                {

                    var vvPath = $"/entity/{entity}/{reg.Name}/{ev.ValuePath}";
                    var path = _vvm.ResolvePath(vvPath);
                    if (path is null)
                    {
                        result.ControlSuccess = false;
                        result.Message = $"Could not resolve ViewVariables path. Attempted path was: {vvPath}";
                        RaiseNetworkEvent(result);
                        return;
                    }
                    path.Set(ev.NewValue);
                    result.ControlSuccess = true;
                    result.Message = "In theory this should work. If it does, change this string to say as such.";
                    RaiseNetworkEvent(result);
                    return;
                }

                result.ControlSuccess = false;
                result.Message = "The specified component does not exist client-side.";
                RaiseNetworkEvent(result);
            }
            result.ControlSuccess = false;
            result.Message = "The specified entity does not exist.";
            RaiseNetworkEvent(result);
        }
        catch (Exception e)
        {
            result.ControlSuccess = false;
            result.Message = $"An exception occurred on the target's client. {e}";
            RaiseNetworkEvent(result);
        }
    }

    private void OnRemoveClientComp(RemoveClientComponentEvent ev)
    {
        var result = new ClientComponentControlResultEvent
        {
            ControlType = ClientComponentControlResultEvent.ClientComponentControlType.Remove,
            Target = _net.ServerChannel!.UserId,
        };
        try
        {
            if (EntityManager.TryGetEntity(ev.NetEntityUid, out var entity))
            {
                if (_factory.TryGetRegistration(ev.ComponentName, out var reg))
                {
                    var comp = _factory.GetComponent(reg.Name);
                    RemComp(entity.Value, comp);
                    result.ControlSuccess = true;
                    result.Message = "Removed component from client entity.";
                    RaiseNetworkEvent(result);
                    return;
                }

                result.ControlSuccess = false;
                result.Message = "The specified component does not exist client-side.";
                RaiseNetworkEvent(result);
            }
            result.ControlSuccess = false;
            result.Message = "The specified entity does not exist.";
            RaiseNetworkEvent(result);
        }
        catch (Exception e)
        {
            result.ControlSuccess = false;
            result.Message = $"An exception occurred on the target's client. {e}";
            RaiseNetworkEvent(result);
        }
    }
}