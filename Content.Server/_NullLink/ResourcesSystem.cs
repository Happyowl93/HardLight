using Content.Server._NullLink.Event;
using Content.Server.Administration.Managers;

public sealed partial class ResourcesSystem : EntitySystem
{
    [Dependency] private readonly IPlayerRolesManager _playerRoles = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlayerResourcesUpdatedEvent>(OnPlayerResourcesUpdated);
    }

    private void OnPlayerResourcesUpdated(ref PlayerResourcesUpdatedEvent ev)
    {
        if (ev.Resources.TryGetValue("credits", out var balance))
            _playerRoles.SetBalance(ev.Player, (int)balance, skipNullLink: true); // We skip null link because it's request to update from null link itself.
    }
}