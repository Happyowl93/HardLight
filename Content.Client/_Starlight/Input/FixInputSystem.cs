using Robust.Client.GameObjects;
using Robust.Shared.Console;

namespace Content.Client._Starlight.Input;

public sealed class FixInputCommand : LocalizedEntityCommands
{
    [Dependency] private readonly IEntitySystemManager _system = default!;
    
    public override string Command => "fixinput";

    public override void Execute(IConsoleShell shell, string argStr, string[] args) =>
        _system.GetEntitySystem<InputSystem>().SetEntityContextActive();
}