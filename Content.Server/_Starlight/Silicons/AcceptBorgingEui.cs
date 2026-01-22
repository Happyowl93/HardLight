using Content.Server.EUI;
using Content.Server.Silicons.Borgs;
using Content.Shared._Starlight.Silicons.Borgs;
using Content.Shared.Eui;
using Content.Shared.Mind;
using Content.Shared.Silicons.Borgs;

namespace Content.Server._Starlight.Silicons;

public sealed class AcceptBorgingEui : BaseEui
{
    private readonly EntityUid _brain;
    private readonly EntityUid _mindId;
    private readonly MindComponent _mind;
    private readonly BorgSystem _borgingSystem;

    public AcceptBorgingEui(EntityUid brain, EntityUid mindId, MindComponent mind, BorgSystem borgingSys)
    {
        _brain = brain;
        _mindId = mindId;
        _mind = mind;
        _borgingSystem = borgingSys;
    }

    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        if (msg is not AcceptBorgingChoiceMessage choice
        ||choice.Button != AcceptBorgingUiButton.Accept)
        {
            Close();
            _borgingSystem.OpenGhostRole(_brain, _mindId, _mind);
            return;
        }

        _borgingSystem.TransferMindToChassis(_brain, _mindId, _mind);
        Close();
    }
}
