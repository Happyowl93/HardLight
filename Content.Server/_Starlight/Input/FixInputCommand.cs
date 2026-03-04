using System.Linq;
using Content.Server.Administration;
using Content.Shared._Starlight.Input;
using Content.Shared.Administration;
using Robust.Shared.Toolshed;

namespace Content.Server._Starlight.Input;

[ToolshedCommand]
[AnyCommand]
public sealed class FixInputCommand : ToolshedCommand
{
    [Dependency] private readonly IEntityNetworkManager _net = default!;

    [CommandImplementation]
    public EntityUid FixInput(IInvocationContext ctx, [PipedArgument] EntityUid uid)
    {
        _net.SendSystemNetworkMessage(new FixInputEvent(), ctx.Session!.Channel);
        ctx.WriteLine($"Refreshed {ctx.Session.Name}'s input context.");
        return uid;
    }

    [CommandImplementation]
    public IEnumerable<EntityUid> FixInput(IInvocationContext ctx, [PipedArgument] IEnumerable<EntityUid> uid)
        => uid.Select(x => FixInput(ctx, x));
}