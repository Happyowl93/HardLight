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
        SubscribeLocalEvent<ClientCompControlComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<ClientCompControlComponent, AfterAutoHandleStateEvent>(OnAfterHandleState);
    }

    private void OnStartup(EntityUid uid, ClientCompControlComponent comp, ComponentStartup _) =>
        EnsureState(uid, comp);

    private void OnAfterHandleState(EntityUid uid, ClientCompControlComponent comp, AfterAutoHandleStateEvent _) =>
        EnsureState(uid, comp);
    
    private void EnsureState(EntityUid uid, ClientCompControlComponent comp)
    {
        foreach (var c in comp.EnsuredComponents)
        {
            if (!_factory.TryGetRegistration(c, out var registration, true)) continue;
            var toAdd = _factory.GetComponent(registration);
            if (HasComp(uid, toAdd.GetType())) continue;
            AddComp(uid, toAdd);
        }

        foreach (var c in comp.RemovedComponents)
        {
            if (!_factory.TryGetRegistration(c, out var registration, true)) continue;
            var toRemove = _factory.GetComponent(registration);
            if (!HasComp(uid, toRemove.GetType())) continue;
            RemComp(uid, toRemove.GetType());
        }

        foreach (var c in comp.ViewVariablesWrites)
        {
            var resolved = _vvm.ResolvePath($"/entity/{uid}{c.Key}");
            var curr = _vvm.ReadPathSerialized(c.Key);
            if(curr == c.Value) continue;
            resolved?.Set(c.Value);
        }
    }
}