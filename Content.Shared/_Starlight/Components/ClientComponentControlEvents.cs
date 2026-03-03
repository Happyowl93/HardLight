using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared._Starlight.Components;

/// <summary>
/// Instruct the client to create
/// </summary>
[Serializable, NetSerializable]
public sealed class CreateClientComponentEvent : ClientComponentControlEvent
{
    public NetEntity NetEntityUid; // client knows about net uid so this should work to locate the same entity on client
    public string ComponentName = default!; // resolve component clientside
}

/// <summary>
/// Instruct client to respond with info about a component on a given entity.
/// </summary>
[Serializable, NetSerializable]
public sealed class ReadClientComponentEvent : ClientComponentControlEvent
{
    public NetEntity NetEntityUid; // client knows about net uid so this should work to locate the same entity on client
    public string ComponentName = default!; // will generate vv path to component clientside
    public string ValuePath = default!;
}

/// <summary>
/// Instruct the client to remove a component from the specified entity.
/// </summary>
[Serializable, NetSerializable]
public sealed class RemoveClientComponentEvent : ClientComponentControlEvent
{
    public NetEntity NetEntityUid; // client knows about net uid so this should work to locate the same entity on client
    public string ComponentName = default!; // resolve component clientside
}

/// <summary>
/// Instruct the client to modify the component on the given entity through ViewVariables.
/// </summary>
[Serializable, NetSerializable]
public sealed class WriteClientComponentEvent : ClientComponentControlEvent
{
    public NetEntity NetEntityUid; // client knows about net uid so this should work to locate the same entity on client
    public string ComponentName = default!; // will generate vv path to component clientside
    public string ValuePath = default!;
    public string NewValue = default!;
}
[Serializable, NetSerializable]
public abstract class ClientComponentControlEvent : EntityEventArgs
{
    public NetUserId Target;
} 