using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared._Starlight.Components;

/// <summary>
/// Raised by the client after processing one of the clientside component control events. Mainly to let the invoking player know the result of the operation.
/// </summary>
[Serializable, NetSerializable]
public sealed class ClientComponentControlResultEvent : EntityEventArgs
{
    public enum ClientComponentControlType
    {
        Create,
        Write,
        Read,
        Remove,
    }

    public NetUserId Target;
    public ClientComponentControlType ControlType; // what kind of operation was it?
    public bool ControlSuccess; // did the operation succeed or fail?
    public string Message = default!; // optional message to further elaborate on the result.
}